using SkRender;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class SkPipeline : RenderPipeline
{
    private SkPipelineAsset _asset;
    private SkRayTracing rayTracing;
    private SkGBuffer gBuffer;

    private readonly bool enableRayTracing = false;

    private const int maxLights = 4;
    private Vector4[] lightColors = new Vector4[maxLights];
    private Vector4[] lightPositions = new Vector4[maxLights];

    private Matrix4x4 projMatrix = Matrix4x4.identity;
    private Matrix4x4 viewMatrix = Matrix4x4.identity;
    private Matrix4x4 jitterProj = Matrix4x4.identity;
    private Matrix4x4 preProj = Matrix4x4.identity;
    private Matrix4x4 preView = Matrix4x4.identity;

    public SkPipeline(SkPipelineAsset asset)
    {
        enableRayTracing = SystemInfo.supportsRayTracing;
        if (!enableRayTracing)
        {
            Debug.LogError("You system is not support ray tracing. Please check your graphic API is D3D12 and os is Windows 10.");
            return;
        }
        GraphicsSettings.lightsUseLinearIntensity = true;
        this._asset = asset;
        rayTracing = new SkRayTracing(asset);
        gBuffer = new SkGBuffer(asset);
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        if (!enableRayTracing)
        {
            return;
        }
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
                //gBuffer.Render(context, camera, true);
                rayTracing.Render(context, camera, true);
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

    private void SetupCameraBuffer(Camera camera)
    {
        preProj = projMatrix;
        preView = viewMatrix;

        projMatrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
        viewMatrix = camera.worldToCameraMatrix;

        jitterProj = projMatrix;
        jitterProj[0, 2] += SK.Jitter.x / camera.pixelWidth;
        jitterProj[1, 2] += SK.Jitter.y / camera.pixelHeight;

        var invProjMatrix = Matrix4x4.Inverse(jitterProj);
        var invViewMatrix = Matrix4x4.Inverse(viewMatrix);
        Shader.SetGlobalMatrix(UniformParams._PreProj, preProj);
        Shader.SetGlobalMatrix(UniformParams._PreView, preView);
        Shader.SetGlobalMatrix(UniformParams._Proj, projMatrix);
        Shader.SetGlobalMatrix(UniformParams._View, viewMatrix);
        Shader.SetGlobalMatrix(UniformParams._JitterProj, jitterProj);
        Shader.SetGlobalMatrix(UniformParams._InvProj, invProjMatrix);
        Shader.SetGlobalMatrix(UniformParams._InvView, invViewMatrix);
        Shader.SetGlobalFloat(UniformParams._FarClip, camera.farClipPlane);

        Shader.SetGlobalVectorArray(UniformParams._LightColors, lightColors);
        Shader.SetGlobalVectorArray(UniformParams._LightPositions, lightPositions);
    }

    protected override void Dispose(bool disposing)
    {
        rayTracing.CleanUp();
        base.Dispose(disposing);
    }
}