using UnityEngine;

public class BoatMovement : MonoBehaviour
{
    public Transform boat;        // 배 오브젝트
    public float waveSpeed = 3f;  // Shader와 동일
    public float waveStrength = 1f;
    public float xMultiplier = 2f; // Shader에서 pos.x * 2 한 부분
    public float offsetY = 0.5f;  // 물 위에서 띄우는 높이
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
