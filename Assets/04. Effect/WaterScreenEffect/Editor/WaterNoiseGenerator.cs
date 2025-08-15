#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class NoiseTextureCreator
{
    [MenuItem("Tools/Generate Noise Texture (Perlin RFloat)")]
    public static void GenerateNoiseTexture()
    {
        int size = 1024;
        var tex = new Texture2D(size, size, TextureFormat.RFloat, false, true);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float v = Mathf.PerlinNoise(x * 0.1f, y * 0.1f);
            tex.SetPixel(x, y, new Color(v, v, v, 1));
        }
        tex.Apply(false, false);

        string absPath = EditorUtility.SaveFilePanel("Save Noise (EXR)", Application.dataPath, "NoiseTexture", "exr");
        if (!string.IsNullOrEmpty(absPath))
        {
            File.WriteAllBytes(absPath, tex.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat | Texture2D.EXRFlags.CompressZIP));
            TryImportNormalSettings(absPath, isNormalMap:false);
            Debug.Log("Saved: " + absPath);
        }
        Object.DestroyImmediate(tex);
    }

    [MenuItem("Tools/Generate Droplet Normal Map")]
    public static void GenerateDropletNormalMap()
    {
        // 파라미터
        int size = 1024;                 // 텍스처 크기
        int dropletCount = 300;          // 물방울 개수
        Vector2 radiusPxRange = new Vector2(6f, 28f); // 반지름(px)
        Vector2 strengthRange = new Vector2(0.4f, 1.0f); // 높이 강도(상대)
        float bumpScale = 6.0f;          // 노멀 세기(경사 스케일)
        int seed = System.DateTime.Now.Millisecond;

        // HeightMap 생성
        float[,] h = new float[size, size];
        var rand = new System.Random(seed);

        // 물방울을 무작위로 찍기 (원형 캡 + 부드러운 가장자리)
        for (int k = 0; k < dropletCount; k++)
        {
            float cx = (float)rand.NextDouble() * size;
            float cy = (float)rand.NextDouble() * size;

            float radius = Mathf.Lerp(radiusPxRange.x, radiusPxRange.y, (float)rand.NextDouble());
            float r2 = radius * radius;
            float strength = Mathf.Lerp(strengthRange.x, strengthRange.y, (float)rand.NextDouble());

            int minX = Mathf.Max(0, Mathf.FloorToInt(cx - radius - 1));
            int maxX = Mathf.Min(size - 1, Mathf.CeilToInt(cx + radius + 1));
            int minY = Mathf.Max(0, Mathf.FloorToInt(cy - radius - 1));
            int maxY = Mathf.Min(size - 1, Mathf.CeilToInt(cy + radius + 1));

            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float d2 = dx * dx + dy * dy;
                if (d2 > r2) continue;

                // 구면 캡 느낌: h = (1 - (r/R)^2)^(p)  (가장자리 부드럽게)
                float t = 1f - (d2 / r2);      // 0..1
                float cap = Mathf.Pow(t, 1.5f); // 지수 조절로 가장자리 부드럽게
                h[x, y] = Mathf.Clamp01(h[x, y] + cap * strength);
            }
        }

        // 높이 맵 살짝 블러(계단 완화)
        BoxBlurInPlace(h, size, size, radius:1);

        // Height → Normal 변환
        var colors = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            int xl = Mathf.Max(x - 1, 0);
            int xr = Mathf.Min(x + 1, size - 1);
            int yd = Mathf.Max(y - 1, 0);
            int yu = Mathf.Min(y + 1, size - 1);

            float dhdx = (h[xr, y] - h[xl, y]) * bumpScale;
            float dhdy = (h[x, yu] - h[x, yd]) * bumpScale;

            Vector3 n = new Vector3(-dhdx, -dhdy, 1f);
            n.Normalize();

            byte r = (byte)Mathf.RoundToInt((n.x * 0.5f + 0.5f) * 255f);
            byte g = (byte)Mathf.RoundToInt((n.y * 0.5f + 0.5f) * 255f);
            byte b = (byte)Mathf.RoundToInt((n.z * 0.5f + 0.5f) * 255f);
            colors[y * size + x] = new Color32(r, g, b, 255);
        }

        var normalTex = new Texture2D(size, size, TextureFormat.RGBA32, true, true);
        normalTex.wrapMode = TextureWrapMode.Repeat;
        normalTex.filterMode = FilterMode.Trilinear;
        normalTex.SetPixels32(colors);
        normalTex.Apply(false, false);

        // 저장
        string absPath = EditorUtility.SaveFilePanel("Save Droplet Normal (PNG)", Application.dataPath, "DropletNormal", "png");
        if (!string.IsNullOrEmpty(absPath))
        {
            File.WriteAllBytes(absPath, normalTex.EncodeToPNG());
            TryImportNormalSettings(absPath, isNormalMap:true);
            Debug.Log($"Droplet normal saved: {absPath}");
        }
        Object.DestroyImmediate(normalTex);
    }

    // 간단한 BoxBlur
    private static void BoxBlurInPlace(float[,] src, int w, int h, int radius)
    {
        if (radius <= 0) return;

        float[,] tmp = new float[w, h];
        int dia = radius * 2 + 1;
        float inv = 1f / dia;

        // 가로
        for (int y = 0; y < h; y++)
        {
            float sum = 0f;
            for (int x = -radius; x <= radius; x++)
            {
                int xi = Mathf.Clamp(x, 0, w - 1);
                sum += src[xi, y];
            }
            for (int x = 0; x < w; x++)
            {
                int xr = Mathf.Clamp(x + radius, 0, w - 1);
                int xl = Mathf.Clamp(x - radius, 0, w - 1);
                if (x > 0)
                {
                    sum += src[xr, y] - src[xl, y];
                }
                tmp[x, y] = sum * inv;
            }
        }

        // 세로
        for (int x = 0; x < w; x++)
        {
            float sum = 0f;
            for (int y = -radius; y <= radius; y++)
            {
                int yi = Mathf.Clamp(y, 0, h - 1);
                sum += tmp[x, yi];
            }
            for (int y = 0; y < h; y++)
            {
                int yu = Mathf.Clamp(y + radius, 0, h - 1);
                int yd = Mathf.Clamp(y - radius, 0, h - 1);
                if (y > 0)
                {
                    sum += tmp[x, yu] - tmp[x, yd];
                }
                src[x, y] = sum * inv;
            }
        }
    }

    private static void TryImportNormalSettings(string absPath, bool isNormalMap)
    {
        if (!absPath.StartsWith(Application.dataPath)) return;
        string assetPath = "Assets" + absPath.Substring(Application.dataPath.Length);
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return;

        if (isNormalMap)
        {
            importer.textureType = TextureImporterType.NormalMap;
            importer.sRGBTexture = false;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.mipmapEnabled = true;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
        }
        else
        {
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = false;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }
        importer.SaveAndReimport();
    }
}
#endif
