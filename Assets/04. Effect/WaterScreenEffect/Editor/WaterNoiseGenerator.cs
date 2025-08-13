using UnityEngine;
using UnityEditor;

public class NoiseTextureCreator
{
    [MenuItem("Tools/Generate Noise Texture")]
    public static void GenerateNoiseTexture()
    {
        int size = 1024;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RFloat, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float value = Mathf.PerlinNoise(x * 0.1f, y * 0.1f);
                tex.SetPixel(x, y, new Color(value, value, value, 1));
            }
        }
        tex.Apply();

        // Assets 폴더에 저장
        byte[] bytes = tex.EncodeToEXR(); // RFloat 보존
        System.IO.File.WriteAllBytes(Application.dataPath + "/NoiseTexture.exr", bytes);

        AssetDatabase.Refresh();
        Debug.Log("Noise Texture saved at Assets/NoiseTexture.exr");
    }
}
