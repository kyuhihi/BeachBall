using System;
using UnityEngine;

public class UnSukCreator : MonoBehaviour
{
    [Header("UnSuk Prefab")]
    public GameObject unSukPrefab;

    [Header("Spawn Area (XZ)")]
    [SerializeField] private float rangeX = 25.4f;   // X 범위(±rangeX)
    [SerializeField] private float rangeY = 3f;      // Y 범위(0 ~ rangeY)
    [SerializeField] private float rangeZ = 16.28f;  // Z 범위(±rangeZ)

    private Vector3 spawnOriginPoint = new Vector3(11.5f, 25.58f, -20.13f);
    private readonly uint m_SpawnMaxCnt = 20;

    [Header("Gizmo & Raycast")]
    [SerializeField] private LayerMask groundMask = 0;  // 바닥 레이어 지정
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
        // 전방 레이로 원의 중심/반경 계산(실패 시 기본값)
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

            // 회전: center 기준 원 안의 임의 지점(반경 r, 각도 θ)을 바라보게
            float theta = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float r = Mathf.Sqrt(UnityEngine.Random.value) * radius; // 원 영역에서 균일 랜덤
            Vector3 offsetXZ = new Vector3(Mathf.Cos(theta) * r, 0f, Mathf.Sin(theta) * r);
            Vector3 target = new Vector3(center.x, 0f, center.z) + offsetXZ;

            Vector3 lookDir = target - pos; 
            // up을 고정하지 않고, 로컬 Z(전방)를 lookDir로 맞춤(운석 낙하 각도 그대로)
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
        // 1) forward 화살표
        DrawArrow(spawnOriginPoint, transform.forward, 2.0f, 0.35f, 0.2f, arrowColor);

        // 2) 전방 레이캐스트
        Vector3 origin = spawnOriginPoint;
        Vector3 dir = transform.forward.normalized;
        if (Physics.Raycast(origin, dir, out var hit, rayDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            Gizmos.color = rayColor;
            Gizmos.DrawLine(origin, hit.point);
            Gizmos.DrawSphere(hit.point, 0.05f);

            // X/Z 범위를 기반으로 한 원의 반지름
            float radius = Mathf.Min(Mathf.Abs(rangeX), Mathf.Abs(rangeZ))*0.5f;
            Vector3 center = new Vector3(hit.point.x, hit.point.y + 0.01f, hit.point.z); // 지면과 겹침 방지로 살짝 올림
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
