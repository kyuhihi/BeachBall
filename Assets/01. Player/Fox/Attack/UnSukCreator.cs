using System;
using UnityEngine;

public class UnSukCreator : MonoBehaviour
{
    [Header("UnSuk Prefab")]
    public GameObject unSukPrefab;

    [Header("Spawn Area (XZ)")]
    private float rangeX = 25.4f;   // X 범위(±rangeX)
    private float rangeY = 2f;     // Y 범위(0 ~ rangeY)
    private float rangeZ = 16.28f;   // Z 범위(±rangeZ)
    
    private Vector3 spawnOriginPoint;
    private readonly uint m_SpawnMaxCnt = 20;

    private void Start()
    {
        spawnOriginPoint = transform.position; // Use the current object's position as the spawn origin point
        spawnOriginPoint.x = Mathf.Abs(spawnOriginPoint.x);
    }

    public void SpawnUnSuk()
    {
        for (uint i = 0; i < m_SpawnMaxCnt; ++i)
        {
            float rx = UnityEngine.Random.Range(-rangeX, rangeX);
            float ry = UnityEngine.Random.Range(0f, rangeY);
            float rz = UnityEngine.Random.Range(-rangeZ, rangeZ);
            Vector3 pos = spawnOriginPoint + new Vector3(rx, ry, rz);
            Instantiate(unSukPrefab, pos, transform.rotation);
        }
    }
}
