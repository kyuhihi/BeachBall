using UnityEngine;

public class WaveGenerator : MonoBehaviour
{
    public GameObject wavePrefab; // 흰색 파도 메쉬 프리팹
    public GameObject waterGameObj;
    public Transform spawnPoint;  // 파도 시작 위치
    public float spawnInterval = 2f;

    void Start()
    {
        //InvokeRepeating(nameof(SpawnWave), 0, spawnInterval);
    }

    void SpawnWave()
    {
        GameObject wave = Instantiate(wavePrefab, spawnPoint.position, spawnPoint.rotation);
        wave.GetComponent<BoatMovement>().waterMesh = waterGameObj;
        wave.transform.SetParent(this.transform);
    }
}

