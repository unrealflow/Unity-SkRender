using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace SkRender
{

    public class SkRayTracing
    {
        private SkPipelineAsset _asset;
        private const int maxNumSubMeshes = 32;
        private RayTracingAccelerationStructure _RTAS;
        private RayTracingShader _shader;
        private readonly Dictionary<int, RTHandle> _outputTargets = new Dictionary<int, RTHandle>();
        private readonly Dictionary<int, Vector4> _outputTargetSizes = new Dictionary<int, Vector4>();
        private readonly Dictionary<int, ComputeBuffer> _PRNGStates = new Dictionary<int, ComputeBuffer>();

        public SkRayTracing(SkPipelineAsset asset)
        {
            _asset = asset;
            _shader = _asset.shader;
            BuildRTAS();
        }

        public void Render(ScriptableRenderContext context, Camera camera, bool isShow = true)
        {
            var rt = GetRT(camera);
            var PRNGStates = GetPRNGStates(camera);
            var cmd = CommandBufferPool.Get($"CMD_Ray: {camera.name}");
            try
            {
                using (new ProfilingSample(cmd, "RayTracing"))
                {
                    cmd.SetRayTracingShaderPass(_shader, "RayTracing");
                    cmd.SetRayTracingAccelerationStructure(_shader, UniformParams._RTAS, _RTAS);
                    cmd.SetRayTracingTextureParam(_shader, UniformParams._RT, rt);
                    cmd.SetRayTracingBufferParam(_shader, UniformParams._PRNGStates, PRNGStates);
                    cmd.DispatchRays(_shader, "SkRayGenShader", (uint)rt.rt.width, (uint)rt.rt.height, 1, camera);
                }
                if (isShow)
                {
                    using (new ProfilingSample(cmd, "FinalBlit"))
                    {
                        cmd.Blit(rt, BuiltinRenderTextureType.CameraTarget, Vector2.one, Vector2.zero);
                    }
                }
                context.ExecuteCommandBuffer(cmd);
            }
            finally
            {
                CommandBufferPool.Release(cmd);
            }
        }

        public void CleanUp()
        {
            _RTAS?.Dispose();
            foreach (var item in _outputTargets)
            {
                item.Value.Release();
            }
            foreach (var item in _PRNGStates)
            {
                item.Value.Release();
            }
        }

        private void BuildRTAS()
        {
            _RTAS = new RayTracingAccelerationStructure();
            bool[] subMeshFlagArray = new bool[maxNumSubMeshes];
            bool[] subMeshCutoffArray = new bool[maxNumSubMeshes];
            for (var i = 0; i < maxNumSubMeshes; ++i)
            {
                subMeshFlagArray[i] = true;
                subMeshCutoffArray[i] = false;
            }

            foreach (var r in _asset.Renderers)
            {
                _RTAS.AddInstance(r, subMeshFlagArray, subMeshCutoffArray);
            }
            _RTAS.Build();
        }

        public RTHandle GetRT(Camera camera)
        {
            var id = camera.GetInstanceID();
            if (_outputTargets.TryGetValue(id, out var rt))
                return rt;

            rt = RTHandles.Alloc(camera.pixelWidth, camera.pixelHeight,
                1, DepthBits.None, GraphicsFormat.R32G32B32A32_SFloat,
                FilterMode.Point, TextureWrapMode.Clamp, TextureDimension.Tex2D,
                true, false, false, false, 1, 0f, 
                MSAASamples.None, false, false,
                RenderTextureMemoryless.None,
                $"OutputTarget_{camera.name}");

            _outputTargets.Add(id, rt);
            return rt;
        }

        public Vector4 GetRTSize(Camera camera)
        {
            var id = camera.GetInstanceID();

            if (_outputTargetSizes.TryGetValue(id, out var rtSize))
                return rtSize;

            rtSize = new Vector4(camera.pixelWidth, camera.pixelHeight,
                1.0f / camera.pixelWidth, 1.0f / camera.pixelHeight);

            _outputTargetSizes.Add(id, rtSize);

            return rtSize;
        }

        public ComputeBuffer GetPRNGStates(Camera camera)
        {
            var id = camera.GetInstanceID();
            if (_PRNGStates.TryGetValue(id, out var buffer))
                return buffer;

            buffer = new ComputeBuffer(camera.pixelWidth * camera.pixelHeight, 4 * 4, ComputeBufferType.Structured, ComputeBufferMode.Immutable);

            var _mt19937 = new MersenneTwister.MT.mt19937ar_cok_opt_t();
            _mt19937.init_genrand((uint)System.DateTime.Now.Ticks);

            var data = new uint[camera.pixelWidth * camera.pixelHeight * 4];
            for (var i = 0; i < camera.pixelWidth * camera.pixelHeight * 4; ++i)
                data[i] = _mt19937.genrand_int32();
            buffer.SetData(data);

            _PRNGStates.Add(id, buffer);
            return buffer;
        }
    }
}