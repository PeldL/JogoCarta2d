// Thick dark ink outline for the 3D clues (inverted hull). Each primitive is pushed out from
// its own centre, so boxes keep closed corners; the width is capped by the part's thinnest
// side, so needles and threads do not drown in ink.
Shader "CaseZero/Evidence3DOutline"
{
    Properties
    {
        _OutlineColor ("Outline colour", Color) = (0.02, 0, 0.006, 1)
        _Width ("Width", Float) = 0.035
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        Pass
        {
            ZWrite On
            Cull Front
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _Width;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vert (Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 centreWS = TransformObjectToWorld(float3(0, 0, 0));
                float3 normalWS = normalize(TransformObjectToWorldNormal(input.normalOS));
                float3 outward = positionWS - centreWS;
                float3 direction = length(outward) > 1e-5 ? normalize(outward) : normalWS;
                direction = normalize(lerp(normalWS, direction, 0.55));
                float3x3 m = (float3x3)GetObjectToWorldMatrix();
                float thinnest = min(length(m._m00_m10_m20), min(length(m._m01_m11_m21), length(m._m02_m12_m22)));
                float width = min(_Width, thinnest * 0.32);
                output.positionCS = TransformWorldToHClip(positionWS + direction * width);
                return output;
            }

            half4 frag (Varyings input) : SV_Target { return _OutlineColor; }
            ENDHLSL
        }
    }
}
