namespace Dreamy
{
    public enum DreamyTileGuideRole
    {
        Unknown,
        BackgroundWater,
        WaterFoam,
        FlatGround,
        ElevatedGround,
        Shadow,
        Cliff,
        Stairs,
        Bridge,
        Decoration,
        Resource,
        Blocker
    }

    public enum DreamyTileCollisionRole
    {
        None,
        BlockMovement,
        Trigger,
        Contextual
    }

    public readonly struct DreamyTileLayerPolicy
    {
        public DreamyTileLayerPolicy(
            string layerName,
            DreamyTileGuideRole guideRole,
            DreamyTileCollisionRole collisionRole,
            int elevationLevel,
            int sortingBand,
            bool walkable,
            bool connectsElevation,
            bool renderBelowActor,
            bool renderAboveActor,
            string notes)
        {
            LayerName = layerName;
            GuideRole = guideRole;
            CollisionRole = collisionRole;
            ElevationLevel = elevationLevel;
            SortingBand = sortingBand;
            Walkable = walkable;
            ConnectsElevation = connectsElevation;
            RenderBelowActor = renderBelowActor;
            RenderAboveActor = renderAboveActor;
            Notes = notes;
        }

        public string LayerName { get; }
        public DreamyTileGuideRole GuideRole { get; }
        public DreamyTileCollisionRole CollisionRole { get; }
        public int ElevationLevel { get; }
        public int SortingBand { get; }
        public bool Walkable { get; }
        public bool ConnectsElevation { get; }
        public bool RenderBelowActor { get; }
        public bool RenderAboveActor { get; }
        public string Notes { get; }

        public bool BlocksMovement => CollisionRole == DreamyTileCollisionRole.BlockMovement;
    }

    public static class DreamyLevelTileRules
    {
        public const int TileSizePixels = 64;
        public const float PixelsPerUnit = 64f;
        public const string GuideSource = "https://pixelfrog-assets.itch.io/tiny-swords/devlog/1138989/tilemap-guide";

        public static DreamyTileLayerPolicy GetLayerPolicy(string layerName)
        {
            switch (layerName)
            {
                case "Background":
                case "BG Color":
                case "Water Background":
                case "Water Background color":
                    return new DreamyTileLayerPolicy(layerName, DreamyTileGuideRole.BackgroundWater, DreamyTileCollisionRole.BlockMovement, -1, 0, false, false, true, false, "Fill water/background space outside land.");

                case "Water Foam":
                    return new DreamyTileLayerPolicy(layerName, DreamyTileGuideRole.WaterFoam, DreamyTileCollisionRole.None, -1, 1, false, false, true, false, "Animated edge where terrain touches water.");

                case "Sand":
                    return new DreamyTileLayerPolicy(layerName, DreamyTileGuideRole.FlatGround, DreamyTileCollisionRole.None, 0, 10, true, false, true, false, "Lowest walkable terrain next to water.");

                case "Grass":
                    return new DreamyTileLayerPolicy(layerName, DreamyTileGuideRole.ElevatedGround, DreamyTileCollisionRole.None, 1, 30, true, false, true, false, "Raised walkable terrain surface.");

                case "Shadows":
                case "Shadow":
                    return new DreamyTileLayerPolicy(layerName, DreamyTileGuideRole.Shadow, DreamyTileCollisionRole.None, 1, 20, false, false, true, false, "Depth sprite for elevated terrain, offset one 64px tile downward.");

                case "Cliff":
                    return new DreamyTileLayerPolicy(layerName, DreamyTileGuideRole.Cliff, DreamyTileCollisionRole.Contextual, 1, 25, false, false, true, false, "Vertical terrain face. Movement should be blocked by navigation rules, not by the whole visual layer.");

                case "Stairs":
                    return new DreamyTileLayerPolicy(layerName, DreamyTileGuideRole.Stairs, DreamyTileCollisionRole.None, 1, 35, true, true, true, false, "Connects lower terrain to elevated terrain.");

                case "Bridge - horizontal":
                case "Bridge - vertical":
                    return new DreamyTileLayerPolicy(layerName, DreamyTileGuideRole.Bridge, DreamyTileCollisionRole.None, 0, 40, true, false, true, false, "Walkable connection over water or gaps.");

                case "Buildings":
                    return new DreamyTileLayerPolicy(layerName, DreamyTileGuideRole.Blocker, DreamyTileCollisionRole.BlockMovement, 1, 80, false, false, false, true, "Large obstacle and actor occluder.");

                case "Trees front":
                    return new DreamyTileLayerPolicy(layerName, DreamyTileGuideRole.Blocker, DreamyTileCollisionRole.BlockMovement, 1, 90, false, false, false, true, "Front tree canopy/trunk obstacle.");

                case "Trees back":
                    return new DreamyTileLayerPolicy(layerName, DreamyTileGuideRole.Blocker, DreamyTileCollisionRole.BlockMovement, 1, 70, false, false, false, true, "Back tree obstacle that can visually sit behind front layers.");

                case "Small rocks":
                case "Rocks":
                    return new DreamyTileLayerPolicy(layerName, DreamyTileGuideRole.Blocker, DreamyTileCollisionRole.BlockMovement, 0, 60, false, false, true, false, "Rock obstacle.");

                case "Miscs":
                    return new DreamyTileLayerPolicy(layerName, DreamyTileGuideRole.Decoration, DreamyTileCollisionRole.Contextual, 0, 100, false, false, false, true, "Mixed props; classify individual items before making them interactive.");

                default:
                    return new DreamyTileLayerPolicy(layerName, DreamyTileGuideRole.Unknown, DreamyTileCollisionRole.Contextual, 0, 50, false, false, true, false, "Unmapped layer. Add a policy before relying on it in gameplay.");
            }
        }

        public static bool LayerBlocksMovement(string layerName)
        {
            return GetLayerPolicy(layerName).BlocksMovement;
        }
    }
}
