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
        GraphicsSettings.renderPipelineAsset = newPipelineAsset;
    }
    public void OnDestroy()
    {
        GraphicsSettings.renderPipelineAsset = _oldPipelineAsset;
    }
}
