using System;
using UnityEngine;

public class Wave : MonoBehaviour
{
    MeshRenderer meshRenderer;
    Material material;

    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            material = meshRenderer.material;
        }
    }
    void Update()
    {

        if (material != null)
        {
            float dissolveAmount = material.GetFloat("_DissolveAmount");
            if (dissolveAmount >= 1.0f)
            {
                // Reset the dissolve effect
                material.SetFloat("_DissolveAmount", 0.0f);
                return;
            }
            material.SetFloat("_DissolveAmount", Mathf.Lerp(0.0f, 1.0f, Time.time * 2f));
        }
    }
}
