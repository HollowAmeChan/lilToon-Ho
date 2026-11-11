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
        _MatCapTex ("MatCap", 2D) = "white" {}
        _MatCapBumpMap ("MatCap Normal", 2D) = "bump" {}
        _MatCap2ndTex ("MatCap 2nd", 2D) = "white" {}
        _MatCap2ndBumpMap ("MatCap 2nd Normal", 2D) = "bump" {}
        _AnisotropyTangentMap ("Anisotropy Tangent", 2D) = "bump" {}
        _AnisotropyScaleMask ("Anisotropy Scale", 2D) = "white" {}
        _UseMatCap ("Use MatCap", Float) = 0
        _UseMatCap2nd ("Use MatCap 2nd", Float) = 0
        _MatCapColor ("MatCap Color", Color) = (1,1,1,1)
        _MatCap2ndColor ("MatCap 2nd Color", Color) = (1,1,1,1)
        _MatCapBlend ("MatCap Blend", Float) = 1
        _MatCap2ndBlend ("MatCap 2nd Blend", Float) = 1
        _MatCapBlendMode ("MatCap Mode", Float) = 1
        _MatCap2ndBlendMode ("MatCap 2nd Mode", Float) = 1
        _MatCapMainStrength ("MatCap Main Strength", Float) = 0
        _MatCap2ndMainStrength ("MatCap 2nd Main Strength", Float) = 0
        _MatCapNormalStrength ("MatCap Normal Strength", Float) = 1
        _MatCap2ndNormalStrength ("MatCap 2nd Normal Strength", Float) = 1
        _MatCapBumpScale ("MatCap Bump Scale", Float) = 1
        _MatCap2ndBumpScale ("MatCap 2nd Bump Scale", Float) = 1
        _MatCapEnableLighting ("MatCap Lighting", Float) = 0
        _MatCap2ndEnableLighting ("MatCap 2nd Lighting", Float) = 0
        _MatCapShadowMask ("MatCap Shadow", Float) = 0
        _MatCap2ndShadowMask ("MatCap 2nd Shadow", Float) = 0
        _MatCapBackfaceMask ("MatCap Backface", Float) = 0
        _MatCap2ndBackfaceMask ("MatCap 2nd Backface", Float) = 0
        _MatCapLod ("MatCap LOD", Float) = 0
        _MatCap2ndLod ("MatCap 2nd LOD", Float) = 0
        _MatCapBlendUV1 ("MatCap UV", Vector) = (0,0,0,0)
        _MatCap2ndBlendUV1 ("MatCap 2nd UV", Vector) = (0,0,0,0)
        _MatCapPerspective ("MatCap Perspective", Float) = 0
        _MatCap2ndPerspective ("MatCap 2nd Perspective", Float) = 0
        _MatCapZRotCancel ("MatCap Z Cancel", Float) = 1
        _MatCap2ndZRotCancel ("MatCap 2nd Z Cancel", Float) = 1
        _MatCapCustomNormal ("MatCap Custom Normal", Float) = 0
        _MatCap2ndCustomNormal ("MatCap 2nd Custom Normal", Float) = 0
        _UseAnisotropy ("Use Anisotropy", Float) = 0
        _Anisotropy2MatCap ("Aniso to MatCap", Float) = 0
        _Anisotropy2MatCap2nd ("Aniso to MatCap 2nd", Float) = 0
        _AnisotropyScale ("Anisotropy Scale", Float) = 0
        _TestMainNormal ("Test Main Normal", Vector) = (0,0,1,0)
        _TestFacing ("Test Facing", Float) = 1
        _FlipNormal ("Flip Normal", Float) = 1
        _MatCapApplyTransparency ("MatCap Transparency", Float) = 0
        _MatCap2ndApplyTransparency ("MatCap 2nd Transparency", Float) = 0
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
            #define LIL_FEATURE_ANISOTROPY
            #define LIL_FEATURE_AnisotropyTangentMap
            #define LIL_FEATURE_AnisotropyScaleMask
            #define LIL_FEATURE_MATCAP
            #define LIL_FEATURE_MATCAP_2ND
            #define LIL_FEATURE_MatCapTex
            #define LIL_FEATURE_MatCap2ndTex
            #define LIL_FEATURE_MatCapBumpMap
            #define LIL_FEATURE_MatCap2ndBumpMap
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
            float4 _TestMainNormal;
            float _TestFacing;
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
                if(_TestMode >= 3 && _TestMode <= 5)
                {
                    lilFragData fd = (lilFragData)0;
                    fd.col = 0;
                    fd.albedo = 1;
                    fd.lightColor = 1;
                    fd.shadowmix = 1;
                    fd.facing = _TestFacing;
                    fd.uvMain = input.uv;
                    fd.uv0 = input.uv;
                    fd.uv1 = input.uv;
                    fd.V = normalize(float3(0.6, 0.6, 0.52915025));
                    fd.headV = fd.V;
                    fd.N = fd.origN = normalize(_TestMainNormal.xyz);
                    fd.TBN = float3x3(1,0,0,0,1,0,0,0,1);
                    fd.matcapN = fd.N;
                    fd.matcap2ndN = fd.N;
                    OVERRIDE_ANISOTROPY
                    // Observe the production distribution step before custom MatCap normals overwrite local N.
                    if(_TestMode == 4) return float4(fd.matcapN * 0.5 + 0.5, 1);
                    if(_TestMode == 5) return float4(fd.matcap2ndN * 0.5 + 0.5, 1);
                    lilGetMatCap(fd LIL_SAMP_IN(sampler_MainTex));
                    lilGetMatCap2nd(fd LIL_SAMP_IN(sampler_MainTex));
                    return float4(fd.col.rgb, 1);
                }
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
