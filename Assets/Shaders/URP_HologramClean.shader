Shader "Custom/URP_HologramClean"
{
    Properties
    {
        [Header(Main Settings)]
        [MainTexture] _BaseMap("Border Texture", 2D) = "white" {}
        [MainColor] _BaseColor("Color Tint", Color) = (0,1,1,1)
        
        [Header(Transparency)]
        _MainAlpha("Global Transparency", Range(0, 1)) = 0.6 // Plus opaque pour mieux voir la forme
        _GlowPower("Emission Power", Range(1, 3)) = 1.5
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        
        // --- CHANGEMENT 1 : BLEND STANDARD ---
        // Au lieu d'additionner la lumière (qui fait blanc), on fait de la vraie transparence
        Blend SrcAlpha OneMinusSrcAlpha
        
        // --- CHANGEMENT 2 : CACHER L'INTÉRIEUR ---
        // On ne dessine que la face avant des cubes pour éviter la "soupe" visuelle
        Cull Back 
        
        // ZWrite Off est bien pour les hologrammes, mais On aide à comprendre la forme
        // On laisse Off pour le style "Fantôme"
        ZWrite Off 

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _MainAlpha;
                float _GlowPower;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);

                // La texture définit la brillance (Blanc = Brillant, Noir = Pas brillant)
                // On garde la couleur choisie (Cyan, Rouge...)
                float3 finalColor = _BaseColor.rgb * _GlowPower;
                
                // --- CALCUL DE L'ALPHA ---
                // Si la texture est blanche (bordure), on est très opaque
                // Si la texture est noire (centre), on utilise la transparence globale
                float alpha = texColor.r + _MainAlpha; 
                
                // On clip l'alpha pour éviter de dépasser 1
                alpha = clamp(alpha, 0, 1);

                // On applique la couleur de la texture pour garder le dessin de la grille
                finalColor *= texColor.rgb + 0.2; // +0.2 pour que le fond ne soit pas noir pur

                return float4(finalColor, alpha * _BaseColor.a);
            }
            ENDHLSL
        }
    }
}