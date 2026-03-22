using System.Collections.Generic;
using UnityEngine;

namespace ET
{
    [ChildOf(typeof(DrawMap))]
    public partial class DrawCarpet : Entity, IAwake, IDestroy
    {
        [StaticField]
        public static string[] mainNames = { "noise_rocky", "Ground_noise_water_shallow", "forest_ground_noise" };

        [StaticField]
        public static string[] overNames = { "blocky", "water", "grass" };

        public MeshRenderer MeshRenderer;
        public MeshFilter MeshFilter;
        public Texture2D MainTexture;
        public Texture2D OverlayTexture;
        public GameObject View;
        public int CarType;
        public MaterialPropertyBlock MatPropBlock;
        public EntityRef<MapLogic> MapLogic;
        public readonly List<Vector3> Vertices = new List<Vector3>();
        public readonly List<Vector2> UV = new List<Vector2>();
        public readonly List<Vector2> UV2 = new List<Vector2>();
    }
}
