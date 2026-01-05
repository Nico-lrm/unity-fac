Shader "Custom/URPOutlineBlock"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0, 0.1)) = 0.03
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        // --- PASSE 1 : CONTOUR ---
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
                float4 _OutlineColor;
                float _OutlineWidth;
                float4 _BaseColor;
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

        // --- PASSE 2 : OBJET ÉCLAIRÉ (MAT) ---
        Pass
        {
            Name "MainObject"
            Tags { "LightMode" = "UniversalForward" } // Accepte la lumière
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Calcul simple de la lumière (Matte, pas de brillant)
                Light mainLight = GetMainLight();
                float3 normal = normalize(input.normalWS);
                float3 lightDir = normalize(mainLight.direction);
                
                // Produit scalaire : Face à la lumière ou pas ?
                float NdotL = max(0, dot(normal, lightDir));
                
                // Couleur finale = Couleur de base * (Lumière + Ambiante un peu boostée)
                float3 lighting = mainLight.color * (NdotL + 0.3); 
                return half4(_BaseColor.rgb * lighting, 1);
            }
            ENDHLSL
        }
    }
}