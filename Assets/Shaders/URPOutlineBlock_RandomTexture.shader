Shader "Custom/URPOutlineBlock_RandomTexture"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map (Texture)", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color (Tint)", Color) = (1,1,1,1)
        
        [Header(Randomization)]
        _TextureZoom ("Texture Zoom", Range(0.01, 1)) = 0.25
        [Toggle] _UseRandomOffset ("Randomize Position", Float) = 1
        
        [Header(Outline)]
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0, 0.1)) = 0.03
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        // --- PASSE 1 : CONTOUR (Inchangée) ---
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
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _OutlineColor;
                float _OutlineWidth;
                float _TextureZoom;
                float _UseRandomOffset;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 newPositionOS = input.positionOS.xyz + (input.normalOS * _OutlineWidth);
                output.positionCS = TransformObjectToHClip(newPositionOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target { return _OutlineColor; }
            ENDHLSL
        }

        // --- PASSE 2 : TEXTURE ZOOMÉE & ALÉATOIRE ---
        Pass
        {
            Name "MainObject"
            Tags { "LightMode" = "UniversalForward" }
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
                float3 normalWS : TEXCOORD0; 
                float2 uv : TEXCOORD1; 
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _TextureZoom;
                float _UseRandomOffset;
            CBUFFER_END

            // Fonction Pseudo-Aléatoire simple basée sur la position X/Z
            float2 GetRandomOffset(float2 positionXZ)
            {
                float2 noise = float2(
                    dot(positionXZ, float2(12.9898, 78.233)),
                    dot(positionXZ, float2(39.346, 11.135))
                );
                return frac(sin(noise) * 43758.5453);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // 1. Position Standard
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                // 2. Calcul des UVs "Intelligents"
                // On récupère la position du cube dans le monde
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                
                // On applique le Zoom (Tiling)
                // Si Zoom = 0.25, on affiche 1/4 de la texture sur le cube (donc c'est gros et net)
                float2 zoomedUV = input.uv * _TextureZoom;

                // On applique le décalage Aléatoire
                float2 randomOffset = float2(0,0);
                if (_UseRandomOffset > 0.5)
                {
                    // On utilise la position (arrondie à l'unité pour que tout le cube ait le même offset)
                    // unity_ObjectToWorld._m03_m23 contient la position X et Z de l'objet
                    float2 objectPosition = float2(unity_ObjectToWorld._m03, unity_ObjectToWorld._m23);
                    randomOffset = GetRandomOffset(objectPosition);
                }

                output.uv = zoomedUV + randomOffset;
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                Light mainLight = GetMainLight();
                float3 normal = normalize(input.normalWS);
                float3 lightDir = normalize(mainLight.direction);
                
                float NdotL = max(0, dot(normal, lightDir));
                float3 lighting = mainLight.color * (NdotL + 0.4); 

                float4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                return half4(texColor.rgb * _BaseColor.rgb * lighting, 1);
            }
            ENDHLSL
        }
    }
}