using UnityEngine;

public class BoatMovement : MonoBehaviour
{
    public Transform boat;        // �� ������Ʈ
    public float waveSpeed = 3f;  // Shader�� ����
    public float waveStrength = 1f;
    public float xMultiplier = 2f; // Shader���� pos.x * 2 �� �κ�
    public float xWaveStrength = 0.5f; // X�� ������ ũ��
    public float offsetY = 0.5f;  // �� ������ ���� ����
    public GameObject waterMesh;

    private Vector3 startPos;

    private float currentWaveHeight = 0f; // Y�� �ĵ� ����
    public float GetCurrentWaveHeight()
    {
        return currentWaveHeight;
    }

    [SerializeField] private float trendDeadZone = 0.001f; // �̼� ���� ����
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

        // Y�� �ĵ�
        float waveHeight = Mathf.Sin(
            (localPos.x * xMultiplier) + (Time.time * waveSpeed)
        ) * waveStrength;

        // ����/���� �߼��� ���� currentWaveHeight ����
        if (!hasPrev)
        {
            prevWaveHeight = waveHeight; // ù ������ �ʱ�ȭ
            hasPrev = true;
            currentWaveHeight = waveHeight; // �ʱⰪ
        }
        else
        {
            float delta = waveHeight - prevWaveHeight;
            if (delta > trendDeadZone)
            {
                // Ŀ���� ��
                currentWaveHeight = Mathf.Lerp(currentWaveHeight, waveHeight, 0.1f);
            }
            else if (delta < -trendDeadZone)
            {
                // �۾����� ��
                currentWaveHeight = waveHeight;
            }
            //currentWaveHeight = waveHeight;
        }

        // X�� �ĵ� (��¦ �������Ѽ� �� �ڿ�������)
        float waveX = Mathf.Cos(
            (localPos.x * xMultiplier * 0.5f) + (Time.time * waveSpeed * 0.8f)
        ) * xWaveStrength;

        boat.position = new Vector3(
            startPos.x + waveX, // X�� �̵�
            waterMesh.transform.position.y + currentWaveHeight + offsetY, // Y�� �̵�
            boat.position.z // Z�� �״��
        );

        // ���� �������� ���� ���
        prevWaveHeight = waveHeight;
    }
}

