using System;
using UnityEngine;

public class UnSukCreator : MonoBehaviour
{
    [Header("UnSuk Prefab")]
    public GameObject unSukPrefab;

    [Header("Spawn Area (XZ)")]
    [SerializeField] private float rangeX = 25.4f;   // X ����(��rangeX)
    [SerializeField] private float rangeY = 3f;      // Y ����(0 ~ rangeY)
    [SerializeField] private float rangeZ = 16.28f;  // Z ����(��rangeZ)

    private Vector3 spawnOriginPoint = new Vector3(11.5f, 25.58f, -20.13f);
    private readonly uint m_SpawnMaxCnt = 20;

    [Header("Gizmo & Raycast")]
    [SerializeField] private LayerMask groundMask = 0;  // �ٴ� ���̾� ����
    private float rayDistance = 1000f;
    [SerializeField] private bool drawOnlyWhenSelected = false;
    [SerializeField] private Color arrowColor = Color.cyan;
    [SerializeField] private Color rayColor = new Color(0.2f, 1f, 0.2f, 1f);
    [SerializeField] private Color circleLineColor = new Color(1f, 0.3f, 0.9f, 1f);
    [SerializeField, Range(12, 128)] private int circleSegments = 64;

    private void Start()
    {
        if (gameObject.transform.parent.position.z > 0)
        {
            Debug.Log("tt");
            spawnOriginPoint.z = Math.Abs(spawnOriginPoint.z);
            Vector3 euler = transform.localEulerAngles;
            euler.y = 203.0f;
            transform.localEulerAngles = euler;
        }
    }

    public void SpawnUnSuk()
    {
        // ���� ���̷� ���� �߽�/�ݰ� ���(���� �� �⺻��)
        Vector3 origin = spawnOriginPoint;
        Vector3 dirFwd = transform.forward.normalized;
        int layerMask = groundMask.value != 0 ? groundMask.value : Physics.DefaultRaycastLayers;
        Vector3 center;
        float radius;
        if (Physics.Raycast(origin, dirFwd, out var hit, rayDistance, layerMask, QueryTriggerInteraction.Ignore))
        {
            center = new Vector3(hit.point.x, hit.point.y + 0.01f, hit.point.z);
            radius = Mathf.Min(Mathf.Abs(rangeX), Mathf.Abs(rangeZ))* 0.5f;
        }
        else
        {
            center = origin;
            radius = Mathf.Min(Mathf.Abs(rangeX), Mathf.Abs(rangeZ));
        }

        for (uint i = 0; i < m_SpawnMaxCnt; ++i)
        {
            float rx = UnityEngine.Random.Range(-rangeX, rangeX);
            float ry = UnityEngine.Random.Range(0f, rangeY);
            float rz = UnityEngine.Random.Range(-rangeZ, rangeZ);
            Vector3 pos = spawnOriginPoint + new Vector3(rx, ry, rz);
            if (pos.x < -5f)
            {
                pos.x += UnityEngine.Random.Range(0f, 3f);
            }

            // ȸ��: center ���� �� ���� ���� ����(�ݰ� r, ���� ��)�� �ٶ󺸰�
            float theta = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float r = Mathf.Sqrt(UnityEngine.Random.value) * radius; // �� �������� ���� ����
            Vector3 offsetXZ = new Vector3(Mathf.Cos(theta) * r, 0f, Mathf.Sin(theta) * r);
            Vector3 target = new Vector3(center.x, 0f, center.z) + offsetXZ;

            Vector3 lookDir = target - pos; 
            // up�� �������� �ʰ�, ���� Z(����)�� lookDir�� ����(� ���� ���� �״��)
            Quaternion rot = lookDir.sqrMagnitude > 1e-6f
                ? Quaternion.FromToRotation(Vector3.forward, lookDir)
                : transform.rotation;

            Instantiate(unSukPrefab, pos, rot);
        }
    }

    // --- Gizmos ---
    private void OnDrawGizmos()
    {
        if (!drawOnlyWhenSelected) DrawForwardAndCircleGizmos();
    }
    private void OnDrawGizmosSelected()
    {
        if (drawOnlyWhenSelected) DrawForwardAndCircleGizmos();
    }

    private void DrawForwardAndCircleGizmos()
    {
        // 1) forward ȭ��ǥ
        DrawArrow(spawnOriginPoint, transform.forward, 2.0f, 0.35f, 0.2f, arrowColor);

        // 2) ���� ����ĳ��Ʈ
        Vector3 origin = spawnOriginPoint;
        Vector3 dir = transform.forward.normalized;
        if (Physics.Raycast(origin, dir, out var hit, rayDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            Gizmos.color = rayColor;
            Gizmos.DrawLine(origin, hit.point);
            Gizmos.DrawSphere(hit.point, 0.05f);

            // X/Z ������ ������� �� ���� ������
            float radius = Mathf.Min(Mathf.Abs(rangeX), Mathf.Abs(rangeZ))*0.5f;
            Vector3 center = new Vector3(hit.point.x, hit.point.y + 0.01f, hit.point.z); // ����� ��ħ ������ ��¦ �ø�
            DrawCircleXZ(center, radius, circleSegments, circleLineColor);
        }
        else
        {
            Gizmos.color = rayColor;
            Gizmos.DrawLine(origin, origin + dir * rayDistance);
        }
    }

    private static void DrawArrow(Vector3 pos, Vector3 dir, float length, float headLen, float headWidth, Color color)
    {
        dir = dir.normalized;
        Vector3 tip = pos + dir * length;
        Gizmos.color = color;
        Gizmos.DrawLine(pos, tip);

        Vector3 up = Mathf.Abs(Vector3.Dot(dir, Vector3.up)) > 0.99f ? Vector3.right : Vector3.up;
        Vector3 right = Vector3.Cross(dir, up).normalized;
        Vector3 upOrtho = Vector3.Cross(right, dir).normalized;
        Vector3 basePt = tip - dir * headLen;
        Gizmos.DrawLine(tip, basePt + right * headWidth);
        Gizmos.DrawLine(tip, basePt - right * headWidth);
        Gizmos.DrawLine(tip, basePt + upOrtho * headWidth);
        Gizmos.DrawLine(tip, basePt - upOrtho * headWidth);
    }

    private static void DrawCircleXZ(Vector3 center, float radius, int segments, Color lineColor)
    {
        if (radius <= 0f || segments < 3) return;
        Gizmos.color = lineColor;
        Vector3 prev = center + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float ang = (i / (float)segments) * Mathf.PI * 2f;
            Vector3 cur = center + new Vector3(Mathf.Cos(ang) * radius, 0f, Mathf.Sin(ang) * radius);
            Gizmos.DrawLine(prev, cur);
            prev = cur;
        }
    }
}
