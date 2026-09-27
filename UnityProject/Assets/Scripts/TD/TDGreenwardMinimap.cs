using UnityEngine;
using UnityEngine.UIElements;

namespace TDAnnihilation
{
    public sealed class TDGreenwardMinimap : VisualElement
    {
        private static readonly Color RoadOutline = new Color(0.12f, 0.14f, 0.11f, 0.95f);
        private static readonly Color RoadColor = new Color(0.72f, 0.48f, 0.25f, 0.95f);
        private TDVerticalSliceBootstrap game;
        private Texture2D terrainImage;

        public TDGreenwardMinimap()
        {
            name = "greenward-minimap-map";
            pickingMode = PickingMode.Ignore;
            style.flexGrow = 1f;
            generateVisualContent += DrawMap;
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                if (terrainImage != null) UnityEngine.Object.Destroy(terrainImage);
                terrainImage = null;
            });
        }

        public void Bind(TDVerticalSliceBootstrap bootstrap)
        {
            game = bootstrap;
            BuildTerrainImage();
            MarkDirtyRepaint();
        }

        private void BuildTerrainImage()
        {
            if (game == null || game.Route == null) return;
            Bounds bounds = game.PlayableBounds;
            if (bounds.size.x <= 0f || bounds.size.z <= 0f) return;
            const int width = 224;
            const int height = 160;
            terrainImage = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Greenward Minimap Terrain",
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float worldX = Mathf.Lerp(bounds.min.x, bounds.max.x, x / (float)(width - 1));
                    float worldZ = Mathf.Lerp(bounds.min.z, bounds.max.z, y / (float)(height - 1));
                    float noise = Mathf.PerlinNoise((worldX + 122f) * .18f, (worldZ + 78f) * .18f);
                    float heightTone = Mathf.Clamp01((GreenwardWorldLayout.HeightAt(worldX, worldZ) + 1f) * .12f);
                    Color color = new Color(.075f, .17f, .115f);
                    color = Color.Lerp(color, new Color(.20f, .32f, .18f), noise * .52f + heightTone * .23f);
                    if (worldX < -30f) color = Color.Lerp(color, new Color(.16f, .12f, .19f), .65f);
                    if (Mathf.Abs(worldZ) > 17f && noise > .38f)
                        color = Color.Lerp(color, new Color(.045f, .15f, .08f), .45f);
                    float riverT = (worldZ + 33f) / 62f;
                    float riverCenter = -15f + Mathf.Sin(riverT * 8.4f) * .65f + Mathf.Sin(riverT * 19f) * .18f;
                    float riverHalfWidth = 1.65f + Mathf.Sin(riverT * 11f) * .28f;
                    if (Mathf.Abs(worldX - riverCenter) < riverHalfWidth + .55f)
                        color = new Color(.15f, .25f, .24f);
                    if (Mathf.Abs(worldX - riverCenter) < riverHalfWidth)
                        color = Color.Lerp(new Color(.075f, .29f, .36f), new Color(.12f, .37f, .43f), noise);
                    if (GreenwardWorldLayout.IsRoad(new Vector2(worldX, worldZ), 2.2f, game.Route))
                        color = Color.Lerp(new Color(.48f, .32f, .18f), new Color(.59f, .42f, .23f), noise);
                    if (worldX > 32f && Mathf.Abs(worldZ - 4f) < 8f)
                        color = Color.Lerp(color, new Color(.34f, .34f, .31f), .78f);
                    pixels[y * width + x] = color;
                }
            terrainImage.SetPixels(pixels);
            terrainImage.Apply(false, true);
            style.backgroundImage = new StyleBackground(terrainImage);
        }

        public void Refresh()
        {
            if (game != null) MarkDirtyRepaint();
        }

        private void DrawMap(MeshGenerationContext context)
        {
            if (game == null || contentRect.width <= 1f || contentRect.height <= 1f) return;

            Rect rect = new Rect(0f, 0f, contentRect.width, contentRect.height);
            Bounds bounds = game.PlayableBounds;
            Painter2D painter = context.painter2D;
            DrawRoute(painter, rect, bounds);

            if (game.Goal != null) DrawWorldMarker(painter, game.Goal.position, bounds, rect, 6f, new Color(1f, 0.79f, 0.34f));
            for (int i = 0; i < game.PlacedTowers.Count; i++)
                if (game.PlacedTowers[i] != null) DrawWorldMarker(painter, game.PlacedTowers[i].position, bounds, rect, 3.5f, new Color(0.77f, 0.56f, 1f));
            for (int i = 0; i < game.ActiveEnemies.Count; i++)
                if (game.ActiveEnemies[i] != null) DrawWorldMarker(painter, game.ActiveEnemies[i].transform.position, bounds, rect, 3.2f, new Color(0.95f, 0.25f, 0.19f));
            if (game.Hero != null) DrawWorldMarker(painter, game.Hero.position, bounds, rect, 5.3f, new Color(0.22f, 0.76f, 1f));
        }

        private void DrawRoute(Painter2D painter, Rect rect, Bounds bounds)
        {
            if (game.Route == null || game.Route.Count < 2 || bounds.size.x <= Mathf.Epsilon || bounds.size.z <= Mathf.Epsilon) return;

            painter.lineCap = LineCap.Round;
            painter.lineJoin = LineJoin.Round;
            painter.BeginPath();
            for (int i = 0; i < game.Route.Count; i++)
            {
                Vector2 point = TDGreenwardMinimapMath.WorldToMap(game.Route[i], bounds, rect);
                if (i == 0) painter.MoveTo(point);
                else painter.LineTo(point);
            }
            painter.lineWidth = 9f;
            painter.strokeColor = RoadOutline;
            painter.Stroke();

            painter.BeginPath();
            for (int i = 0; i < game.Route.Count; i++)
            {
                Vector2 point = TDGreenwardMinimapMath.WorldToMap(game.Route[i], bounds, rect);
                if (i == 0) painter.MoveTo(point);
                else painter.LineTo(point);
            }
            painter.lineWidth = 5f;
            painter.strokeColor = RoadColor;
            painter.Stroke();
        }

        private static void DrawWorldMarker(Painter2D painter, Vector3 world, Bounds bounds, Rect rect, float radius, Color color)
        {
            if (bounds.size.x <= Mathf.Epsilon || bounds.size.z <= Mathf.Epsilon) return;

            Vector2 center = TDGreenwardMinimapMath.WorldToMap(world, bounds, rect);
            painter.BeginPath();
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * 0.25f;
                Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                if (i == 0) painter.MoveTo(point);
                else painter.LineTo(point);
            }
            painter.ClosePath();
            painter.fillColor = color;
            painter.Fill();
        }
    }
}
