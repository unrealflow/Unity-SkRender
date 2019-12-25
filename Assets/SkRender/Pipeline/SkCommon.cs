using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SkRender
{
    public static class UniformParams
    {
        //PerCamera
        public static readonly int _View = Shader.PropertyToID("_View");
        public static readonly int _Proj = Shader.PropertyToID("_Proj");
        public static readonly int _JitterProj = Shader.PropertyToID("_JitterProj");
        public static readonly int _PreView = Shader.PropertyToID("_PreView");
        public static readonly int _PreProj = Shader.PropertyToID("_PreProj");
        public static readonly int _InvView = Shader.PropertyToID("_InvView");
        public static readonly int _InvProj = Shader.PropertyToID("_InvProj");
        public static readonly int _FarClip = Shader.PropertyToID("_FarClip");
        public static readonly int _RTSize = Shader.PropertyToID("_RTSize");

        //PerDraw
        public static readonly int _RTAS = Shader.PropertyToID("_RTAS");
        public static readonly int _RT = Shader.PropertyToID("_RT");
        public static readonly int _FrameIndex = Shader.PropertyToID("_FrameIndex");
        public static readonly int _LightColors = Shader.PropertyToID("_LightColors");
        public static readonly int _LightPositions = Shader.PropertyToID("_LightPositions");
        public static readonly int _PRNGStates = Shader.PropertyToID("_PRNGStates");
 

        //Denoise
        public static readonly int _Position = Shader.PropertyToID("_Position");
        public static readonly int _Normal = Shader.PropertyToID("_Normal");
        public static readonly int _PrePosition = Shader.PropertyToID("_PrePosition");
        public static readonly int _PreNormal = Shader.PropertyToID("_PreNormal");
        public static readonly int _Albedo = Shader.PropertyToID("_Albedo");
        public static readonly int _RayResult = Shader.PropertyToID("_RayResult");
        public static readonly int _DenoiseResult = Shader.PropertyToID("_DenoiseResult");
        public static readonly int _PreResult = Shader.PropertyToID("_PreResult");

    }

    public static class SK
    {
        public static System.UInt64 FrameIndex = 0;
        public static Vector2 Jitter = new Vector2();

        public static float RadicalInverse(System.UInt32 Base, System.UInt64 i)
        {
            float Digit, Radical, Inverse;
            Digit = Radical = 1.0f / (float)Base;
            Inverse = 0.0f;
            while (i > 0)
            {
                Inverse += Digit * (float)(i % Base);
                Digit *= Radical;

                i /= Base;
            }
            return Inverse;
        }
        public static void Update()
        {
            FrameIndex += 1;
            Jitter.x = (1.0f - 2.0f * RadicalInverse(2, FrameIndex));
            Jitter.y = (1.0f - 2.0f * RadicalInverse(3, FrameIndex));
        }
    }

}
