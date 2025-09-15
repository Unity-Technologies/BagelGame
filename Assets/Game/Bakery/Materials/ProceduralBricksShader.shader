
Shader "Bagel/ProceduralBricks"
{
    Properties
    {
        _Tiling ("Tiling (x,y bricks)", Vector) = (8,4,0,0)
        _MortarWidth ("Mortar Width", Range(0,0.3)) = 0.08
        _Bevel ("Bevel", Range(0,0.3)) = 0.07
        _Stagger ("Row Stagger", Range(0,1)) = 0.5
        _BrickColorA ("Brick Color A", Color) = (0.55,0.21,0.18,1)
        _BrickColorB ("Brick Color B", Color) = (0.50,0.18,0.15,1)
        _MortarColor ("Mortar Color", Color) = (0.75,0.75,0.75,1)
        _ColorVariation ("Color Variation", Range(0,0.6)) = 0.15
        _HeightStrength ("Height Strength", Range(0,2)) = 0.4
        _NormalStrength ("Normal Strength", Range(0,3)) = 1.0
        _BrickSmoothness ("Brick Smoothness", Range(0,1)) = 0.4
        _MortarSmoothness ("Mortar Smoothness", Range(0,1)) = 0.15
        _RandomSeed ("Random Seed", Float) = 1.0
        _Metallic ("Metallic", Range(0,1)) = 0.0

        _AAScale ("AA Scale", Range(0.1,2)) = 1.0
        _AAClampFrac ("AA Clamp (x MortarWidth)", Range(0,1)) = 0.45
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // URP lighting variants
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
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 tangentWS   : TEXCOORD2;
                float3 bitangentWS : TEXCOORD3;
                float3 posWS       : TEXCOORD4;
                half   fogCoord    : TEXCOORD5;
                float4 shadowCoord : TEXCOORD6;
                float2 screenUV    : TEXCOORD7;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Tiling;
                float  _MortarWidth;
                float  _Bevel;
                float  _Stagger;
                float4 _BrickColorA;
                float4 _BrickColorB;
                float4 _MortarColor;
                float  _ColorVariation;
                float  _HeightStrength;
                float  _NormalStrength;
                float  _BrickSmoothness;
                float  _MortarSmoothness;
                float  _RandomSeed;
                float  _Metallic;
                float  _AAScale;
                float  _AAClampFrac;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   nInputs   = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionHCS = posInputs.positionCS;
                OUT.posWS       = posInputs.positionWS;
                OUT.uv          = IN.uv;
                OUT.normalWS    = nInputs.normalWS;
                OUT.tangentWS   = nInputs.tangentWS.xyz;
                OUT.bitangentWS = nInputs.bitangentWS.xyz;
                OUT.fogCoord    = ComputeFogFactor(posInputs.positionCS.z);
                OUT.shadowCoord = TransformWorldToShadowCoord(posInputs.positionWS);
                OUT.screenUV    = GetNormalizedScreenSpaceUV(OUT.positionHCS);
                return OUT;
            }

            float hash21(float2 p, float seed)
            {
                float h = sin(dot(p, float2(12.9898, 78.233)) + seed) * 43758.5453;
                return frac(h);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // TBN
                float3 t = normalize(IN.tangentWS);
                float3 b = normalize(IN.bitangentWS);
                float3 n0 = normalize(IN.normalWS);
                float3x3 TBN = float3x3(t, b, n0);

                // Base UV and tiling
                float2 uv = IN.uv * _Tiling.xy;

                // Row index & stagger
                float row    = floor(uv.y);
                float parity = fmod(row, 2.0);
                uv.x += parity * _Stagger;

                // Cell UV inside each brick
                float2 cellUV = frac(uv);

                // Edge distance to nearest border of the brick
                float dx = min(cellUV.x, 1.0 - cellUV.x);
                float dy = min(cellUV.y, 1.0 - cellUV.y);
                float edgeDist = min(dx, dy);

                // Better AA: use width in *edgeDist* domain
                float w = fwidth(edgeDist) * _AAScale;
                w = min(w, _MortarWidth * _AAClampFrac); // avoid over-expanding mortar

                // Mortar mask (1 in mortar, 0 in brick)
                float mortar    = 1.0 - smoothstep(_MortarWidth, _MortarWidth + w, edgeDist);
                float brickMask = 1.0 - mortar;

                // Per-brick random (use integer brick ID)
                float2 brickID = floor(uv);
                float r = hash21(brickID, _RandomSeed);

                // Brick color variation
                float variation = ((r - 0.5) * 2.0) * _ColorVariation;
                float3 baseCol  = lerp(_BrickColorA.rgb, _BrickColorB.rgb, r);
                float3 brickCol = baseCol * (1.0 + variation);

                // Height / bevel (0 near mortar → 1 at brick center)
                float h = smoothstep(_MortarWidth, _MortarWidth + _Bevel, edgeDist) * brickMask;

                // Normal from height (Tangent space)
                float dhdx = ddx(h);
                float dhdy = ddy(h);
                float3 nTS = normalize(float3(-dhdx * _HeightStrength * _NormalStrength,
                                              -dhdy * _HeightStrength * _NormalStrength,
                                              1.0));

                // Base color & smoothness mix
                float3 albedo = lerp(_MortarColor.rgb, brickCol, brickMask);
                float smooth  = lerp(_MortarSmoothness, _BrickSmoothness, brickMask);
                float metallic = _Metallic;

                // Convert tangent-space normal to world-space for lighting
                float3 normalWS = normalize(mul(TBN, nTS));

                InputData lightingInput = (InputData)0;
                lightingInput.positionWS = IN.posWS;
                lightingInput.normalWS   = normalWS;
                lightingInput.viewDirectionWS = GetWorldSpaceViewDir(IN.posWS);
                lightingInput.shadowCoord = IN.shadowCoord;
                lightingInput.fogCoord    = IN.fogCoord;
                lightingInput.normalizedScreenSpaceUV = IN.screenUV;
                lightingInput.bakedGI = SampleSH(normalWS);

                SurfaceData surf = (SurfaceData)0;
                surf.albedo     = albedo;
                surf.metallic   = metallic;
                surf.specular   = 0;
                surf.smoothness = smooth;
                surf.normalTS   = float3(0,0,1);
                surf.emission   = 0;
                surf.occlusion  = 1;
                surf.alpha      = 1;

                half4 color = UniversalFragmentPBR(lightingInput, surf);
                color.rgb = MixFog(color.rgb, lightingInput.fogCoord);
                return color;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
