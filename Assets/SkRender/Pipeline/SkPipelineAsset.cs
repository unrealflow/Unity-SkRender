using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

[CreateAssetMenu(fileName ="SkPipelineAsset",menuName ="SkPipeline/SkPipelineAsset",order = -1)]
public class SkPipelineAsset : RenderPipelineAsset
{
    public List<Renderer> Renderers;
    public RayTracingShader shader;
    private void FindRenderers()
    {
        Renderers = new List<Renderer>();
        //var objects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        var objects = Object.FindObjectsOfType(typeof(Renderer)).Cast<Renderer>();
        foreach (var r in objects)
        {
            //var r = ob.GetComponent<Renderer>();
            if (r != null)
            {
                Renderers.Add(r);
            }
        }
    }
    protected override RenderPipeline CreatePipeline()
    {
        FindRenderers();
        return new SkPipeline(this);
    }
}
