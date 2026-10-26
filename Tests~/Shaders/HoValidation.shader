Shader "Hidden/lilToonHoValidation"
{
    Properties
    {
        _TestMode ("Mode", Float) = 0
        _TestColor ("Vertex color", Vector) = (1,1,1,1)
        _OutlineZBias ("Z Bias", Float) = 0.8
        _OutlineZBiasMask ("Z Mask", 2D) = "white" {}
        _ShadowReceiveMask ("Receive Mask", 2D) = "white" {}
        _ShadowStrengthMask ("Strength Mask", 2D) = "white" {}
        _UseShadow ("Shadow", Float) = 1
        _ShadowStrength ("Strength", Float) = 1
        _ShadowReceive ("Receive 1", Float) = 1
        _Shadow2ndReceive ("Receive 2", Float) = 0
        _Shadow3rdReceive ("Receive 3", Float) = 0
        _ShadowColor ("Color 1", Color) = (0.8,0.8,0.8,1)
        _Shadow2ndColor ("Color 2", Color) = (0.4,0.4,0.4,0)
        _Shadow3rdColor ("Color 3", Color) = (0.2,0.2,0.2,0)
        _ShadowBorder ("Border 1", Float) = 0.5
        _Shadow2ndBorder ("Border 2", Float) = 0.5
        _Shadow3rdBorder ("Border 3", Float) = 0.5
        _ShadowBlur ("Blur 1", Float) = 0.1
        _Shadow2ndBlur ("Blur 2", Float) = 0.1
        _Shadow3rdBlur ("Blur 3", Float) = 0.1
        _ShadowMaskType ("Mask type", Float) = 0
        _ShadowFlatBlur ("Flat blur", Float) = 1
        _ShadowFlatBorder ("Flat border", Float) = 1
    }
    SubShader
    {
        Pass
        {
            Cull Off ZWrite Off ZTest Always
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ HO_TEST_STRIPPED
            #define LIL_FEATURE_SHADOW
            #define LIL_FEATURE_SHADOW_3RD
            #define LIL_FEATURE_RECEIVE_SHADOW
            #define LIL_FEATURE_ShadowStrengthMask
            #define LIL_PASS_FORWARD
            #define LIL_USE_SHADOW
            #if !defined(HO_TEST_STRIPPED)
                #define LIL_FEATURE_ShadowReceiveMask
                #define LIL_FEATURE_OutlineZBiasMask
            #endif
            #include "Assets/lilToon/Shader/Includes/lil_pipeline_brp.hlsl"
            #include "Assets/lilToon/Shader/Includes/lil_common.hlsl"
            #include "Assets/lilToon/Shader/Includes/lil_common_frag.hlsl"
            float _TestMode;
            float4 _TestColor;
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float bias : TEXCOORD1; };
            v2f vert(appdata_img input)
            {
                v2f output;
                output.pos = UnityObjectToClipPos(input.vertex);
                output.uv = input.texcoord;
                // Evaluate the production mask function in the actual vertex stage.
                output.bias = lilGetOutlineZBias(input.texcoord, _OutlineZBias, _OutlineZBiasMask LIL_SAMP_IN(lil_sampler_linear_repeat));
                return output;
            }
            float4 frag(v2f input) : SV_Target
            {
                if(_TestMode == 0)
                {
                    float3 outlineVector = lilGetOutlineVertexColorVector(_TestColor, float3(0,0,1), float3x3(1,0,0,0,1,0,0,0,1));
                    return float4(outlineVector * 0.5 + 0.5, 1);
                }
                if(_TestMode == 1) return float4(input.bias.xxx, 1);
                lilFragData fd = (lilFragData)0;
                fd.col = 1;
                fd.albedo = 1;
                fd.lightColor = 1;
                fd.N = fd.origN = fd.L = fd.origL = float3(0,0,1);
                fd.facing = 1;
                fd.uvMain = input.uv;
                fd.attenuation = 0;
                lilGetShading(fd LIL_SAMP_IN(lil_sampler_linear_repeat));
                return float4(fd.col.rgb, 1);
            }
            ENDHLSL
        }
    }
}
