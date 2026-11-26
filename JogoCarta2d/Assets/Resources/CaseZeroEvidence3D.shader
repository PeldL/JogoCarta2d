// Shaded material for the 3D evidence viewer, styled after the game's clue drawings:
// every colour is mapped onto the same crimson ink ramp (near-black maroon to pale rose),
// lit in flat toon bands with a red rim glow and a little paper grain. Works with the URP
// 2D Renderer (which does not light 3D meshes) by computing a fixed key light in the shader.
Shader "CaseZero/Evidence3D"
{
    Properties
    {
        _BaseColor ("Color", Color) = (1, 1, 1, 1)
        _Shine ("Shine", Range(0, 1)) = 0.25
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        Pass
        {
            ZWrite On
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Shine;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 viewWS : TEXCOORD1; float3 positionWS : TEXCOORD2; };

            Varyings vert (Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewWS = GetWorldSpaceViewDir(positionWS);
                output.positionWS = positionWS;
                return output;
            }

            // Crimson ink ramp shared by every clue drawing.
            float3 InkRamp(float value)
            {
                float3 c0 = float3(0.035, 0.005, 0.01);
                float3 c1 = float3(0.22, 0.025, 0.04);
                float3 c2 = float3(0.5, 0.06, 0.09);
                float3 c3 = float3(0.8, 0.34, 0.36);
                value = saturate(value);
                if (value < 0.34) return lerp(c0, c1, value / 0.34);
                if (value < 0.72) return lerp(c1, c2, (value - 0.34) / 0.38);
                return lerp(c2, c3, (value - 0.72) / 0.28);
            }

            float Hash(float3 p)
            {
                return frac(sin(dot(p, float3(12.9898, 78.233, 37.719))) * 43758.5453);
            }

            half4 frag (Varyings input) : SV_Target
            {
                float3 n = normalize(input.normalWS);
                float3 v = normalize(input.viewWS);
                float3 l = normalize(float3(-0.45, 0.75, -0.5));
                float lambert = dot(n, l) * 0.5 + 0.5;
                // Three flat bands with soft edges, like the cel shading in the drawings.
                float shade = 0.42 + smoothstep(0.40, 0.46, lambert) * 0.33 + smoothstep(0.70, 0.75, lambert) * 0.25;
                float value = dot(_BaseColor.rgb, float3(0.3, 0.59, 0.11));
                value = pow(value, 0.95) * shade * 0.86;
                // Fine grain so flat faces read like painted paper.
                value += (Hash(floor(input.positionWS * 140)) - 0.5) * 0.05;
                float3 color = InkRamp(value);
                float3 h = normalize(l + v);
                float specular = smoothstep(0.92, 0.95, dot(n, h)) * _Shine;
                float rim = smoothstep(0.55, 0.8, 1 - saturate(dot(n, v)));
                color += specular * float3(1, 0.78, 0.76) * 0.55 + rim * float3(0.7, 0.05, 0.08) * 0.35;
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
