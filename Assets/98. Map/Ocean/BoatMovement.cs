using UnityEngine;

public class BoatMovement : MonoBehaviour
{
    public Transform boat;        // �� ������Ʈ
    public float waveSpeed = 3f;  // Shader�� ����
    public float waveStrength = 1f;
    public float xMultiplier = 2f; // Shader���� pos.x * 2 �� �κ�
    public float offsetY = 0.5f;  // �� ������ ���� ����
    public GameObject waterMesh;


    void Start()
    {
        boat = gameObject.transform;
    }

    void Update()
    {
        Vector3 localPos = waterMesh.transform.InverseTransformPoint(boat.position);
        float waveHeight = Mathf.Sin(
            (localPos.x * xMultiplier) + (Time.time * waveSpeed)
        ) * waveStrength;
        boat.position = new Vector3(boat.position.x, waterMesh.transform.position.y + waveHeight + offsetY, boat.position.z);

    }
}
