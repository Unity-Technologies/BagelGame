
Shader "Bagel/ProceduralHedge"
{
    Properties
    {
        _HedgeLight ("Hedge Light Color", Color) = (0.34, 0.65, 0.25, 1)
        _HedgeDark  ("Hedge Dark Color",  Color) = (0.12, 0.30, 0.10, 1)
        _BacklightColor ("Backlight Color", Color) = (0.35, 0.7, 0.35, 1)

        _LeafScale ("Leaf Scale (world)", Float) = 2.5
        _LeafSharpness ("Leaf Sharpness", Range(0.5, 4)) = 2.0
        _LeafContrast ("Leaf Contrast", Range(0, 2)) = 0.9
        _LeafNormalStrength ("Leaf Normal Strength", Range(0, 2)) = 0.9

        _LumpScale ("Lump Scale (world)", Float) = 0.4
        _LumpAmount ("Lump Amount", Range(0, 1)) = 0.35

        _RimStrength ("Rim Strength", Range(0, 1)) = 0.25
        _BacklightStrength ("Backlight Strength", Range(0, 2)) = 0.6

        _Smoothness ("Smoothness", Range(0,1)) = 0.25
        _Metallic ("Metallic", Range(0,1)) = 0.0

        _TriplanarSharpness ("Triplanar Sharpness", Range(0.5,8)) = 4.0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 300
        Cull Back
        ZWrite On
        ZTest LEqual

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // URP keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile _ _REFLECTION_PROBE_BOX_PROJECTION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 posWS       : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                half   fogCoord    : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
                float2 screenUV    : TEXCOORD4;
                float3 tangentWS   : TEXCOORD5;
                float3 bitangentWS : TEXCOORD6;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _HedgeLight;
                float4 _HedgeDark;
                float4 _BacklightColor;

                float _LeafScale;
                float _LeafSharpness;
                float _LeafContrast;
                float _LeafNormalStrength;

                float _LumpScale;
                float _LumpAmount;

                float _RimStrength;
                float _BacklightStrength;

                float _Smoothness;
                float _Metallic;
                float _TriplanarSharpness;
            CBUFFER_END

            // -------------------- Utility noise --------------------
            float3 hash33(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.xxy + p.yzz) * p.zyx);
            }

            float hash21(float2 p)
            {
                float3 p3 = frac(float3(p.x, p.y, p.x) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            // Value noise
            float noise2(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = hash21(i + float2(0,0));
                float b = hash21(i + float2(1,0));
                float c = hash21(i + float2(0,1));
                float d = hash21(i + float2(1,1));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // Worley / Voronoi F1
            float worley(float2 p, out float2 cellCenter, out float2 cellID)
            {
                float2 ip = floor(p);
                float minD = 10.0;
                float2 nearest = 0;
                float2 id = 0;
                [unroll] for (int y=-1; y<=1; y++)
                {
                    [unroll] for (int x=-1; x<=1; x++)
                    {
                        float2 g = float2(x,y);
                        float2 lattice = ip + g;
                        float2 rand = hash33(float3(lattice, 0)).xy;
                        float2 c = lattice + rand; // cell center
                        float d = distance(c, p);
                        if (d < minD) { minD = d; nearest = c; id = lattice; }
                    }
                }
                cellCenter = nearest;
                cellID = id;
                return minD;
            }

            // Leaf pattern on 2D uv (procedural)
            // Returns: brightness [0..1], and a pseudo-normal perturbation vector in uv space
            float3 leafPattern(float2 uv, float scale, float sharp, float contrast, out float2 grad)
            {
                float2 C, ID;
                float d = worley(uv * scale, C, ID);    // distance to nearest cell center
                // Shape leaf: invert distance and sharpen
                float leaf = saturate(pow(saturate(1.0 - d), sharp));
                // Add slight random rotation/gradient per cell for anisotropy
                float ang = hash21(ID) * 6.2831853;
                float2 dir = float2(cos(ang), sin(ang));
                // Brightness ramp (cartoony)
                float val = saturate(leaf * (0.6 + 0.4 * noise2(uv * scale * 0.5)));
                val = pow(val, 1.0 + contrast * 0.5);

                // Gradient approximation for normal
                float e = 0.001;
                float d1 = saturate(pow(saturate(1.0 - worley((uv + float2(e,0)) * scale, C, ID)), sharp));
                float d2 = saturate(pow(saturate(1.0 - worley((uv + float2(0,e)) * scale, C, ID)), sharp));
                grad = float2(d1 - leaf, d2 - leaf) + dir * 0.05;

                return float3(val, leaf, 0);
            }

            // Triplanar blend helper
            float3 triplanarWeights(float3 n)
            {
                n = abs(n);
                n = pow(n, _TriplanarSharpness);
                return n / max(dot(n, 1.0.xxx), 1e-5);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   nInputs   = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionHCS = posInputs.positionCS;
                OUT.posWS       = posInputs.positionWS;
                OUT.normalWS    = nInputs.normalWS;
                OUT.tangentWS   = nInputs.tangentWS.xyz;
                OUT.bitangentWS = nInputs.bitangentWS.xyz;
                OUT.fogCoord    = ComputeFogFactor(posInputs.positionCS.z);
                OUT.shadowCoord = TransformWorldToShadowCoord(posInputs.positionWS);
                OUT.screenUV    = GetNormalizedScreenSpaceUV(OUT.positionHCS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 posWS = IN.posWS;
                float3 nWS0 = normalize(IN.normalWS);
                float3 viewWS = GetWorldSpaceViewDir(posWS);

                // --------- Triplanar UVs ---------
                float3 w = triplanarWeights(nWS0);
                float2 uvX = posWS.zy;
                float2 uvY = posWS.xz;
                float2 uvZ = posWS.xy;

                float2 g1, g2, g3;
                float3 p1 = leafPattern(uvX, 1.0 / max(_LeafScale, 1e-4), _LeafSharpness, _LeafContrast, g1);
                float3 p2 = leafPattern(uvY, 1.0 / max(_LeafScale, 1e-4), _LeafSharpness, _LeafContrast, g2);
                float3 p3 = leafPattern(uvZ, 1.0 / max(_LeafScale, 1e-4), _LeafSharpness, _LeafContrast, g3);

                float leafVal = dot(float3(p1.x, p2.x, p3.x), w);
                float2 grad = g1 * w.x + g2 * w.y + g3 * w.z;

                // Large-scale "lumps" to break uniformity
                float lump = noise2(posWS.xz / max(_LumpScale, 1e-3));
                lump = lerp(0.5, lump, _LumpAmount);

                // Color ramp (cartoon two-tone with mild variation)
                float3 lightCol = _HedgeLight.rgb;
                float3 darkCol  = _HedgeDark.rgb;
                float3 albedo   = lerp(darkCol, lightCol, saturate(leafVal * 0.85 + lump * 0.25));

                // Micro normal from UV gradient
                float3 t = normalize(IN.tangentWS);
                float3 b = normalize(IN.bitangentWS);
                float3 nMicroTS = normalize(float3(-grad.x, -grad.y, 1.0));
                float3 nMicroWS = normalize(mul(float3x3(t,b,nWS0), nMicroTS));
                float3 nWS = normalize(lerp(nWS0, nMicroWS, _LeafNormalStrength));

                // Ambient
                InputData inputData = (InputData)0;
                inputData.positionWS = posWS;
                inputData.normalWS = nWS;
                inputData.viewDirectionWS = viewWS;
                inputData.shadowCoord = IN.shadowCoord;
                inputData.fogCoord = IN.fogCoord;
                inputData.normalizedScreenSpaceUV = IN.screenUV;
                inputData.bakedGI = SampleSH(nWS);

                SurfaceData surf = (SurfaceData)0;
                surf.albedo     = albedo;
                surf.metallic   = _Metallic;
                surf.specular   = 0;
                surf.smoothness = _Smoothness;
                surf.normalTS   = float3(0,0,1);
                surf.emission   = 0;
                surf.occlusion  = 1;
                surf.alpha      = 1;

                half4 col = UniversalFragmentPBR(inputData, surf);

                // Rim light (soft cartoon edge)
                float rim = pow(1.0 - saturate(dot(normalize(viewWS), nWS)), 3.0) * _RimStrength;
                col.rgb += rim * _HedgeLight.rgb * 0.5;

                // Backlight (fake subsurface for leaves)
                Light mainLight = GetMainLight(IN.shadowCoord);
                float back = saturate(dot(-mainLight.direction, nWS));
                col.rgb += back * _BacklightStrength * _BacklightColor.rgb;

                // Fog
                col.rgb = MixFog(col.rgb, inputData.fogCoord);
                return col;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
