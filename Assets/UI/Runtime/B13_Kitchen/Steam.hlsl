// The steam's motion, run in the vertex stage of Steam.shadergraph (its Custom Function node).
//
// SteamPuffs.cs draws each puff once, at the bottom of the element where it starts, and a mesh modifier writes
// the puff's motion into the extra vertex channels of all its vertices. This moves them every frame on the GPU:
// the CPU only sets the global _BagelTime.
//
//   UV1  start x (a fraction of the width), time offset (seconds), rise time (seconds), sway (a fraction of the width)
//   UV2  sway frequency (radians a second), end scale, 0, 1 (a puff; 0 leaves the vertex where it is)
//   UV3  element width, height, where the puffs start and where they're gone (y, all in points)
//
// A puff rises from the bottom to the top in its rise time, swaying more as it goes, grows, fades in quickly and
// out slowly, then starts again at the bottom.

void SteamPuff_float(float3 Position, float4 UV1, float4 UV2, float4 UV3, float Time, out float3 Out, out float Alpha)
{
    Out = Position;
    Alpha = 1;
    if (UV2.w < 0.5)
        return;

    float2 size = UV3.xy;
    float bottom = UV3.z;
    float2 start = float2(UV1.x * size.x, bottom);
    // Where the vertex is around the puff's middle, as drawn.
    float2 local = Position.xy - start;

    float life = frac((Time + UV1.y) / max(UV1.z, 0.01));
    float y = lerp(bottom, UV3.w, life);
    float x = start.x + sin(Time * UV2.x + UV1.y * 3.0) * UV1.w * size.x * life;
    float scale = lerp(0.45, UV2.y, life);

    Out = float3(float2(x, y) + local * scale, Position.z);
    Alpha = smoothstep(0.0, 0.12, life) * (1.0 - smoothstep(0.45, 1.0, life));
}

void SteamPuff_half(half3 Position, half4 UV1, half4 UV2, half4 UV3, half Time, out half3 Out, out half Alpha)
{
    float3 position;
    float alpha;
    SteamPuff_float(Position, UV1, UV2, UV3, Time, position, alpha);
    Out = position;
    Alpha = alpha;
}
