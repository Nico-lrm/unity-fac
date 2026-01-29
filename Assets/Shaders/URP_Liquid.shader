Shader "Custom/URP_Liquid"
{
    Properties
    {
        [Header(Base)]
        [MainTexture] _BaseMap("Noise Texture", 2D) = "white" {}
        [MainColor] _BaseColor("Color Tint", Color) = (1,0.2,0,1) // Orange/Rouge par défaut
        
        // --- NOUVEAU : Paramètre de tuilage ---
        [Header(Texture Settings)]
        _Tiling ("Texture Tiling (Repetition)", Float) = 10.0
        // --------------------------------------

        [Header(Movement)]
        _SpeedX ("Scroll Speed X", Range(-1, 1)) = 0.05
        _SpeedY ("Scroll Speed Y", Range(-1, 1)) = 0.05
        _WaveHeight ("Wave Height", Range(0, 1)) = 0.05
        _WaveSpeed ("Wave Speed", Range(0, 5)) = 0.5

        [Header(Glow)]
        [HDR] _EmissionColor("Emission Color", Color) = (1,0.5,0,1) // Orange lumineux
        _EmissionPower("Emission Power", Range(0, 10)) = 3.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                // --- NOUVEAU VARIABLE ---
                float _Tiling;
                // ------------------------
                float _SpeedX;
                float _SpeedY;
                float _WaveHeight;
                float _WaveSpeed;
                float4 _EmissionColor;
                float _EmissionPower;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // 1. Vagues (Vertex Displacement)
                float3 pos = input.positionOS.xyz;
                float time = _Time.y * _WaveSpeed;
                // Pour un grand plan, il faut aussi ajuster la fréquence des vagues selon la position
                float wave = sin(pos.x * 0.5 + time) * cos(pos.z * 0.5 + time) * _WaveHeight;
                pos.y += wave;

                output.positionWS = TransformObjectToWorld(pos);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                
                // --- 2. Calcul des UVs (Modifié) ---
                // On multiplie les UVs de base par le facteur de Tiling
                float2 tiledUV = input.uv * _Tiling;

                // Puis on applique le scrolling sur ces UVs déjà répétés
                output.uv.x = tiledUV.x + _Time.y * _SpeedX;
                output.uv.y = tiledUV.y + _Time.y * _SpeedY;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float4 noise = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                
                float3 albedo = _BaseColor.rgb * noise.rgb;
                // On utilise le canal Rouge de la texture pour définir où ça brille le plus (les fissures)
                // saturate permet d'augmenter le contraste du glow
                float glowMask = saturate(noise.r * 1.5); 
                float3 emission = _EmissionColor.rgb * glowMask * _EmissionPower;

                return float4(albedo + emission, _BaseColor.a);
            }
            ENDHLSL
        }
    }
}