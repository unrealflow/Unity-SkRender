using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace SkRender
{
    public class SkDenoise
    {
        private  Dictionary<int, RTHandle> _outputTargets = new Dictionary<int, RTHandle>();
        private  Dictionary<int, RTHandle> _preResult = new Dictionary<int, RTHandle>();
        private ComputeShader _shader;
        private SkGBuffer _gLine;
        private SkRayTracing _rLine;
        public SkDenoise(SkPipelineAsset asset,SkGBuffer gLine,SkRayTracing rLine)
        {
            _shader = asset._DenoiseShader;
            if(_shader==null)
            {
                Debug.LogError("No DenoiseShader!");
            }
            _gLine = gLine;
            _rLine = rLine;
        }
        public void Render(ScriptableRenderContext context,Camera camera,bool isShow=true)
        {
            SwapRT();
            var cmd = CommandBufferPool.Get($"CMD_SVGF: {camera.name}");
            var output = GetRT(camera);
            var preRes = GetPreRes(camera);
            var gBuffer = _gLine.GetRT(camera);
            var preGBuffer = _gLine.GetPreRT(camera);
            var rayRes = _rLine.GetRT(camera);
            try
            {
                //cmd.SetComputeIntParam(_shader,0.)
                cmd.SetComputeTextureParam(_shader, 0, UniformParams._DenoiseResult, output);
                cmd.SetComputeTextureParam(_shader, 0, UniformParams._PreResult, preRes);
                cmd.SetComputeTextureParam(_shader, 0, UniformParams._Position, gBuffer.position);
                cmd.SetComputeTextureParam(_shader, 0, UniformParams._Normal, gBuffer.normal);
                cmd.SetComputeTextureParam(_shader, 0, UniformParams._Albedo, gBuffer.albode);
                cmd.SetComputeTextureParam(_shader, 0, UniformParams._PrePosition, preGBuffer.position);
                cmd.SetComputeTextureParam(_shader, 0, UniformParams._PreNormal, preGBuffer.normal);
                cmd.SetComputeTextureParam(_shader, 0, UniformParams._RayResult, rayRes);

                cmd.DispatchCompute(_shader, 0, camera.pixelWidth, camera.pixelHeight, 1);
                if(isShow)
                {
                    cmd.Blit(output, BuiltinRenderTextureType.CameraTarget, Vector2.one, Vector2.zero);
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
            foreach (var item in _outputTargets)
            {
                item.Value.Release();
            }
            foreach (var item in _preResult)
            {
                item.Value.Release();
            }
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
        public RTHandle GetPreRes(Camera camera)
        {
            var id = camera.GetInstanceID();
            if (_preResult.TryGetValue(id, out var rt))
                return rt;

            rt = RTHandles.Alloc(camera.pixelWidth, camera.pixelHeight,
                1, DepthBits.None, GraphicsFormat.R32G32B32A32_SFloat,
                FilterMode.Point, TextureWrapMode.Clamp, TextureDimension.Tex2D,
                true, false, false, false, 1, 0f,
                MSAASamples.None, false, false,
                RenderTextureMemoryless.None,
                $"OutputTarget_{camera.name}");

            _preResult.Add(id, rt);
            return rt;
        }
        public void SwapRT()
        {
            var temp = _outputTargets;
            _outputTargets = _preResult;
            _preResult = _outputTargets;
        }
    }
}