using System.Collections.Generic;
using UnityEngine;

namespace PuzzleGame.Presentation.UI
{
    /// <summary>
    /// Procedural placeholder art. Every character/enemy/pack art request is
    /// resolved through here by string art-ref; when real art exists, swap
    /// the resolution to load sprites while keeping the same keys.
    /// Textures are cached and generated once per key.
    /// </summary>
    public static class PlaceholderArt
    {
        static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();

        /// <summary>
        /// Character/enemy portrait placeholder: vertical element gradient,
        /// diagonal sheen, initials. Awakened art refs get a brighter, more
        /// saturated variant with a halo band so the upgrade reads instantly.
        /// </summary>
        public static Texture2D Portrait(string artRef, ElementId element, string displayName, bool awakened)
        {
            string key = "p:" + artRef + ":" + (awakened ? "A" : "b");
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            const int w = 128, h = 160;
            var top = Theme.ElementColor(element);
            var bottom = Color.Lerp(top, Theme.BgDeep, awakened ? 0.35f : 0.65f);
            if (awakened)
            {
                top = Color.Lerp(top, Color.white, 0.25f);
            }

            var tex = NewTex(w, h);
            int seed = StableHash(artRef);
            for (int y = 0; y < h; y++)
            {
                float t = y / (float)(h - 1);
                var row = Color.Lerp(bottom, top, t);
                for (int x = 0; x < w; x++)
                {
                    var c = row;
                    // Diagonal sheen stripes, offset per character so cards differ.
                    float stripe = Mathf.Sin((x + y * 0.7f + seed % 97) * 0.11f);
                    if (stripe > 0.86f) c = Color.Lerp(c, Color.white, awakened ? 0.22f : 0.10f);
                    // Simple silhouette blob to suggest a figure.
                    float dx = (x - w * 0.5f) / (w * 0.30f);
                    float dy = (y - h * 0.38f) / (h * 0.34f);
                    if (dx * dx + dy * dy < 1f)
                    {
                        c = Color.Lerp(c, Theme.BgDeep, 0.42f);
                    }
                    // Awakened halo band behind the figure.
                    if (awakened)
                    {
                        float hd = Mathf.Abs(Mathf.Sqrt(dx * dx + dy * dy) - 1.18f);
                        if (hd < 0.09f) c = Color.Lerp(c, Color.white, 0.6f);
                    }
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            Cache[key] = tex;
            return tex;
        }

        /// <summary>Round orb texture with radial shading and a specular dot.</summary>
        public static Texture2D Orb(OrbColor color)
        {
            string key = "o:" + color;
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            const int s = 96;
            var baseC = Theme.OrbColorOf(color);
            var tex = NewTex(s, s);
            float r = s * 0.48f;
            Vector2 center = new Vector2(s * 0.5f, s * 0.5f);
            Vector2 lightPos = new Vector2(s * 0.36f, s * 0.64f);
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    if (d > r)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }
                    float edge = Mathf.Clamp01((r - d) / 2.2f);            // anti-alias rim
                    float shade = 1f - Mathf.Pow(d / r, 2.2f) * 0.55f;      // darker rim
                    var c = baseC * shade;
                    float ld = Vector2.Distance(new Vector2(x, y), lightPos);
                    if (ld < s * 0.16f)
                    {
                        c = Color.Lerp(c, Color.white, 0.75f * (1f - ld / (s * 0.16f)));
                    }
                    // Heart orbs get a soft inner glow instead of gloss.
                    if (color == OrbColor.Heart && d < r * 0.5f)
                    {
                        c = Color.Lerp(c, Color.white, 0.18f * (1f - d / (r * 0.5f)));
                    }
                    c.a = edge;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            Cache[key] = tex;
            return tex;
        }

        /// <summary>
        /// Card-pack texture for the summon rip. Tier drives richness:
        /// 0 = single pack, 1 = ten-pull pack, 2 = premium/foil tease.
        /// </summary>
        public static Texture2D Pack(string packArt, int tier)
        {
            string key = "k:" + packArt + ":" + tier;
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            const int w = 128, h = 176;
            int seed = StableHash(packArt);
            var hueA = Color.HSVToRGB(((seed % 360) / 360f), 0.55f, 0.55f);
            var hueB = Color.HSVToRGB((((seed % 360) + 40) % 360 / 360f), 0.65f, 0.30f);
            if (tier >= 2)
            {
                hueA = Color.Lerp(hueA, Theme.Rarity5, 0.45f);
            }
            var tex = NewTex(w, h);
            for (int y = 0; y < h; y++)
            {
                float t = y / (float)(h - 1);
                for (int x = 0; x < w; x++)
                {
                    var c = Color.Lerp(hueB, hueA, t);
                    // Foil diamond pattern grows with tier.
                    float diag = Mathf.PingPong(x + y + seed % 31, 24f) / 24f;
                    if (diag > 1f - 0.08f * (tier + 1))
                    {
                        c = Color.Lerp(c, Color.white, tier >= 2 ? 0.5f : 0.22f);
                    }
                    // Crimped top/bottom edges.
                    if (y < 8 || y > h - 9)
                    {
                        c = Color.Lerp(c, Color.black, ((x / 4) % 2 == 0) ? 0.35f : 0.15f);
                    }
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            Cache[key] = tex;
            return tex;
        }

        /// <summary>Wide stage/scene background placeholder tinted by theme key.</summary>
        public static Texture2D Background(string themeRef)
        {
            string key = "bg:" + themeRef;
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            const int w = 256, h = 128;
            int seed = StableHash(themeRef);
            var sky = Color.HSVToRGB((seed % 360) / 360f, 0.35f, 0.35f);
            var ground = Color.Lerp(sky, Theme.BgDeep, 0.6f);
            var tex = NewTex(w, h);
            for (int y = 0; y < h; y++)
            {
                float t = y / (float)(h - 1);
                for (int x = 0; x < w; x++)
                {
                    var c = Color.Lerp(ground, sky, t);
                    // Rolling hill silhouettes.
                    float hill = Mathf.Sin((x + seed % 53) * 0.045f) * 14f + h * 0.3f;
                    if (y < hill) c = Color.Lerp(c, Theme.BgDeep, 0.5f);
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            Cache[key] = tex;
            return tex;
        }

        public static string Initials(string displayName)
        {
            if (string.IsNullOrEmpty(displayName)) return "?";
            var parts = displayName.Split(' ');
            if (parts.Length == 1) return parts[0].Substring(0, Mathf.Min(2, parts[0].Length)).ToUpperInvariant();
            return (parts[0].Substring(0, 1) + parts[parts.Length - 1].Substring(0, 1)).ToUpperInvariant();
        }

        static Texture2D NewTex(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };
            return tex;
        }

        static int StableHash(string s)
        {
            if (string.IsNullOrEmpty(s)) return 17;
            unchecked
            {
                int hash = 23;
                foreach (char c in s) hash = hash * 31 + c;
                return hash < 0 ? -hash : hash;
            }
        }
    }
}
