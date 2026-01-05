Shader "Custom/ShaderTypeOfShit"
{
    Properties
    {
        // _Color est le nom standard pour que ton script MapGenerator puisse changer la couleur
        _Color("Main Color", Color) = (1, 1, 1, 1)
        _RampThreshold("Seuil Lumière", Range(-1, 1)) = 0.5
        _RampSmooth("Douceur", Range(0.0, 1)) = 0.01
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _RampThreshold;
                float _RampSmooth;
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
                // 1. Calcul de la lumière principale (Soleil)
                Light mainLight = GetMainLight();
                float3 normal = normalize(input.normalWS);
                float3 lightDir = normalize(mainLight.direction);
                
                // Produit scalaire : est-ce que la face regarde le soleil ?
                float NdotL = dot(normal, lightDir);
                
                // 2. Effet Toon : On coupe net la lumière au lieu de faire un dégradé
                float lightIntensity = smoothstep(_RampThreshold - _RampSmooth, _RampThreshold + _RampSmooth, NdotL);
                
                // On garde un tout petit peu de lumière ambiante (+0.2) pour ne pas avoir des ombres noires pures
                float3 lighting = mainLight.color * (lightIntensity + 0.2); 
                
                // 3. Couleur Finale
                float3 finalColor = _Color.rgb * lighting;
                
                return half4(finalColor, 1);
            }
            ENDHLSL
        }
    }
}