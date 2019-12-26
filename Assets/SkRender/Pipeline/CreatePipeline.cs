using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

public class CreatePipeline : MonoBehaviour
{

    public RenderPipelineAsset newPipelineAsset;
    private RenderPipelineAsset _oldPipelineAsset;

    // Start is called before the first frame update
    void Start()
    {
        _oldPipelineAsset = GraphicsSettings.renderPipelineAsset;
        if (!SystemInfo.supportsRayTracing)
        {
            Debug.LogError("You system is not support ray tracing. Please check your graphic API is D3D12 and os is Windows 10.");
            return;
        }
        GraphicsSettings.renderPipelineAsset = newPipelineAsset;
    }
    public void OnDestroy()
    {
        GraphicsSettings.renderPipelineAsset = _oldPipelineAsset;
    }
}
