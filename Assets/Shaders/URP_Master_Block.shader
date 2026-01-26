Shader "Custom/URP_Master_Block"
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

        // --- FEATURE 1 : RANDOMIZATION ---
        [Header(Randomization Features)]
        [Toggle] _UseRandomUV ("Enable Random Position/Zoom", Float) = 0
        _TextureZoom ("Texture Zoom (Tiling)", Range(0.01, 2)) = 1.0
        
        [Toggle] _UseRandomTint ("Enable Random Color Tint", Float) = 0
        _RandomStrength("Tint Variation Strength", Range(0, 0.2)) = 0.05

        // --- FEATURE 2 : MAGMA / EMISSION ---
        [Header(Emission Features)]
        [Toggle] _UseEmission ("Enable Emission (Magma)", Float) = 0
        [NoScaleOffset] _EmissionMap("Emission Mask (Black/White)", 2D) = "black" {}
        [HDR] _EmissionColor("Emission Color (HDR)", Color) = (0,0,0,1)
        _EmissionPower("Emission Power", Range(0, 10)) = 3.0
        
        [Header(Emission Pulse)]
        [Toggle] _UsePulse ("Enable Pulsing", Float) = 0
        _PulseSpeed("Pulse Speed", Range(0, 10)) = 2.0
        _PulseMin("Min Intensity", Range(0, 1)) = 0.5
        _PulseMax("Max Intensity", Range(1, 2)) = 1.5
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        // --- PASSE 1 : CONTOUR (OUTLINE) ---
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front // On affiche l'intérieur du modèle gonflé

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineWidth;
                // On déclare les autres variables pour éviter les erreurs, même si inutilisées ici
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

        // --- PASSE 2 : RENDU PRINCIPAL (TOUT EN UN) ---
        Pass
        {
            Name "MainObject"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // Nécessaire pour les ombres et la lumière
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

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
                float3 positionWS : TEXCOORD2; // Pour le calcul aléatoire
                float randomSeed : TEXCOORD3;  // Stocke la variation de couleur
            };

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _OutlineColor; float _OutlineWidth;
                float _RampThreshold;
                float _RampSmoothness;
                // Random vars
                float _UseRandomUV;
                float _TextureZoom;
                float _UseRandomTint;
                float _RandomStrength;
                // Emission vars
                float _UseEmission;
                float4 _EmissionColor;
                float _EmissionPower;
                float _UsePulse;
                float _PulseSpeed;
                float _PulseMin;
                float _PulseMax;
            CBUFFER_END

            // Fonction aléatoire basée sur la position X/Z
            float GetRandomHash(float2 positionXZ)
            {
                float2 noise = float2(
                    dot(positionXZ, float2(12.9898, 78.233)),
                    dot(positionXZ, float2(39.346, 11.135))
                );
                return frac(sin(noise.x + noise.y) * 43758.5453);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // Position et Normale standard
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                // --- LOGIQUE ALÉATOIRE ---
                // On arrondit la position pour avoir une valeur unique par bloc (si placé sur la grille)
                float2 gridPos = floor(output.positionWS.xz);
                float randomVal = GetRandomHash(gridPos);

                // 1. Calcul UV (Zoom + Random Offset)
                float2 finalUV = input.uv;
                
                if (_UseRandomUV > 0.5)
                {
                    // Zoom
                    finalUV *= _TextureZoom;
                    // Décalage aléatoire
                    finalUV += float2(randomVal, randomVal * 0.5);
                }
                else 
                {
                    // Juste le tiling standard d'Unity si on n'utilise pas le random
                    finalUV = TRANSFORM_TEX(input.uv, _BaseMap);
                }
                
                output.uv = finalUV;

                // 2. Calcul Tint (Variation de couleur)
                output.randomSeed = 1.0;
                if (_UseRandomTint > 0.5)
                {
                    // On varie légèrement la luminosité entre (1 - strength) et (1 + strength)
                    output.randomSeed = 1.0 + (randomVal * 2.0 - 1.0) * _RandomStrength;
                }

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // --- A. TEXTURE & COULEUR DE BASE ---
                float4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                
                // On applique la couleur de base ET la variation aléatoire calculée dans le vertex
                float3 albedo = texColor.rgb * _BaseColor.rgb * input.randomSeed;

                // --- B. ÉCLAIRAGE TOON ---
                Light mainLight = GetMainLight(TransformWorldToHClip(input.positionWS)); // Recup ombres
                float3 normal = normalize(input.normalWS);
                float3 lightDir = normalize(mainLight.direction);
                
                float NdotL = dot(normal, lightDir);
                
                // Effet Toon (Ramp)
                float toonRamp = smoothstep(_RampThreshold - _RampSmoothness, _RampThreshold + _RampSmoothness, NdotL);
                
                // Ombres portées
                float shadow = mainLight.shadowAttenuation;
                toonRamp *= shadow;

                float3 finalLighting = albedo * (mainLight.color * toonRamp + unity_AmbientSky);

                // --- C. ÉMISSION (MAGMA) ---
                float3 emission = float3(0,0,0);
                
                if (_UseEmission > 0.5)
                {
                    float mask = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).r;
                    
                    float pulse = 1.0;
                    if (_UsePulse > 0.5)
                    {
                        pulse = (sin(_Time.y * _PulseSpeed) * 0.5 + 0.5); // 0 à 1
                        pulse = lerp(_PulseMin, _PulseMax, pulse);        // Min à Max
                    }

                    emission = mask * _EmissionColor.rgb * _EmissionPower * pulse;
                }

                // --- D. COMBINAISON FINALE ---
                return half4(finalLighting + emission, 1);
            }
            ENDHLSL
        }
    }
}