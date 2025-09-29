#ifndef PROC_BRICKS_CUSTOM_V2_INCLUDED
#define PROC_BRICKS_CUSTOM_V2_INCLUDED

// Procedural Bricks for Shader Graph (URP/HDRP-agnostic surface layer)
// - No bevel (flat).
// - Edge darkening (“dirt”) that fades inward from mortar.
// - Stable AA using fwidth(edgeDist) + clamp to avoid mortar growth at distance.
//
// Custom Function (File) setup:
//   Function Name: BrickProc
//   Precision: Inherit (ok), or Float
//   Stage: Fragment (needed for ddx/ddy/fwidth)
//
// Inputs (add in this order; types shown are SG port types):
//   float2  UV
//   float2  Tiling
//   float   MortarWidth
//   float   Stagger
//   float3  BrickColorA
//   float3  BrickColorB
//   float3  MortarColor
//   float   ColorVariation
//   float   BrickSmoothness
//   float   MortarSmoothness
//   float   Metallic
//   float   RandomSeed
//   float   AAScale
//   float   AAClampFrac
//   float   EdgeFadeWidth
//   float   EdgeFadeAmount
//   float   EdgeFadeExponent
//
// Outputs:
//   float3  OutBaseColor   → Base Color
//   float3  OutNormalTS    → Normal (Tangent Space)
//   float   OutSmoothness  → Smoothness
//   float   OutMetallic    → Metallic
//   float   OutBrickMask   → (optional utility)
//   float   OutMortarMask  → (optional utility)
//
// Suggested defaults:
//   Tiling=(8,4), MortarWidth=0.08, Stagger=0.5, ColorVariation=0.15,
//   BrickSmoothness=0.4, MortarSmoothness=0.15, Metallic=0,
//   RandomSeed=1, AAScale=1, AAClampFrac=0.45,
//   EdgeFadeWidth=0.08, EdgeFadeAmount=0.35, EdgeFadeExponent=1.5

inline float _hash21(float2 p, float seed)
{
    float h = sin(dot(p, float2(12.9898, 78.233)) + seed) * 43758.5453;
    return frac(h);
}

inline void BrickProc(
    float2 UV,
    float2 Tiling,
    float MortarWidth,
    float Stagger,
    float3 BrickColorA,
    float3 BrickColorB,
    float3 MortarColor,
    float ColorVariation,
    float BrickSmoothness,
    float MortarSmoothness,
    float Metallic,
    float RandomSeed,
    float AAScale,
    float AAClampFrac,
    float EdgeFadeWidth,
    float EdgeFadeAmount,
    float EdgeFadeExponent,
    out float3 OutBaseColor,
    out float3 OutNormalTS,
    out float OutSmoothness,
    out float OutMetallic,
    out float OutBrickMask,
    out float OutMortarMask)
{
    // Base tiling
    float2 uv = UV * Tiling;

    // Row stagger
    float row = floor(uv.y);
    float parity = fmod(row, 2.0);
    uv.x += parity * Stagger;

    // Cell-local coords
    float2 cellUV = frac(uv);

    // Distance to nearest brick edge
    float dx = min(cellUV.x, 1.0 - cellUV.x);
    float dy = min(cellUV.y, 1.0 - cellUV.y);
    float edgeDist = min(dx, dy);

    // Stable AA in edgeDist domain
    //float w = fwidth(edgeDist) * AAScale;
    //w = min(w, MortarWidth * AAClampFrac); // avoid mortar “growth” at distance

    // Masks
    float mortar = 0.0;// 1.0 - smoothstep(MortarWidth, MortarWidth + w, edgeDist);
    float brickMask = 1.0 - mortar;

    // Per-brick random + color
    float2 brickID = floor(uv);
    float r = _hash21(brickID, RandomSeed);
    float variation = ((r - 0.5) * 2.0) * ColorVariation;
    float3 baseCol = lerp(BrickColorA, BrickColorB, r);
    float3 brickCol = baseCol * (1.0 + variation);

    // Edge darkening inward from mortar
    //float inner0 = MortarWidth + w;
    //float inner1 = inner0 + EdgeFadeWidth;
    //float edgeDark01 = 1.0 - smoothstep(inner0, inner1, edgeDist); // 1 at edge, 0 inward
    //edgeDark01 = pow(saturate(edgeDark01), EdgeFadeExponent);
    //float darkFactor = 1.0 - edgeDark01 * EdgeFadeAmount;
    //brickCol *= darkFactor;

    // Outputs
    OutBaseColor = lerp(MortarColor, brickCol, brickMask);
    OutNormalTS = float3(0, 0, 1); // flat tangent-space normal
    OutSmoothness = lerp(MortarSmoothness, BrickSmoothness, brickMask);
    OutMetallic = Metallic;
    OutBrickMask = brickMask;
    OutMortarMask = mortar;
}

// Some Shader Graph versions auto-suffix function calls with _float / _half.
// Provide wrappers so the node compiles regardless of precision.

inline void BrickProc_float(
    float2 UV,
    float2 Tiling,
    float MortarWidth,
    float Stagger,
    float3 BrickColorA,
    float3 BrickColorB,
    float3 MortarColor,
    float ColorVariation,
    float BrickSmoothness,
    float MortarSmoothness,
    float Metallic,
    float RandomSeed,
    float AAScale,
    float AAClampFrac,
    float EdgeFadeWidth,
    float EdgeFadeAmount,
    float EdgeFadeExponent,
    out float3 OutBaseColor,
    out float3 OutNormalTS,
    out float OutSmoothness,
    out float OutMetallic,
    out float OutBrickMask,
    out float OutMortarMask)
{
    BrickProc(UV, Tiling, MortarWidth, Stagger, BrickColorA, BrickColorB, MortarColor,
              ColorVariation, BrickSmoothness, MortarSmoothness, Metallic, RandomSeed,
              AAScale, AAClampFrac, EdgeFadeWidth, EdgeFadeAmount, EdgeFadeExponent,
              OutBaseColor, OutNormalTS, OutSmoothness, OutMetallic, OutBrickMask, OutMortarMask);
}

inline void BrickProc_half(
    half2 UV,
    half2 Tiling,
    half MortarWidth,
    half Stagger,
    half3 BrickColorA,
    half3 BrickColorB,
    half3 MortarColor,
    half ColorVariation,
    half BrickSmoothness,
    half MortarSmoothness,
    half Metallic,
    half RandomSeed,
    half AAScale,
    half AAClampFrac,
    half EdgeFadeWidth,
    half EdgeFadeAmount,
    half EdgeFadeExponent,
    out half3 OutBaseColor,
    out half3 OutNormalTS,
    out half OutSmoothness,
    out half OutMetallic,
    out half OutBrickMask,
    out half OutMortarMask)
{
    // Promote to float for stable derivatives, then cast back
    float2 UVf = UV;
    float2 Tilingf = Tiling;
    float MortarWidthf = MortarWidth;
    float Staggerf = Stagger;
    float3 BrickColorAf = BrickColorA;
    float3 BrickColorBf = BrickColorB;
    float3 MortarColorf = MortarColor;
    float ColorVariationf = ColorVariation;
    float BrickSmoothnessf = BrickSmoothness;
    float MortarSmoothnessf = MortarSmoothness;
    float Metallicf = Metallic;
    float RandomSeedf = RandomSeed;
    float AAScalef = AAScale;
    float AAClampFracf = AAClampFrac;
    float EdgeFadeWidthf = EdgeFadeWidth;
    float EdgeFadeAmountf = EdgeFadeAmount;
    float EdgeFadeExponentf = EdgeFadeExponent;

    float3 OutBaseColorf;
    float3 OutNormalTSf;
    float OutSmoothnessf;
    float OutMetallicf;
    float OutBrickMaskf;
    float OutMortarMaskf;

    BrickProc(UVf, Tilingf, MortarWidthf, Staggerf, BrickColorAf, BrickColorBf, MortarColorf,
              ColorVariationf, BrickSmoothnessf, MortarSmoothnessf, Metallicf, RandomSeedf,
              AAScalef, AAClampFracf, EdgeFadeWidthf, EdgeFadeAmountf, EdgeFadeExponentf,
              OutBaseColorf, OutNormalTSf, OutSmoothnessf, OutMetallicf, OutBrickMaskf, OutMortarMaskf);

    OutBaseColor = (half3) OutBaseColorf;
    OutNormalTS = (half3) OutNormalTSf;
    OutSmoothness = (half) OutSmoothnessf;
    OutMetallic = (half) OutMetallicf;
    OutBrickMask = (half) OutBrickMaskf;
    OutMortarMask = (half) OutMortarMaskf;
}

#endif // PROC_BRICKS_CUSTOM_V2_INCLUDED
