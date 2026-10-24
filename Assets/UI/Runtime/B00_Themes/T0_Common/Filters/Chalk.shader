// A custom USS filter: whatever it's on looks drawn in chalk. It wears the element's pixels away in a
// grain, more along the slant a stick of chalk drags across a board, so lines and letters come out
// powdery with the board showing through. Used from USS with
//   filter: filter("/Assets/UI/Runtime/B00_Themes/T0_Common/Filters/Chalk.asset" <strength> <grain>);
// strength is how much wears away (0 to 1), grain the size of the grain in pixels.
Shader "UI Toolkit/Filters/Chalk"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Strength ("Strength", Float) = 0.5
        _Grain ("Grain (px)", Float) = 1.5
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
            float _Strength;
            float _Grain;

            v2f vert (FilterVertexInput v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.rectIndex = GetFilterRectIndex(v);
                return o;
            }

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // Value noise, 0 to 1.
            float Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash(i);
                float b = Hash(i + float2(1, 0));
                float c = Hash(i + float2(0, 1));
                float d = Hash(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Pixels from the corner of the element's texture, so the grain sticks to the element wherever it
                // is drawn. The texture covers the element's bounding box, children included: an element whose
                // children move past its edges clips them (overflow: hidden), or the grain slides as they move.
                float4 uvRect = GetFilterUVRect(i.rectIndex);
                float2 px = (i.uv - uvRect.xy) * _MainTex_TexelSize.zw;
                float2 p = px / max(_Grain, 0.5);

                // Fine grain, and streaks stretched along a slant like a stick of chalk dragged sideways.
                float grain = Noise(p) * 0.6 + Noise(p * 2.7 + 17.0) * 0.4;
                float2 slant = float2(p.x * 0.8 + p.y * 0.35, p.y * 0.8 - p.x * 0.35);
                float streaks = Noise(slant * float2(0.18, 1.6));
                float wear = saturate(grain * 0.7 + streaks * 0.45 - 0.2);
                float keep = 1.0 - saturate(_Strength) * smoothstep(0.35, 0.8, wear);

                // The filter chain is premultiplied: fading the whole color wears it away.
                half4 col = tex2D(_MainTex, i.uv) * keep;

                #if _UIE_OUTPUT_LINEAR
                col.rgb = GammaToLinearSpace(col.rgb);
                #endif

                return col;
            }
            ENDCG
        }
    }
}
