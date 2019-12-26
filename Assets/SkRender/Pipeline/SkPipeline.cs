using SkRender;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

public class SkPipeline : RenderPipeline
{
    private SkPipelineAsset _asset;
    private SkRayTracing rayTracing;
    private SkGBuffer gBuffer;
    private SkDenoise denoise;

    private const int maxLights = 4;
    private Vector4[] lightColors = new Vector4[maxLights];
    private Vector4[] lightPositions = new Vector4[maxLights];

    public class CamBuf
    {
        public Matrix4x4 projMatrix = Matrix4x4.identity;
        public Matrix4x4 viewMatrix = Matrix4x4.identity;
        public Matrix4x4 jitterProj = Matrix4x4.identity;
        public Matrix4x4 preProj = Matrix4x4.identity;
        public Matrix4x4 preView = Matrix4x4.identity;
    }


    private readonly Dictionary<int, CamBuf> _CamBufs = new Dictionary<int, CamBuf>();
    public SkPipeline(SkPipelineAsset asset)
    {
        GraphicsSettings.lightsUseLinearIntensity = true;
        this._asset = asset;
        gBuffer = new SkGBuffer(asset);
        rayTracing = new SkRayTracing(asset);
        denoise = new SkDenoise(asset, gBuffer, rayTracing);
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        BeginFrameRendering(context, cameras);
        System.Array.Sort(cameras, (l, r) => (int)(l.depth - r.depth));
        SK.Update();
        SetupWorldBuffer();
        foreach (var camera in cameras)
        {
            if (camera.cameraType != CameraType.Game && camera.cameraType != CameraType.SceneView)
                continue;
            if (!camera.TryGetCullingParameters(out ScriptableCullingParameters cullParam))
            {
                continue;
            }
            BeginCameraRendering(context, camera);
            {
                var cullRes = context.Cull(ref cullParam);
                var lights = cullRes.visibleLights;
                ConfigureLights(ref lights);
                SetupCameraBuffer(camera);
                gBuffer.Render(context, camera, false);
                rayTracing.Render(context, camera, true);
                denoise.Render(context, camera, true);
            }
            EndCameraRendering(context, camera);
            context.Submit();
        }
        EndFrameRendering(context, cameras);
    }

    private void ConfigureLights(ref NativeArray<VisibleLight> lights)
    {
        for (int i = 0; i < maxLights; i++)
        {
            if (i < lights.Length)
            {
                lightColors[i] = lights[i].finalColor;
                lightColors[i].w = 0.0f;
                if (lights[i].lightType == LightType.Directional)
                {
                    Vector4 v = lights[i].localToWorldMatrix.GetColumn(2);
                    v.x = -v.x;
                    v.y = -v.y;
                    v.z = -v.z;
                    lightPositions[i] = v;
                }
                else if (lights[i].lightType == LightType.Point)
                {
                    lightColors[i].w = lights[i].light.shadowRadius;
                    lightPositions[i] =
                        lights[i].localToWorldMatrix.GetColumn(3);
                }
            }
            else
            {
                lightPositions[i].w = -1.0f;
            }
        }
    }

    private void SetupWorldBuffer()
    {
        Shader.SetGlobalInt(UniformParams._FrameIndex,
                            SK.FrameIndex > System.Int32.MaxValue ?
                            System.Int32.MaxValue : (int)SK.FrameIndex);
    }
    private CamBuf GetCamBuf(Camera camera)
    {
        var id = camera.GetInstanceID();
        if (_CamBufs.TryGetValue(id, out var buf))
        {
            buf.preProj = buf.jitterProj;
            buf.preView = buf.viewMatrix;
        }
        else
        {
            buf = new CamBuf();
            buf.preProj = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false); ;
            buf.preView = camera.worldToCameraMatrix;
        }
        buf.projMatrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
        buf.viewMatrix = camera.worldToCameraMatrix;

        buf.jitterProj = buf.projMatrix;
        buf.jitterProj[0, 2] += SK.Jitter.x / camera.pixelWidth;
        buf.jitterProj[1, 2] += SK.Jitter.y / camera.pixelHeight;


        return buf;
    }
    private void SetupCameraBuffer(Camera camera)
    {
        var b = GetCamBuf(camera);
        var invProjMatrix = Matrix4x4.Inverse(b.jitterProj);
        var invViewMatrix = Matrix4x4.Inverse(b.viewMatrix);
        Shader.SetGlobalMatrix(UniformParams._PreProj, b.preProj);
        Shader.SetGlobalMatrix(UniformParams._PreView, b.preView);
        Shader.SetGlobalMatrix(UniformParams._Proj, b.projMatrix);
        Shader.SetGlobalMatrix(UniformParams._View, b.viewMatrix);
        Shader.SetGlobalMatrix(UniformParams._JitterProj, b.jitterProj);
        Shader.SetGlobalMatrix(UniformParams._InvProj, invProjMatrix);
        Shader.SetGlobalMatrix(UniformParams._InvView, invViewMatrix);
        Shader.SetGlobalFloat(UniformParams._FarClip, camera.farClipPlane);

        Shader.SetGlobalVectorArray(UniformParams._LightColors, lightColors);
        Shader.SetGlobalVectorArray(UniformParams._LightPositions, lightPositions);
        Shader.SetGlobalVector(UniformParams._RTSize,
                                new Vector4(
                                camera.pixelWidth, camera.pixelHeight, SK.Jitter.x, SK.Jitter.y));
        Shader.SetGlobalVector(UniformParams._BackgroundColor, camera.backgroundColor);
    }

    protected override void Dispose(bool disposing)
    {
        rayTracing?.CleanUp();
        gBuffer?.CleanUp();
        base.Dispose(disposing);
    }
}