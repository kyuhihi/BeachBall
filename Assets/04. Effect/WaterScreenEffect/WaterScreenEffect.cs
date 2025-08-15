using UnityEngine;

[ExecuteInEditMode]
public class WaterScreenEffect : MonoBehaviour
{
    public Material waterMaterial;

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (waterMaterial != null)
            Graphics.Blit(src, dest, waterMaterial);
        else
            Graphics.Blit(src, dest);
    }
}
