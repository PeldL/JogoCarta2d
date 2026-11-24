// Simple shaded material for the 3D evidence viewer. Works with the URP 2D Renderer
// (which does not light 3D meshes) by computing a fixed key light + rim in the shader.
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
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 viewWS : TEXCOORD1; };

            Varyings vert (Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewWS = GetWorldSpaceViewDir(positionWS);
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                float3 n = normalize(input.normalWS);
                float3 v = normalize(input.viewWS);
                float3 l = normalize(float3(-0.45, 0.75, -0.5));
                float diffuse = saturate(dot(n, l));
                float fill = saturate(dot(n, normalize(float3(0.6, 0.2, -0.4)))) * 0.25;
                float3 h = normalize(l + v);
                float specular = pow(saturate(dot(n, h)), 32) * _Shine;
                float rim = pow(1 - saturate(dot(n, v)), 3) * 0.25;
                float3 color = _BaseColor.rgb * (0.16 + diffuse * 0.9 + fill) + specular + rim * float3(1, 0.92, 0.8);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
