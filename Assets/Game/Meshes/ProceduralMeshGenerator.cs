using System;
using UnityEngine;

namespace Bagel
{
    // The inputs and the code for one procedural mesh. A .procmesh asset keeps a generator's inputs in
    // its .meta file and imports the mesh it builds, so every mesh in the game can be re-tweaked.
    [Serializable]
    public abstract class ProceduralMeshGenerator
    {
        public abstract void Build(MeshBuilder builder);

        public Mesh Generate(string name)
        {
            var builder = new MeshBuilder();
            Build(builder);
            return builder.ToMesh(name);
        }

        // A repeatable random number in [0, 1) for each (seed, index): the same inputs always build the
        // same mesh, on every machine.
        protected static float Hash01(int seed, int index)
        {
            unchecked
            {
                uint h = (uint)seed * 0x9E3779B1u + (uint)index * 0x85EBCA77u;
                h ^= h >> 15;
                h *= 0x2C1B3C6Du;
                h ^= h >> 12;
                h *= 0x297A2D39u;
                h ^= h >> 15;
                return (h >> 8) * (1f / (1 << 24));
            }
        }
    }
}
