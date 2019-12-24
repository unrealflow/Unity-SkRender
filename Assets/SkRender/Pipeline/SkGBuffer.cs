using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace SkRender
{
    public class SkGBuffer
    {
        private class GBuffer
        {
            public RTHandle position { get { return this.L?[0]; } }
            public RTHandle normal { get { return this.L?[1]; } }
            public RTHandle albode { get { return this.L?[2]; } }

            public RTHandle depth;
            public RTHandle[] L;

            public RenderTargetIdentifier[] GetID()
            {
                return new RenderTargetIdentifier[] { L[0],L[1],L[2] };
            }
            public GBuffer(Camera camera)
            {
                L = new RTHandle[3];
                #region Alloc_RT
                L[0] = RTHandles.Alloc(camera.pixelWidth, camera.pixelHeight,
                                        1, DepthBits.None, GraphicsFormat.R32G32B32A32_SFloat,
                                         FilterMode.Point, TextureWrapMode.Clamp, TextureDimension.Tex2D,
                                        true, false, false, false, 1, 0f,
                                        MSAASamples.None, false, false,
                                        RenderTextureMemoryless.None,
                                        $"Position_{camera.name}");
                L[1] = RTHandles.Alloc(camera.pixelWidth, camera.pixelHeight,
                                        1, DepthBits.None, GraphicsFormat.R32G32B32A32_SFloat,
                                         FilterMode.Point, TextureWrapMode.Clamp, TextureDimension.Tex2D,
                                        true, false, false, false, 1, 0f,
                                        MSAASamples.None, false, false,
                                        RenderTextureMemoryless.None,
                                        $"Normal_{camera.name}");
                L[2] = RTHandles.Alloc(camera.pixelWidth, camera.pixelHeight,
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
        };
        SkPipelineAsset _asset;
        ScriptableCullingParameters cullParam;
        CullingResults cullRes;
        private readonly Dictionary<int, GBuffer> gbuffers = new Dictionary<int, GBuffer>();

        public SkGBuffer(SkPipelineAsset asset)
        {
            _asset = asset;
        }

        public void Render(ScriptableRenderContext context, Camera camera,bool isShow=false)
        {
            if (!camera.TryGetCullingParameters(out this.cullParam))
            {
                return;
            }
            context.SetupCameraProperties(camera);
            var rtId = GetRT(camera);
            var depth = GetDepthTex(camera);
            var cmd= CommandBufferPool.Get($"CMD_GBuffer: {camera.name}");
            try
            {
                var flags = camera.clearFlags;
                if (isShow)
                {
                    cmd.Blit(rtId[1], BuiltinRenderTextureType.CameraTarget, Vector2.one, Vector2.zero);
                }
                cmd.SetRenderTarget(rtId, depth);
                cmd.ClearRenderTarget(true,true,Color.black);
                Shader errorShader = Shader.Find("SkRender/GBuffer");
                var errorMaterial = new Material(errorShader)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                foreach (var r in _asset.Renderers)
                {
                    cmd.DrawRenderer(r, errorMaterial);
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
            
        }

        private RenderTargetIdentifier[] GetRT(Camera camera)
        {
            var id = camera.GetInstanceID();
            if(gbuffers.TryGetValue(id,out var g))
            {
                return g.GetID();
            }
            g = new GBuffer(camera);
            gbuffers.Add(id, g);
            return g.GetID();
        }
        private RTHandle GetDepthTex(Camera camera)
        {
            var id = camera.GetInstanceID();
            if (gbuffers.TryGetValue(id, out var g))
            {
                return g.depth;
            }
            g = new GBuffer(camera);
            gbuffers.Add(id, g);
            return g.depth;
        }
    }
}