Shader "H_Shaders/H_UIRoundedRectOutlineSDF"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [PerRendererData] _Color ("Tint", Color) = (1,1,1,1)

        _OutlineWidthPx ("Outline Width (px)", Float) = 2
        _CornerRadiusPx ("Corner Radius (px)", Float) = 8
        _RectSizeUnits ("Rect Size (units)", Vector) = (100,100,0,0)
        _PadUnits ("Pad (units)", Float) = 0
        _FillCenter ("Fill Center", Float) = 0
        _EdgeSoftness ("Edge Softness", Range(0.5, 3)) = 1

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "RoundedRectSDF"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // 2.5 because the AA uses fwidth (screen-space derivatives).
            #pragma target 2.5

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float4 mask : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float4 _TextureSampleAdd;

            float4 _ClipRect;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;

            float _OutlineWidthPx;
            float _CornerRadiusPx;
            float4 _RectSizeUnits;
            float _PadUnits;
            float _FillCenter;
            float _EdgeSoftness;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float4 vPosition = UnityObjectToClipPos(v.vertex);
                o.vertex = vPosition;
                o.worldPosition = v.vertex;
                o.uv = v.texcoord;

                float2 pixelSize = vPosition.w;
                pixelSize /= float2(1, 1) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

                float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                o.mask = float4(v.vertex.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25 /
                    (0.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize.xy)));

                o.color = v.color * _Color;
                return o;
            }

            inline float sdRoundRect(float2 p, float2 halfSize, float radius)
            {
                // Signed distance to a rounded rectangle centered at origin.
                float2 b = halfSize - radius;
                float2 q = abs(p) - b;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Alpha precision stabilization from UI/Default
                const half alphaPrecision = half(0xff);
                const half invAlphaPrecision = half(1.0 / alphaPrecision);
                i.color.a = round(i.color.a * alphaPrecision) * invAlphaPrecision;

                float2 rectSize = _RectSizeUnits.xy;
                float pad = max(0.0, _PadUnits);
                float2 paddedSize = rectSize + pad * 2.0;

                // Reconstruct local position relative to rect center using UV.
                // UV spans the padded quad, so this stays correct regardless of pivot/layout.
                float2 p = (i.uv - 0.5) * paddedSize;

                float radius = max(0.0, _CornerRadiusPx);
                float outlineWidth = max(0.0, _OutlineWidthPx);

                float2 halfSize = max(0.0, rectSize * 0.5);
                radius = min(radius, min(halfSize.x, halfSize.y));

                float dist = sdRoundRect(p, halfSize, radius);

                float aa = max(0.0001, fwidth(dist) * _EdgeSoftness);

                // Outside-only stroke in range dist ∈ [0, outlineWidth]
                float a0 = smoothstep(0.0 - aa, 0.0 + aa, dist);                 // 0 inside, 1 outside
                float a1 = smoothstep(outlineWidth - aa, outlineWidth + aa, dist); // 0 within band, 1 beyond
                float stroke = saturate(a0 - a1);

                float fill = 1.0 - a0;
                float shapeAlpha = lerp(stroke, max(stroke, fill), saturate(_FillCenter));

                fixed4 tex = tex2D(_MainTex, i.uv) + _TextureSampleAdd;
                fixed4 col = i.color * tex;
                col.a *= shapeAlpha;

#ifdef UNITY_UI_CLIP_RECT
                half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(i.mask.xy)) * i.mask.zw);
                col.a *= m.x * m.y;
#endif

#ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
#endif

                col.rgb *= col.a;
                return col;
            }
            ENDCG
        }
    }

    Fallback "UI/Default"
}
