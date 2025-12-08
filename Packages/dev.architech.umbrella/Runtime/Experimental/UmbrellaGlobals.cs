using System;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.U2D;

using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("ArchiTech.Umbrella.Editor")]

namespace ArchiTech.Umbrella
{
    public static class UmbrellaGlobals
    {
        public static readonly Type[] ColliderTypes =
        {
            typeof(BoxCollider),
            typeof(SphereCollider),
            typeof(CapsuleCollider),
            typeof(TerrainCollider),
            typeof(MeshCollider),
            typeof(WheelCollider),
        };

        public static readonly Type[] RendererTypes =
        {
            typeof(BillboardRenderer),
            typeof(CanvasRenderer),
            typeof(LineRenderer),
            typeof(MeshRenderer),
            typeof(SkinnedMeshRenderer),
            typeof(SpriteRenderer),
            typeof(SpriteShapeRenderer),
            typeof(TilemapRenderer),
            typeof(TrailRenderer),
        };
    }
}