using UnityEngine;

public class WaveGenerator : MonoBehaviour
{
    public GameObject wavePrefab; // ��� �ĵ� �޽� ������
    public GameObject waterGameObj;
    public Transform spawnPoint;  // �ĵ� ���� ��ġ
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

