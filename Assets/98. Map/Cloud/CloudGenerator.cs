using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class CloudGenerator : MonoBehaviour
{
    [SerializeField]
    private GameObject CloudPrefab;
    private float spawnInterval = 2f;

    private Queue<GameObject> cloudsList = new Queue<GameObject>();

    void Start()
    {

    }
    void Update()
    {
        spawnInterval -= Time.deltaTime;
        if (spawnInterval <= 0f)
        {
            SpawnCloud();
        }
    }

    void SpawnCloud()
    {
        Vector3 randomOffset = new Vector3(Random.Range(-2f, 2f), Random.Range(-2f, 0.5f), 0.0f);
        Vector3 SpawnPosition = transform.position + randomOffset;
        GameObject cloud = null;
        if (cloudsList.Count > 0)
        {
            cloud = cloudsList.Dequeue();
        }
        else
        {
            cloud = Instantiate(CloudPrefab, SpawnPosition, transform.rotation);
        }
        cloud.transform.SetParent(this.transform);
        spawnInterval = Random.Range(2f, 10f);
    }

    public void EnqueueCloud(GameObject cloud)
    {
        cloudsList.Enqueue(cloud);
    }
}

