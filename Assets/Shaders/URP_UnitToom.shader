Shader "Custom/URP_Unit_Toon"
{
    Properties
    {
        [Header(Base)]
        [MainTexture] _BaseMap("Texture", 2D) = "white" {}
        [MainColor] _BaseColor("Color Tint", Color) = (1,1,1,1)
        
        [Header(Team Highlight)]
        [HDR] _RimColor("Rim Color", Color) = (0,0,0,1)
        _RimPower("Rim Power (Sharpness)", Range(0.5, 8.0)) = 3.0

        [Header(Outline)]
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0, 0.05)) = 0.02
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 300

        // --- PASSE 1 : L'OUTLINE (Contour Noir) ---
        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _RimColor;
                float _RimPower;
                float4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 newPos = input.positionOS.xyz + (input.normalOS * _OutlineWidth);
                output.positionCS = TransformObjectToHClip(newPos);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }

        // --- PASSE 2 : LE MODÈLE (Texture + Rim Light) ---
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
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

            // --- CORRECTION ICI : NOMS DE VARIABLES STANDARDS ---
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;  // <--- C'était ici l'erreur (_BaseMap_ST_2)
                float4 _BaseColor;
                float4 _RimColor;
                float _RimPower;
            CBUFFER_END
            // ----------------------------------------------------

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, float4(1,1,1,1));

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                
                // Cette ligne avait besoin de _BaseMap_ST défini correctement
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap); 
                
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(output.positionWS);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                
                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                float3 normal = normalize(input.normalWS);
                
                float NdotL = max(0, dot(normal, lightDir));
                float3 diffuse = mainLight.color * (NdotL + 0.4); 

                float3 viewDir = normalize(input.viewDirWS);
                float rim = 1.0 - saturate(dot(viewDir, normal));
                float3 rimEmission = _RimColor.rgb * pow(rim, _RimPower);

                float3 finalColor = (texColor.rgb * diffuse) + rimEmission;

                return float4(finalColor, texColor.a);
            }
            ENDHLSL
        }
    }
}