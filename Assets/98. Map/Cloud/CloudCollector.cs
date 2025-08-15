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
            // ������ �����ϴ� ����
        }
    }
 
}
