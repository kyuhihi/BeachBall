using UnityEngine;

public class CloudCollector : MonoBehaviour
{
    private CloudGenerator cloudGenerator;
    private void Start()
    {
        cloudGenerator = FindFirstObjectByType<CloudGenerator>();
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Cloud"))
        {
            cloudGenerator.EnqueueCloud(other.gameObject);
            // 구름을 수집하는 로직
        }
    }
 
}
