Shader "Custom/URP_SelectionCircle"
{
    Properties
    {
        [Header(Mode Selection)]
        [Toggle(_USE_TEXTURE)] _UseTexture("Use Texture Mode", Float) = 0
        
        [Header(Base Settings)]
        [MainColor] _BaseColor("Color", Color) = (0,0.5,1,1)
        _PulseSpeed("Pulse Speed", Range(0, 10)) = 3.0

        [Header(Texture Settings)]
        [MainTexture] _BaseMap("Magic Texture (Black BG)", 2D) = "black" {}
        _RotationSpeed("Rotation Speed", Range(-50, 50)) = 30.0

        [Header(Ring Settings)]
        _Thickness("Ring Thickness", Range(0.01, 0.5)) = 0.05
        _Radius("Ring Radius", Range(0, 0.5)) = 0.45

        [Header(Blending)]
        // Permet de changer le mode de fusion dans l'inspecteur
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Source Blend", Float) = 5  // Par défaut SrcAlpha
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dest Blend", Float) = 10 // Par défaut OneMinusSrcAlpha
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        
        // On utilise les variables définies dans les Properties pour le Blend
        Blend [_SrcBlend] [_DstBlend]
        
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // Active la variante de shader pour le Toggle
            #pragma shader_feature _USE_TEXTURE

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
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float _PulseSpeed;
                // Texture vars
                float _RotationSpeed;
                // Ring vars
                float _Thickness;
                float _Radius;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                
                // Calcul de la rotation UV (Utile seulement si _USE_TEXTURE est actif, mais peu coûteux)
                float2 centeredUV = input.uv - 0.5;
                
                // Si on utilise la texture, on applique la rotation
                #if _USE_TEXTURE
                    float angle = _Time.y * _RotationSpeed * 0.01745; // Degrés vers radians
                    float s, c;
                    sincos(angle, s, c);
                    float2x2 rotationMatrix = float2x2(c, -s, s, c);
                    output.uv = mul(centeredUV, rotationMatrix) + 0.5;
                #else
                    output.uv = input.uv; // Pas de rotation pour le cercle simple
                #endif

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Pulsation commune aux deux modes
                float pulse = 0.5 + 0.5 * sin(_Time.y * _PulseSpeed);

                #if _USE_TEXTURE
                    // --- MODE 1 : TEXTURE MAGIQUE ---
                    float4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                    // On retourne la texture * couleur * pulsation
                    return texColor * _BaseColor * pulse;

                #else
                    // --- MODE 2 : CERCLE PROCÉDURAL ---
                    float2 center = input.uv - 0.5;
                    float dist = length(center);

                    float delta = 0.01;
                    float outer = smoothstep(_Radius + _Thickness/2 + delta, _Radius + _Thickness/2, dist);
                    float inner = smoothstep(_Radius - _Thickness/2, _Radius - _Thickness/2 - delta, dist);
                    float ring = outer * inner;

                    return float4(_BaseColor.rgb, ring * _BaseColor.a * (0.5 + 0.5 * pulse));
                #endif
            }
            ENDHLSL
        }
    }
}