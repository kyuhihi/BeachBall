using UnityEngine;

public class BoatMovement : MonoBehaviour
{
    public Transform boat;
    public float waveSpeed = 3f;
    public float waveStrength = 1f;
    public float xMultiplier = 2f;
    public float xWaveStrength = 0.5f; // X 방향 흔들림 크기
    public float offsetY = 0.5f;
    public float forwardSpeed = 1f; // 앞으로 가는 속도
    public GameObject waterMesh;

    private Vector3 startPos;

    // 인스턴스별 오프셋 추가
    [SerializeField] private float phaseOffset = 0f;    // 파동 위상 오프셋
    [SerializeField] private float xSampleOffset = 0f;  // 샘플링 X 오프셋
    private float spawnTime;

    void Start()
    {
        boat = transform;
        startPos = boat.position;
        spawnTime = Time.time;

        // 기본적으로 인스턴스마다 다른 값 부여(원하면 WaveGenerator에서 직접 세팅)
        if (Mathf.Approximately(phaseOffset, 0f))
            phaseOffset = (GetInstanceID() & 0xFFFF) * 0.013f; // 임의성 부여
        if (Mathf.Approximately(xSampleOffset, 0f))
            xSampleOffset = (GetInstanceID() & 0xFFFF) * 0.01f;
    }

    void Update()
    {
        if (waterMesh == null) return;

        // 인스턴스별 시간 기준(스폰 시점부터 흐르는 시간)
        float t = Time.time - spawnTime;

        // 기본 전진 이동 (인스턴스별 시작점 기준)
        float forwardX = startPos.x + (t * forwardSpeed);

        // waterMesh 로컬 좌표 변환 + 샘플 오프셋
        Vector3 sampleWorld = new Vector3(forwardX + xSampleOffset, boat.position.y, boat.position.z);
        Vector3 localPos = waterMesh.transform.InverseTransformPoint(sampleWorld);

        // 파도 Y 높이 계산(위상 오프셋 적용)
        float arg = (localPos.x * xMultiplier) + ((t + phaseOffset) * waveSpeed);
        float waveHeight = Mathf.Sin(arg) * waveStrength;

        // 파도에 따른 X축 흔들림
        float waveXOffset = Mathf.Cos(arg) * xWaveStrength;

        // 최종 위치 적용
        boat.position = new Vector3(
            forwardX + waveXOffset,
            waterMesh.transform.position.y + waveHeight + offsetY,
            boat.position.z
        );
    }
}
