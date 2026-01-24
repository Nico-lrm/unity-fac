Shader "Custom/URPWaterCode"
{
    Properties
    {
        [Header(Base Appearance)]
        [MainColor] _BaseColor("Water Color", Color) = (0.2, 0.5, 0.8, 0.8)
        [NoScaleOffset] _BumpMap("Normal Map (Ripples)", 2D) = "bump" {}
        _Smoothness("Smoothness", Range(0, 1)) = 0.9
        _NormalStrength("Ripple Strength", Range(0, 2)) = 1.0

        [Header(Movement)]
        _RippleSpeed("Ripple Speed", Vector) = (0.05, 0.05, 0, 0)
        
        [Header(Waves Physics)]
        _WaveHeight("Wave Height", Range(0, 1)) = 0.2
        _WaveFrequency("Wave Frequency", Range(0, 5)) = 1.0
        _WaveSpeed("Wave Speed", Range(0, 5)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        LOD 300

        Pass
        {
            Name "WaterPass"
            Tags { "LightMode" = "UniversalForward" }
            
            // Paramètres vitaux pour la transparence
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off // On ne bloque pas ce qui est derrière l'eau
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
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
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _RippleSpeed;
                float _WaveHeight;
                float _WaveFrequency;
                float _WaveSpeed;
                float _Smoothness;
                float _NormalStrength;
            CBUFFER_END

            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

            Varyings vert(Attributes input)
            {
                Varyings output;

                // 1. Calcul de la Position Monde
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);

                // 2. MATHÉMATIQUES DES VAGUES (Vertex Displacement)
                // On utilise le Sinus basé sur le temps et la position X/Z
                // On combine deux vagues pour un effet moins "robotique"
                float wave1 = sin(_Time.y * _WaveSpeed + worldPos.x * _WaveFrequency);
                float wave2 = cos(_Time.y * _WaveSpeed * 0.8 + worldPos.z * _WaveFrequency * 0.5);
                
                // On applique le mouvement sur la hauteur (Y)
                worldPos.y += (wave1 + wave2) * _WaveHeight;

                // 3. Transfert des données
                output.positionWS = worldPos;
                output.positionCS = TransformWorldToHClip(worldPos);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                // Animation des UVs pour le courant
                output.uv = input.uv + (_Time.y * _RippleSpeed.xy);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 1. Gestion de la Lumière et Normales
                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                float3 viewDir = normalize(GetCameraPositionWS() - input.positionWS);

                // Lecture de la Normal Map (Ripples)
                float3 normalMap = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv));
                // Ajuster l'intensité
                normalMap.xy *= _NormalStrength;
                normalMap.z = sqrt(1.0 - saturate(dot(normalMap.xy, normalMap.xy)));
                
                // On combine la normale de la géométrie (vagues) avec la normale de la texture (rides)
                // Note : Pour faire simple ici on utilise principalement la normale texture projetée sur la surface
                float3 normal = normalize(input.normalWS + normalMap.x * float3(1,0,0) + normalMap.y * float3(0,0,1));

                // 2. Calculs d'éclairage (Lambert + Specular Blinn-Phong)
                // Diffuse
                float NdotL = max(0, dot(normal, lightDir));
                
                // Specular (Brillance)
                float3 halfVector = normalize(lightDir + viewDir);
                float NdotH = max(0, dot(normal, halfVector));
                float specular = pow(NdotH, _Smoothness * 128.0) * _Smoothness; // 128 = puissance spéculaire

                // 3. Couleur Finale
                // Couleur de base * (Lumière + Ambiante) + Brillance Blanche
                float3 lighting = (mainLight.color * (NdotL + 0.3)) + (specular * mainLight.color);
                float3 finalColor = _BaseColor.rgb * lighting;

                return float4(finalColor, _BaseColor.a);
            }
            ENDHLSL
        }
    }
}