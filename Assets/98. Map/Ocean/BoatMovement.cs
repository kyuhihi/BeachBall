using UnityEngine;

public class BoatMovement : MonoBehaviour
{
    public Transform boat;
    public float waveSpeed = 3f;
    public float waveStrength = 1f;
    public float xMultiplier = 2f;
    public float xWaveStrength = 0.5f; // X ���� ��鸲 ũ��
    public float offsetY = 0.5f;
    public float forwardSpeed = 1f; // ������ ���� �ӵ�
    public GameObject waterMesh;

    private Vector3 startPos;

    // �ν��Ͻ��� ������ �߰�
    [SerializeField] private float phaseOffset = 0f;    // �ĵ� ���� ������
    [SerializeField] private float xSampleOffset = 0f;  // ���ø� X ������
    private float spawnTime;

    void Start()
    {
        boat = transform;
        startPos = boat.position;
        spawnTime = Time.time;

        // �⺻������ �ν��Ͻ����� �ٸ� �� �ο�(���ϸ� WaveGenerator���� ���� ����)
        if (Mathf.Approximately(phaseOffset, 0f))
            phaseOffset = (GetInstanceID() & 0xFFFF) * 0.013f; // ���Ǽ� �ο�
        if (Mathf.Approximately(xSampleOffset, 0f))
            xSampleOffset = (GetInstanceID() & 0xFFFF) * 0.01f;
    }

    void Update()
    {
        if (waterMesh == null) return;

        // �ν��Ͻ��� �ð� ����(���� �������� �帣�� �ð�)
        float t = Time.time - spawnTime;

        // �⺻ ���� �̵� (�ν��Ͻ��� ������ ����)
        float forwardX = startPos.x + (t * forwardSpeed);

        // waterMesh ���� ��ǥ ��ȯ + ���� ������
        Vector3 sampleWorld = new Vector3(forwardX + xSampleOffset, boat.position.y, boat.position.z);
        Vector3 localPos = waterMesh.transform.InverseTransformPoint(sampleWorld);

        // �ĵ� Y ���� ���(���� ������ ����)
        float arg = (localPos.x * xMultiplier) + ((t + phaseOffset) * waveSpeed);
        float waveHeight = Mathf.Sin(arg) * waveStrength;

        // �ĵ��� ���� X�� ��鸲
        float waveXOffset = Mathf.Cos(arg) * xWaveStrength;

        // ���� ��ġ ����
        boat.position = new Vector3(
            forwardX + waveXOffset,
            waterMesh.transform.position.y + waveHeight + offsetY,
            boat.position.z
        );
    }
}
