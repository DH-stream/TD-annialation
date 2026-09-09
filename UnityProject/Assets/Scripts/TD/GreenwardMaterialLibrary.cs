using System.Collections.Generic;
using UnityEngine;

namespace TDAnnihilation
{
    public static class GreenwardMaterialLibrary
    {
        private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();

        public static Material Grass => Get("Grass", new Color(0.20f, 0.38f, 0.16f), 0.18f, 731, 13f, 0.42f);
        public static Material Soil => Get("Soil", new Color(0.38f, 0.25f, 0.13f), 0.12f, 811);
        public static Material Gravel => Get("Gravel", new Color(0.57f, 0.47f, 0.32f), 0.08f, 941, 4.5f, 0.55f);
        public static Material CompactedGravel => Get("CompactedGravel", new Color(0.43f, 0.36f, 0.26f), 0.06f, 983, 5f, 0.32f);
        public static Material Stone => Get("Stone", new Color(0.44f, 0.43f, 0.38f), 0.14f, 1061, 3f, 0.48f);
        public static Material Timber => Get("Timber", new Color(0.30f, 0.16f, 0.08f), 0.16f, 1181);
        public static Material Plaster => Get("Plaster", new Color(0.72f, 0.59f, 0.39f), 0.2f, 1301);
        public static Material Roof => Get("Roof", new Color(0.38f, 0.12f, 0.08f), 0.2f, 1423);
        public static Material Leaves => Get("Leaves", new Color(0.14f, 0.32f, 0.12f), 0.12f, 1543);
        public static Material Corruption => Get("Corruption", new Color(0.34f, 0.06f, 0.25f), 0.28f, 1663);
        public static Material Water => Get("Water", new Color(0.12f, 0.42f, 0.50f), 0.72f, 1733);

        public static Material ForColor(Color color)
        {
            if (ColorDistance(color, new Color(0.20f, 0.38f, 0.16f)) < 0.16f) return Grass;
            if (color.b > 0.40f && color.g > color.r * 2f) return Water;
            if (color.r > 0.58f && color.g > 0.42f) return Plaster;
            if (color.r > 0.30f && color.g < 0.20f) return Roof;
            if (color.g > color.r * 1.35f) return Leaves;
            if (color.b > color.r * 1.15f) return Get("Arcane", color, 0.35f, 1777);
            if (color.r < 0.25f && color.g < 0.25f) return Timber;
            return Get("Tint_" + ColorUtility.ToHtmlStringRGB(color), color, 0.16f, color.GetHashCode());
        }

        private static float ColorDistance(Color a, Color b)
        {
            Vector3 delta = new Vector3(a.r - b.r, a.g - b.g, a.b - b.b);
            return delta.magnitude;
        }

        private static Material Get(string key, Color baseColor, float smoothness, int seed, float tiling = 2f, float normalStrength = 0.28f)
        {
            if (Materials.TryGetValue(key, out Material existing)) return existing;
            Shader shader = Shader.Find(key == "Leaves" ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
            Material material = new Material(shader) { name = "Greenward " + key };
            Texture2D texture = key == "Gravel"
                ? Resources.Load<Texture2D>("TDAnnihilation/Surfaces/gravel_stones_diff_1k")
                : null;
            if (texture == null) texture = BuildTexture(key, baseColor, seed);
            Texture2D normal = key == "Gravel"
                ? Resources.Load<Texture2D>("TDAnnihilation/Surfaces/gravel_stones_nor_gl_1k")
                : null;
            if (normal == null) normal = BuildNormalTexture(key, seed);
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", key == "Gravel" ? new Color(0.82f, 0.72f, 0.58f) : baseColor);
            material.SetTextureScale("_BaseMap", Vector2.one * tiling);
            if (key != "Leaves")
            {
                material.SetTexture("_BumpMap", normal);
                material.SetTextureScale("_BumpMap", Vector2.one * tiling);
                material.SetFloat("_BumpScale", normalStrength);
                material.EnableKeyword("_NORMALMAP");
            }
            material.mainTexture = texture;
            material.SetFloat("_Smoothness", smoothness);
            if (key == "Leaves") material.SetFloat("_Cull", 0f);
            material.enableInstancing = true;
            Materials.Add(key, material);
            return material;
        }

        private static Texture2D BuildTexture(string name, Color baseColor, int seed)
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGB24, true)
            {
                name = "Generated " + name,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };
            Color[] pixels = new Color[size * size];
            float offsetX = Mathf.Abs(seed % 997) * 0.17f;
            float offsetY = Mathf.Abs(seed % 619) * 0.21f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float broad = Mathf.PerlinNoise(offsetX + x * 0.038f, offsetY + y * 0.038f);
                    float detail = Mathf.PerlinNoise(offsetX * 2f + x * 0.23f, offsetY * 2f + y * 0.23f);
                    float fleck = Mathf.PerlinNoise(offsetX * 4f + x * 0.61f, offsetY * 4f + y * 0.61f);
                    float variation = (broad - 0.5f) * 0.46f + (detail - 0.5f) * 0.18f;
                    if (fleck > 0.76f) variation += 0.16f;
                    if (fleck < 0.20f) variation -= 0.12f;
                    pixels[y * size + x] = new Color(
                        Mathf.Clamp01(baseColor.r + variation),
                        Mathf.Clamp01(baseColor.g + variation * 0.85f),
                        Mathf.Clamp01(baseColor.b + variation * 0.60f));
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(true, false);
            return texture;
        }

        private static Texture2D BuildNormalTexture(string name, int seed)
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true, true)
            {
                name = "Generated " + name + " Normal",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };
            Color[] pixels = new Color[size * size];
            float offsetX = Mathf.Abs(seed % 997) * 0.17f;
            float offsetY = Mathf.Abs(seed % 619) * 0.21f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float left = SampleHeight(x - 1, y, offsetX, offsetY, size);
                    float right = SampleHeight(x + 1, y, offsetX, offsetY, size);
                    float down = SampleHeight(x, y - 1, offsetX, offsetY, size);
                    float up = SampleHeight(x, y + 1, offsetX, offsetY, size);
                    Vector3 normal = new Vector3(left - right, 0.65f, down - up).normalized;
                    pixels[y * size + x] = new Color(normal.x * 0.5f + 0.5f, normal.y * 0.5f + 0.5f, normal.z * 0.5f + 0.5f, 1f);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(true, false);
            return texture;
        }

        private static float SampleHeight(int x, int y, float offsetX, float offsetY, int size)
        {
            x = (x + size) % size;
            y = (y + size) % size;
            float broad = Mathf.PerlinNoise(offsetX + x * 0.038f, offsetY + y * 0.038f);
            float detail = Mathf.PerlinNoise(offsetX * 2f + x * 0.23f, offsetY * 2f + y * 0.23f);
            return broad * 0.65f + detail * 0.35f;
        }
    }
}
