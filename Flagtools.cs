using System;
using System.IO;
using System.Reflection;
using System.Text;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MelonLoader;
using UnityEngine;

namespace MultiLanguage
{
    internal static class FlagTools
    {
        public const int Size = 256;   // размер готового флага, px

        public static byte[] ReadResource(string name)
        {
            using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
            {
                if (s == null) return null;
                using (var ms = new MemoryStream())
                {
                    s.CopyTo(ms);
                    return ms.ToArray();
                }
            }
        }

        public static string ReadText(string name)
        {
            var bytes = ReadResource(name);
            return bytes == null ? null : Encoding.UTF8.GetString(bytes).TrimStart('\uFEFF');
        }

        // Flags/<код>.png -> круглый спрайт
        public static Sprite LoadRound(string code)
        {
            Texture2D src = null;
            try
            {
                var data = ReadResource("Flags/" + code + ".png");
                if (data == null)
                {
                    MelonLogger.Warning($"Flag Flags/{code}.png not found, using placeholder.");
                    return Placeholder();
                }

                src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!ImageConversion.LoadImage(src, data))
                {
                    MelonLogger.Warning($"Flag {code}.png could not be decoded.");
                    return Placeholder();
                }

                return MakeRound(src);
            }
            catch (Exception e)
            {
                MelonLogger.Warning($"Flag {code} failed: {e.Message}");
                return Placeholder();
            }
            finally
            {
                if (src != null) UnityEngine.Object.Destroy(src);
            }
        }

        private static Sprite Placeholder()
        {
            var t = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                    t.SetPixel(x, y, new Color(0.35f, 0.35f, 0.4f, 1f));
            t.Apply();
            try { return MakeRound(t); }
            finally { UnityEngine.Object.Destroy(t); }
        }

        // берём квадрат из центра, масштабируем, режем кругом и затемняем к краю
        private static Sprite MakeRound(Texture2D src)
        {
            int w = src.width, h = src.height;
            int side = Math.Min(w, h);
            float ox = (w - side) * 0.5f;
            float oy = (h - side) * 0.5f;

            var px = new Il2CppStructArray<Color32>(Size * Size);
            float r = Size * 0.5f;

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float dx = x + 0.5f - r;
                    float dy = y + 0.5f - r;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);

                    float edge = Mathf.Clamp01(r - d);      // сглаживание края, 1px
                    if (edge <= 0f)
                    {
                        px[y * Size + x] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    float u = (ox + (x + 0.5f) / Size * side) / w;
                    float v = (oy + (y + 0.5f) / Size * side) / h;
                    Color c = src.GetPixelBilinear(u, v);

                    // к краю темнее, как у флагов в игре
                    float t = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.70f, 1f, d / r));
                    float k = 1f - 0.55f * t;

                    px[y * Size + x] = new Color32(
                        (byte)(Mathf.Clamp01(c.r * k) * 255f),
                        (byte)(Mathf.Clamp01(c.g * k) * 255f),
                        (byte)(Mathf.Clamp01(c.b * k) * 255f),
                        (byte)(Mathf.Clamp01(c.a * edge) * 255f));
                }
            }

            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}