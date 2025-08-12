using System;
using UnityEngine;

public class Wave : MonoBehaviour
{
    MeshRenderer meshRenderer;
    BoatMovement boatMovement;
    Material material;
    

    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        boatMovement = GetComponent<BoatMovement>();
        if (meshRenderer != null)
        {
            material = meshRenderer.material;
        }
    }
    void Update()
    {
        if (material != null)
        {
            float dissolveTime = boatMovement.GetCurrentWaveHeight();

            material.SetFloat("_DissolveAmount", dissolveTime);
        }
    }
}
