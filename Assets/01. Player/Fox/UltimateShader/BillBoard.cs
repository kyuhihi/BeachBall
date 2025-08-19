using UnityEngine;

public class BillBoard : MonoBehaviour
{
    Transform m_CamTransform;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_CamTransform = Camera.main.transform;
    }

    // Update is called once per frame
    void Update()
    {
        transform.LookAt(m_CamTransform.position); ;
    }
}
