using UnityEngine;

public class BoatMovement : MonoBehaviour
{
    public Transform boat;        // 배 오브젝트
    public float waveSpeed = 3f;  // Shader와 동일
    public float waveStrength = 1f;
    public float xMultiplier = 2f; // Shader에서 pos.x * 2 한 부분
    public float xWaveStrength = 0.5f; // X축 움직임 크기
    public float offsetY = 0.5f;  // 물 위에서 띄우는 높이
    public GameObject waterMesh;

    private Vector3 startPos;

    private float currentWaveHeight = 0f; // Y축 파도 높이
    public float GetCurrentWaveHeight()
    {
        return currentWaveHeight;
    }

    [SerializeField] private float trendDeadZone = 0.001f; // 미세 변동 무시
    private float prevWaveHeight = 0f;
    private bool hasPrev = false;

    void Start()
    {
        boat = transform;
        startPos = boat.position;
    }

    void Update()
    {
        Vector3 localPos = waterMesh.transform.InverseTransformPoint(boat.position);

        // Y축 파도
        float waveHeight = Mathf.Sin(
            (localPos.x * xMultiplier) + (Time.time * waveSpeed)
        ) * waveStrength;

        // 증가/감소 추세에 따라 currentWaveHeight 설정
        if (!hasPrev)
        {
            prevWaveHeight = waveHeight; // 첫 프레임 초기화
            hasPrev = true;
            currentWaveHeight = waveHeight; // 초기값
        }
        else
        {
            float delta = waveHeight - prevWaveHeight;
            if (delta > trendDeadZone)
            {
                // 커지는 중
                currentWaveHeight = Mathf.Lerp(currentWaveHeight, waveHeight, 0.1f);
            }
            else if (delta < -trendDeadZone)
            {
                // 작아지는 중
                currentWaveHeight = waveHeight;
            }
            //currentWaveHeight = waveHeight;
        }

        // X축 파도 (살짝 지연시켜서 더 자연스럽게)
        float waveX = Mathf.Cos(
            (localPos.x * xMultiplier * 0.5f) + (Time.time * waveSpeed * 0.8f)
        ) * xWaveStrength;

        boat.position = new Vector3(
            startPos.x + waveX, // X축 이동
            waterMesh.transform.position.y + currentWaveHeight + offsetY, // Y축 이동
            boat.position.z // Z축 그대로
        );

        // 다음 프레임을 위한 기록
        prevWaveHeight = waveHeight;
    }
}

