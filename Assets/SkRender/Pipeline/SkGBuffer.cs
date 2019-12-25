using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace SkRender
{
    public class SkGBuffer
    {
        public class GBuffer
        {
            public RTHandle position { get { return this.colors?[0]; } }
            public RTHandle normal { get { return this.colors?[1]; } }
            public RTHandle albode { get { return this.colors?[2]; } }

            public RTHandle depth;
            public RTHandle[] colors;

            public RenderTargetIdentifier[] GetID()
            {
                return new RenderTargetIdentifier[] { colors[0], colors[1], colors[2] };
            }
            public GBuffer(Camera camera)
            {
                colors = new RTHandle[3];
                #region Alloc_RT
                colors[0] = RTHandles.Alloc(camera.pixelWidth, camera.pixelHeight,
                                       1, DepthBits.None, GraphicsFormat.R32G32B32A32_SFloat,
                                        FilterMode.Point, TextureWrapMode.Clamp, TextureDimension.Tex2D,
                                       true, false, false, false, 1, 0f,
                                       MSAASamples.None, false, false,
                                       RenderTextureMemoryless.None,
                                       $"Position_{camera.name}");
                colors[1] = RTHandles.Alloc(camera.pixelWidth, camera.pixelHeight,
                                       1, DepthBits.None, GraphicsFormat.R32G32B32A32_SFloat,
                                        FilterMode.Point, TextureWrapMode.Clamp, TextureDimension.Tex2D,
                                       true, false, false, false, 1, 0f,
                                       MSAASamples.None, false, false,
                                       RenderTextureMemoryless.None,
                                       $"Normal_{camera.name}");
                colors[2] = RTHandles.Alloc(camera.pixelWidth, camera.pixelHeight,
                                        1, DepthBits.None, GraphicsFormat.R32G32B32A32_SFloat,
                                         FilterMode.Point, TextureWrapMode.Clamp, TextureDimension.Tex2D,
                                        true, false, false, false, 1, 0f,
                                        MSAASamples.None, false, false,
                                        RenderTextureMemoryless.None,
                                        $"Albedo_{camera.name}");

                depth= RTHandles.Alloc(camera.pixelWidth, camera.pixelHeight,
                                        1, DepthBits.Depth24, GraphicsFormat.None,
                                         FilterMode.Point, TextureWrapMode.Clamp, TextureDimension.Tex2D,
                                        false, false, false, false, 1, 0f,
                                        MSAASamples.None, false, false,
                                        RenderTextureMemoryless.None,
                                        $"Depth_{camera.name}");
                #endregion
            }
            public void Release()
            {
                position.Release();
                normal.Release();
                albode.Release();
                depth?.Release();
            }
        };
        SkPipelineAsset _asset;
        ScriptableCullingParameters cullParam;
        CullingResults cullRes;
        Shader _shader;
        Material material;
        private  Dictionary<int, GBuffer> _GBuffers = new Dictionary<int, GBuffer>();
        private  Dictionary<int, GBuffer> _PreGBuffers = new Dictionary<int, GBuffer>();

        public SkGBuffer(SkPipelineAsset asset)
        {
            _asset = asset;
            if(asset._GBufferShader==null)
            {
                _shader= Shader.Find("SkRender/GBuffer");
            }
            else
            {
                _shader = asset._GBufferShader;
            }
            material = new Material(_shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        public void Render(ScriptableRenderContext context, Camera camera,bool isShow=false)
        {
           
            if (!camera.TryGetCullingParameters(out this.cullParam))
            {
                return;
            }
            SwapRT();
            context.SetupCameraProperties(camera);
            var rt = GetRT(camera);
            var cmd= CommandBufferPool.Get($"CMD_GBuffer: {camera.name}");
            try
            {
                var flags = camera.clearFlags;

                cmd.SetRenderTarget(rt.GetID(), rt.depth);
                cmd.ClearRenderTarget(true,true,Color.black);
                
                foreach (var r in _asset.Renderers)
                {
                    cmd.DrawRenderer(r, material);
                }
                if (isShow)
                {
                    cmd.Blit(rt.position, BuiltinRenderTextureType.CameraTarget, Vector2.one, Vector2.zero);
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
            foreach (var g in _GBuffers)
            {
                g.Value.Release();
            }
        }

        public GBuffer GetRT(Camera camera)
        {
            var id = camera.GetInstanceID();
            if(_GBuffers.TryGetValue(id,out var g))
            {
                return g;
            }
            g = new GBuffer(camera);
            _GBuffers.Add(id, g);
            return g;
        }
        public GBuffer GetPreRT(Camera camera)
        {
            var id = camera.GetInstanceID();
            if (_PreGBuffers.TryGetValue(id, out var g))
            {
                return g;
            }
            g = new GBuffer(camera);
            _PreGBuffers.Add(id, g);
            return g;
        }
        void SwapRT()
        {
            var temp = _PreGBuffers;
            _PreGBuffers = _GBuffers;
            _GBuffers = temp;
        }

    }
}