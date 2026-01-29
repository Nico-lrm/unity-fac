Shader "Custom/URP_Master_Block_Lit"
{
    Properties
    {
        [Header(Base Settings)]
        [MainTexture] _BaseMap("Base Map (Texture)", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color (Tint)", Color) = (1,1,1,1)

        [Header(Outline Settings)]
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0, 0.1)) = 0.03

        [Header(Toon Lighting)]
        _RampThreshold("Shadow Threshold", Range(0, 1)) = 0.5
        _RampSmoothness("Shadow Softness", Range(0.001, 0.5)) = 0.01

        [Header(Randomization)]
        [Toggle] _UseRandomUV ("Enable Random Position/Zoom", Float) = 0
        _TextureZoom ("Texture Zoom", Range(0.01, 2)) = 1.0
        [Toggle] _UseRandomTint ("Enable Random Tint", Float) = 0
        _RandomStrength("Tint Strength", Range(0, 0.2)) = 0.05

        [Header(Emission)]
        [Toggle] _UseEmission ("Enable Emission", Float) = 0
        [NoScaleOffset] _EmissionMap("Emission Mask", 2D) = "black" {}
        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0,1)
        _EmissionPower("Emission Power", Range(0, 10)) = 3.0
        [Toggle] _UsePulse ("Enable Pulse", Float) = 0
        _PulseSpeed("Pulse Speed", Range(0, 10)) = 2.0
        _PulseMin("Min Intensity", Range(0, 1)) = 0.5
        _PulseMax("Max Intensity", Range(1, 2)) = 1.5
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        // --- PASSE 1 : OUTLINE (Inchangé) ---
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor; float _OutlineWidth;
                float4 _BaseMap_ST; float4 _BaseColor; float _RampThreshold; float _RampSmoothness;
                float _UseRandomUV; float _TextureZoom; float _UseRandomTint; float _RandomStrength;
                float _UseEmission; float4 _EmissionColor; float _EmissionPower;
                float _UsePulse; float _PulseSpeed; float _PulseMin; float _PulseMax;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 newPos = input.positionOS.xyz + (input.normalOS * _OutlineWidth);
                output.positionCS = TransformObjectToHClip(newPos);
                return output;
            }
            half4 frag(Varyings input) : SV_Target { return _OutlineColor; }
            ENDHLSL
        }

        // --- PASSE 2 : LIT (CORRIGÉE) ---
        Pass
        {
            Name "MainObject"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // --- PRAGMAS CRUCIAUX ---
            // Ombres Main Light
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            
            // Ombres Additional Lights (C'est ça qui active les SpotLights)
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes 
            { 
                float4 positionOS : POSITION; 
                float3 normalOS : NORMAL; 
                float2 uv : TEXCOORD0; 
            };

            struct Varyings 
            { 
                float4 positionCS : SV_POSITION; 
                float3 normalWS : TEXCOORD0; 
                float2 uv : TEXCOORD1; 
                float3 positionWS : TEXCOORD2; 
                float randomSeed : TEXCOORD3;
                float4 shadowCoord : TEXCOORD4; // <--- AJOUT CRUCIAL
            };

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST; float4 _BaseColor;
                float4 _OutlineColor; float _OutlineWidth;
                float _RampThreshold; float _RampSmoothness;
                float _UseRandomUV; float _TextureZoom; float _UseRandomTint; float _RandomStrength;
                float _UseEmission; float4 _EmissionColor; float _EmissionPower;
                float _UsePulse; float _PulseSpeed; float _PulseMin; float _PulseMax;
            CBUFFER_END

            // Fonction Random
            float GetRandomHash(float2 positionXZ)
            {
                float2 noise = float2(dot(positionXZ, float2(12.9898, 78.233)), dot(positionXZ, float2(39.346, 11.135)));
                return frac(sin(noise.x + noise.y) * 43758.5453);
            }

            // Fonction Helper Toon
            float GetToonRamp(float NdotL)
            {
                return smoothstep(_RampThreshold - _RampSmoothness, _RampThreshold + _RampSmoothness, NdotL);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                // CALCUL CORRECT DES OMBRES
                output.shadowCoord = GetShadowCoord(vertexInput);

                // Logique Random
                float2 gridPos = floor(output.positionWS.xz);
                float randomVal = GetRandomHash(gridPos);

                float2 finalUV = input.uv;
                if (_UseRandomUV > 0.5) { finalUV *= _TextureZoom; finalUV += float2(randomVal, randomVal * 0.5); }
                else { finalUV = TRANSFORM_TEX(input.uv, _BaseMap); }
                output.uv = finalUV;

                output.randomSeed = 1.0;
                if (_UseRandomTint > 0.5) { output.randomSeed = 1.0 + (randomVal * 2.0 - 1.0) * _RandomStrength; }

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                float3 albedo = texColor.rgb * _BaseColor.rgb * input.randomSeed;

                float3 normal = normalize(input.normalWS);
                
                // --- 1. MAIN LIGHT ---
                // Correction : On utilise shadowCoord, pas HClip !
                Light mainLight = GetMainLight(input.shadowCoord);
                
                float NdotL = dot(normal, mainLight.direction);
                float mainRamp = GetToonRamp(NdotL);
                mainRamp *= mainLight.shadowAttenuation; 
                
                float3 lightingColor = mainLight.color * mainRamp;

                // --- 2. ADDITIONAL LIGHTS (Spot / Point) ---
                uint pixelLightCount = GetAdditionalLightsCount();
                for (uint i = 0; i < pixelLightCount; ++i)
                {
                    Light light = GetAdditionalLight(i, input.positionWS);

                    float3 lightDir = light.direction;
                    
                    // On calcule l'angle de la lumière normalement
                    float NdotL_Add = saturate(dot(normal, lightDir));
                    
                    // --- MODIFICATION ICI ---
                    // Au lieu d'utiliser GetToonRamp(NdotL_Add), on utilise la lumière brute
                    // Cela permet aux lampadaires d'avoir un dégradé doux et d'être visibles de loin
                    
                    float attenuation = light.distanceAttenuation * light.shadowAttenuation;
                    
                    // On ajoute la lumière directement (Mode Réaliste pour les lampes)
                    lightingColor += light.color * NdotL_Add * attenuation;
                }

                lightingColor += unity_AmbientSky;
                float3 finalColor = albedo * lightingColor;

                // --- 3. EMISSION ---
                float3 emission = float3(0,0,0);
                if (_UseEmission > 0.5)
                {
                    float mask = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).r;
                    float pulse = 1.0;
                    if (_UsePulse > 0.5) { pulse = lerp(_PulseMin, _PulseMax, (sin(_Time.y * _PulseSpeed) * 0.5 + 0.5)); }
                    emission = mask * _EmissionColor.rgb * _EmissionPower * pulse;
                }

                return half4(finalColor + emission, 1);
            }
            ENDHLSL
        }
    }
}