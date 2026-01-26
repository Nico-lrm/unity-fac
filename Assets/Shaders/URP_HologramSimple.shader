Shader "Custom/URP_HologramSimple"
{
    Properties
    {
        [Header(Main Settings)]
        // Ta texture carrée avec la croix
        [MainTexture] _BaseMap("Holo Texture (Black BG)", 2D) = "black" {}
        
        // La couleur globale (Cyan, Rouge, Vert...)
        [MainColor] _BaseColor("Tint Color", Color) = (0,1,1,1)
        
        // Pour régler la puissance du "néon"
        _GlowIntensity("Glow Intensity", Range(0.5, 5)) = 2.0
        
        [Header(Animation)]
        // Vitesse du petit clignotement
        _FlickerSpeed("Flicker Speed", Float) = 15.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        
        // Blend One One = Additif Pur.
        // Le noir de ta texture (valeur 0) n'ajoutera rien -> Transparent.
        // Les couleurs s'additionnent -> Effet lumineux intense.
        Blend One One
        
        ZWrite Off // On voit à travers les objets derrière
        Cull Off   // IMPORTANT : On voit l'intérieur des cubes (les faces arrières)

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
                float _GlowIntensity;
                float _FlickerSpeed;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            Varyings vert(Attributes input)
            {
                Varyings output;
                // Conversion standard de la position
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                // Application du Tiling/Offset de la texture si besoin
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 1. Lecture de ta texture
                float4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);

                // 2. Calcul du Scintillement (Flicker)
                // Une petite vibration rapide de l'intensité (entre 0.9 et 1.1 environ)
                float flicker = 1.0 + (sin(_Time.y * _FlickerSpeed) * 0.1);

                // 3. Combinaison Finale
                // Couleur de la texture * La Teinte choisie * L'intensité * Le scintillement
                float3 finalRGB = texColor.rgb * _BaseColor.rgb * _GlowIntensity * flicker;

                // En mode Blend One One, l'Alpha en sortie n'est pas très important,
                // mais on retourne 1 par précaution. La transparence vient du noir de la texture.
                return float4(finalRGB, 1.0);
            }
            ENDHLSL
        }
    }
}