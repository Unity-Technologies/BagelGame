// A custom USS filter: chalk written onto the board and wiped off it. Used from USS with
//   filter: filter("/Assets/UI/Runtime/B00_Themes/T0_Common/Filters/ChalkWipe.asset" <written> <erased>);
// written (0 to 1) writes the element in, a line at a time from the top, each line from its start, the next one
// starting before the last is done, like a hand that's quick with the chalk. erased (0 to 1) sweeps an eraser
// across it from the start of the lines, in a wavy stroke, and leaves a faint smear behind that the end of the
// sweep takes too. At 1 and 0, the element is as it is. Animated with a transition on filter.
Shader "UI Toolkit/Filters/ChalkWipe"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Written ("Written", Float) = 1
        _Erased ("Erased", Float) = 0
        _Line ("Line height (px)", Float) = 26
        _Rtl ("Right to left", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Blend One Zero
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _UIE_OUTPUT_LINEAR

            #include "UnityCG.cginc"
            #include "UnityUIEFilter.cginc"

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                uint rectIndex : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float _Written;
            float _Erased;
            float _Line;
            float _Rtl;

            v2f vert (FilterVertexInput v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.rectIndex = GetFilterRectIndex(v);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Where in the element: across from the start of its lines, and down from its top.
                float4 uvRect = GetFilterUVRect(i.rectIndex);
                float2 p = saturate((i.uv - uvRect.xy) / max(uvRect.zw, 1e-5));
                float across = lerp(p.x, 1.0 - p.x, step(0.5, _Rtl));
                float down = 1.0 - p.y;
                float heightPx = uvRect.w * _MainTex_TexelSize.w;
                half4 col = tex2D(_MainTex, i.uv);

                // Written: line after line. A line starts once the one above is a third done.
                float rows = max(1.0, ceil(heightPx / max(_Line, 4.0)));
                float row = min(floor(down * rows), rows - 1.0);
                const float stagger = 0.33;
                float order = (row * stagger + across) / ((rows - 1.0) * stagger + 1.0);
                const float tip = 0.025;
                float written = saturate((_Written * (1.0 + tip) - order) / tip);

                // Erased: a wavy eraser stroke from the start of the lines, leaving a smear of chalk dust.
                float wave = 0.04 * sin(down * 9.0 + 1.7) + 0.025 * sin(down * 23.0 + 0.4);
                float front = _Erased * 1.35 - 0.2;
                float erased = saturate((front - (across + wave)) / 0.15);
                float2 texel = _MainTex_TexelSize.xy;
                half4 smear = (tex2D(_MainTex, i.uv + float2(texel.x * 4.0, texel.y)) +
                               tex2D(_MainTex, i.uv - float2(texel.x * 4.0, texel.y)) +
                               tex2D(_MainTex, i.uv + float2(texel.x * 9.0, -texel.y * 2.0)) +
                               tex2D(_MainTex, i.uv - float2(texel.x * 9.0, -texel.y * 2.0))) * 0.25;
                half4 dust = smear * 0.22 * saturate(1.0 - _Erased);

                // The filter chain is premultiplied: fading the whole color fades it out.
                col = lerp(col, dust, erased) * written;

                #if _UIE_OUTPUT_LINEAR
                col.rgb = GammaToLinearSpace(col.rgb);
                #endif

                return col;
            }
            ENDCG
        }
    }
}
