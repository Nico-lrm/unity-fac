Shader "Custom/URP_Unit_TeamColor"
{
    Properties
    {
        [MainTexture] _BaseMap("Texture", 2D) = "white" {}
        [MainColor] _BaseColor("Color Tint", Color) = (1,1,1,1)
        
        [Header(Team Highlight)]
        [HDR] _RimColor("Rim Color", Color) = (0,0,0,1) // HDR permet de faire briller fort
        _RimPower("Rim Power (Sharpness)", Range(0.5, 8.0)) = 2.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // Indispensable pour que les animations fonctionnent !
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
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
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float3 viewDirWS : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _RimColor;
                float _RimPower;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // Calculs de base pour URP
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, float4(1,1,1,1)); // Tangent dummy

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                
                // Calcul de la direction de vue pour le Rim Light
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(output.positionWS);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 1. Texture & Lumière de base
                float4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                float3 normal = normalize(input.normalWS);
                
                float NdotL = max(0, dot(normal, lightDir));
                float3 diffuse = mainLight.color * (NdotL + 0.3); // +0.3 pour l'ambiance

                // 2. EFFET RIM LIGHT (Le Contour Magique)
                float3 viewDir = normalize(input.viewDirWS);
                // On calcule l'angle entre le regard et la surface
                // Plus la surface fuit le regard (bords), plus c'est fort.
                float rim = 1.0 - saturate(dot(viewDir, normal));
                // On affine le contour avec la puissance
                float3 rimEmission = _RimColor.rgb * pow(rim, _RimPower);

                // 3. Combinaison
                float3 finalColor = (texColor.rgb * diffuse) + rimEmission;

                return float4(finalColor, texColor.a);
            }
            ENDHLSL
        }
    }
}