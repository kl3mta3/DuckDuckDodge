Shader "Universal Render Pipeline/Custom/WaterSimpleURP"
{
    Properties
    {
        _WaterColor ("Water Color", Color) = (0.15,0.45,0.65,1)
        _Smoothness ("Smoothness", Range(0,1)) = 0.9
        _Metallic   ("Metallic",   Range(0,1)) = 0.0
        _Alpha      ("Transparency", Range(0,1)) = 0.6

        _Tiling     ("UV Tiling",  Float) = 2.0
        _WaveAmp    ("Wave Amplitude", Range(0,0.2)) = 0.05
        _WaveFreq   ("Wave Frequency", Range(0,20))  = 6.0
        _WaveSpeed1 ("Wave Speed X", Float) = 0.7
        _WaveSpeed2 ("Wave Speed Y", Float) = 1.1

        _FresnelPow ("Fresnel Power", Range(0.5, 8)) = 3.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Transparent"
            "RenderType"     = "Transparent"
            "IgnoreProjector"= "True"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            // URP lighting variants
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings {
                float4 positionHCS : SV_Position;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float2 uv          : TEXCOORD2;
                float3 viewDirWS   : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _WaterColor;
                half  _Smoothness;
                half  _Metallic;
                half  _Alpha;
                float _Tiling;
                float _WaveAmp;
                float _WaveFreq;
                float _WaveSpeed1;
                float _WaveSpeed2;
                float _FresnelPow;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings o;
                o.positionWS  = TransformObjectToWorld(IN.positionOS.xyz);
                o.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                o.positionHCS = TransformWorldToHClip(o.positionWS);
                o.uv          = IN.uv * _Tiling;
                o.viewDirWS   = GetWorldSpaceViewDir(o.positionWS);
                return o;
            }

            float WaveH(float2 uv, float t)
            {
                float x = sin(uv.x * _WaveFreq + t * _WaveSpeed1);
                float y = sin(uv.y * _WaveFreq + t * _WaveSpeed2);
                return x * y;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float t = _Time.y;

                // Build perturbed normal (assumes flat XZ surface)
                const float e = 0.01;
                float hx = (WaveH(IN.uv + float2(e,0), t) - WaveH(IN.uv - float2(e,0), t)) * _WaveAmp;
                float hy = (WaveH(IN.uv + float2(0,e), t) - WaveH(IN.uv - float2(0,e), t)) * _WaveAmp;
                float3 normalWS = normalize(IN.normalWS + float3(-hx, 0, -hy));

                // --- Initialize structs to zero (fixes your error) ---
                SurfaceData surfaceData = (SurfaceData)0;
                InputData   inputData   = (InputData)0;

                // Fill surface
                surfaceData.albedo      = _WaterColor.rgb;
                surfaceData.metallic    = _Metallic;
                surfaceData.specular    = 0;                 // ignored unless _SPECULAR_SETUP
                surfaceData.smoothness  = _Smoothness;
                surfaceData.normalTS    = float3(0,0,1);     // no normal map; we pass WS normal below
                surfaceData.occlusion   = 1;
                surfaceData.emission    = 0;
                surfaceData.alpha       = 1;

                // Fill input
                inputData.positionWS        = IN.positionWS;
                inputData.normalWS          = normalize(normalWS);
                inputData.viewDirectionWS   = normalize(IN.viewDirWS);
                inputData.shadowCoord       = TransformWorldToShadowCoord(IN.positionWS);
                inputData.vertexLighting    = 0;
                inputData.bakedGI           = SampleSH(inputData.normalWS);

                half4 col = UniversalFragmentPBR(inputData, surfaceData);

                // Fresnel-driven transparency
                float ndv  = saturate(dot(inputData.viewDirectionWS, inputData.normalWS));
                float fres = pow(1.0 - ndv, _FresnelPow);
                col.a = saturate(_Alpha * (0.6 + 0.4 * fres));

                return col;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }
    FallBack Off
}