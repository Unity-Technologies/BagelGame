#ifndef PROC_HEDGE_CUSTOM_V1_INCLUDED
#define PROC_HEDGE_CUSTOM_V1_INCLUDED

// Stylized Hedge (Shader Graph Custom Function - File mode)
// Triplanar, small-leaf look, optional micro-normal from pattern.
// Stage: Fragment
//
// Function name to use in the node: HedgeProc
// (Wrappers HedgeProc_float / HedgeProc_half are also provided.)

// --------------------- Utilities ---------------------
inline float3 _hash33(float3 p)
{
    p = frac(p * 0.1031);
    p += dot(p, p.yzx + 33.33);
    return frac((p.xxy + p.yzz) * p.zyx);
}

inline float _hash21(float2 p)
{
    float3 p3 = frac(float3(p.x, p.y, p.x) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

// Value noise (simple, cheap)
inline float _noise2(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);
    float a = _hash21(i + float2(0, 0));
    float b = _hash21(i + float2(1, 0));
    float c = _hash21(i + float2(0, 1));
    float d = _hash21(i + float2(1, 1));
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

// Worley (Voronoi F1) distance on 2D grid
inline float _worley(float2 p, out float2 center, out float2 cellID)
{
    float2 ip = floor(p);
    float md = 10.0;
    float2 nc = 0, id = 0;
    [unroll]
    for (int y = -1; y <= 1; y++)
    {
        [unroll]
        for (int x = -1; x <= 1; x++)
        {
            float2 g = float2(x, y);
            float2 lattice = ip + g;
            float2 rnd = _hash33(float3(lattice, 0)).xy;
            float2 c = lattice + rnd;
            float d = distance(c, p);
            if (d < md)
            {
                md = d;
                nc = c;
                id = lattice;
            }
        }
    }
    center = nc;
    cellID = id;
    return md;
}

// Stylized "leaflet" on a single 2D plane + gradient
inline float _leafPattern(float2 uv, float invLeafScale, float sharp, float contrast, out float2 grad)
{
    float2 C, ID;
    float d = _worley(uv * invLeafScale, C, ID); // nearest-cell distance
    float leaf = saturate(pow(saturate(1.0 - d), sharp)); // inverted + sharpened
    // slight variation
    float var = 0.6 + 0.4 * _noise2(uv * invLeafScale * 0.5);
    float val = pow(saturate(leaf * var), 1.0 + contrast * 0.5);

    // finite-diff gradient (scaled to pattern space)
    float e = 0.005 / max(invLeafScale, 1e-5); // small step in uv
    float d1 = saturate(pow(saturate(1.0 - _worley((uv + float2(e, 0)) * invLeafScale, C, ID)), sharp));
    float d2 = saturate(pow(saturate(1.0 - _worley((uv + float2(0, e)) * invLeafScale, C, ID)), sharp));
    grad = float2(d1 - leaf, d2 - leaf);
    return val;
}

// Triplanar weights
inline float3 _triWeights(float3 n, float sharp)
{
    n = abs(n);
    n = pow(n, sharp);
    return n / max(n.x + n.y + n.z, 1e-5);
}

// --------------------- Core (float) ---------------------
inline void HedgeProc(
    // Inputs
    float3 PositionWS,
    float3 NormalWS,
    float3 TangentWS,
    float3 BitangentWS,
    float TriplanarSharpness,
    float LeafScale, // world units per leaf cell (bigger = larger leaves)
    float LeafSharpness,
    float LeafContrast,
    float LeafNormalStrength, // 0..2
    float LumpScale, // world units for macro variation
    float LumpAmount, // 0..1 mix
    float3 HedgeLightColor,
    float3 HedgeDarkColor,
    float Smoothness,
    float Metallic,
    // Outputs
    out float3 OutBaseColor,
    out float3 OutNormalTS,
    out float OutSmoothness,
    out float OutMetallic,
    out float OutLeafMask // utility mask (brightness of leaf pattern)
)
{
    float3 nWS0 = normalize(NormalWS);
    float3 tWS = normalize(TangentWS);
    float3 bWS = normalize(BitangentWS);

    // Triplanar
    float3 w = _triWeights(nWS0, TriplanarSharpness);
    float invLeafScale = 1.0 / max(LeafScale, 1e-4);

    // Plane UVs
    float2 uvX = PositionWS.zy; // X-facing uses Z/Y
    float2 uvY = PositionWS.xz; // Y-facing uses X/Z
    float2 uvZ = PositionWS.xy; // Z-facing uses X/Y

    // Leaf values + gradients per plane
    float2 gX, gY, gZ;
    float vX = _leafPattern(uvX, invLeafScale, LeafSharpness, LeafContrast, gX);
    float vY = _leafPattern(uvY, invLeafScale, LeafSharpness, LeafContrast, gY);
    float vZ = _leafPattern(uvZ, invLeafScale, LeafSharpness, LeafContrast, gZ);

    // Blend value
    float leafVal = dot(float3(vX, vY, vZ), w);

    // Macro "lumps" variation (soft shade across the hedge)
    float lump = _noise2(PositionWS.xz / max(LumpScale, 1e-3));
    lump = lerp(0.5, lump, saturate(LumpAmount));

    // Color ramp
    float3 base = lerp(HedgeDarkColor, HedgeLightColor, saturate(leafVal * 0.85 + lump * 0.25));

    // Micro normal from gradients (map each plane's grad into world axes)
    float3 gWX = float3(0, -gX.y, -gX.x);
    float3 gWY = float3(-gY.x, 0, -gY.y);
    float3 gWZ = float3(-gZ.x, -gZ.y, 0);
    float3 gW = normalize(w.x * gWX + w.y * gWY + w.z * gWZ);

    float3 nMicroWS = normalize(nWS0 + gW * LeafNormalStrength);
    // World → Tangent
    float3 nTS = normalize(float3(dot(nMicroWS, tWS), dot(nMicroWS, bWS), dot(nMicroWS, nWS0)));

    // Outputs
    OutBaseColor = base;
    OutNormalTS = nTS;
    OutSmoothness = Smoothness;
    OutMetallic = Metallic;
    OutLeafMask = leafVal;
}

// --------------------- Wrappers (float/half) ---------------------
inline void HedgeProc_float(
    float3 PositionWS, float3 NormalWS, float3 TangentWS, float3 BitangentWS,
    float TriplanarSharpness, float LeafScale, float LeafSharpness, float LeafContrast, float LeafNormalStrength,
    float LumpScale, float LumpAmount, float3 HedgeLightColor, float3 HedgeDarkColor, float Smoothness, float Metallic,
    out float3 OutBaseColor, out float3 OutNormalTS, out float OutSmoothness, out float OutMetallic, out float OutLeafMask
)
{
    HedgeProc(PositionWS, NormalWS, TangentWS, BitangentWS, TriplanarSharpness, LeafScale, LeafSharpness, LeafContrast, LeafNormalStrength,
              LumpScale, LumpAmount, HedgeLightColor, HedgeDarkColor, Smoothness, Metallic,
              OutBaseColor, OutNormalTS, OutSmoothness, OutMetallic, OutLeafMask);
}

inline void HedgeProc_half(
    half3 PositionWS, half3 NormalWS, half3 TangentWS, half3 BitangentWS,
    half TriplanarSharpness, half LeafScale, half LeafSharpness, half LeafContrast, half LeafNormalStrength,
    half LumpScale, half LumpAmount, half3 HedgeLightColor, half3 HedgeDarkColor, half Smoothness, half Metallic,
    out half3 OutBaseColor, out half3 OutNormalTS, out half OutSmoothness, out half OutMetallic, out half OutLeafMask
)
{
    float3 oBC;
    float3 oNT;
    float oS;
    float oM;
    float oMask;
    HedgeProc((float3) PositionWS, (float3) NormalWS, (float3) TangentWS, (float3) BitangentWS,
              (float) TriplanarSharpness, (float) LeafScale, (float) LeafSharpness, (float) LeafContrast, (float) LeafNormalStrength,
              (float) LumpScale, (float) LumpAmount, (float3) HedgeLightColor, (float3) HedgeDarkColor, (float) Smoothness, (float) Metallic,
              oBC, oNT, oS, oM, oMask);
    OutBaseColor = (half3) oBC;
    OutNormalTS = (half3) oNT;
    OutSmoothness = (half) oS;
    OutMetallic = (half) oM;
    OutLeafMask = (half) oMask;
}

#endif // PROC_HEDGE_CUSTOM_V1_INCLUDED
