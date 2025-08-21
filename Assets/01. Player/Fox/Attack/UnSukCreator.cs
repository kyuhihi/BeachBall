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

    [Header("Center/Radius (hitPoint 기준)")]
    [SerializeField] private Vector3 baseSpawnOriginPoint = new Vector3(11.5f, 25.58f, -20.13f);
    [SerializeField] private Vector3 baseHitPoint        = new Vector3(-0.370f, 0.0f, 4.02f);
    [SerializeField] private float spawnRadius = 8f; // hitPoint를 중심으로 사용할 반지름

    private Vector3 spawnOriginPoint; // side에 따라 계산된 실제 값
    private Vector3 hitPoint;         // side에 따라 계산된 실제 값

    private enum CourtSide { Right, Left }
    [SerializeField] private CourtSide side = CourtSide.Right;

    private readonly uint m_SpawnMaxCnt = 20;

    [Header("Gizmos")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField, Range(8, 128)] private int circleSegments = 64;
    [SerializeField] private Color rightColor = new Color(0.2f, 0.9f, 1f, 1f);
    [SerializeField] private Color leftColor  = new Color(1f, 0.4f, 0.8f, 1f);

    private void Awake()     { ApplySide(); }
#if UNITY_EDITOR
    private void OnValidate(){ ApplySide(); }
#endif

    [ContextMenu("Toggle Side (Left/Right)")]
    public void ToggleSide()
    {
        ApplySide();
    }

    private void ApplySide()
    {
        if(transform.parent.position.z > 0f)
        {
            side = CourtSide.Left;
        }
        else
        {
            side = CourtSide.Right;
        }
        // z축 기준 미러링 규칙(기존 코드의 의도 반영)
        if (side == CourtSide.Right)
        {
            spawnOriginPoint = new Vector3(baseSpawnOriginPoint.x, baseSpawnOriginPoint.y, -Mathf.Abs(baseSpawnOriginPoint.z));
            hitPoint = new Vector3(baseHitPoint.x, baseHitPoint.y, Mathf.Abs(baseHitPoint.z));
        }
        else // Left
        {
            spawnOriginPoint = new Vector3(baseSpawnOriginPoint.x, baseSpawnOriginPoint.y, Mathf.Abs(baseSpawnOriginPoint.z));
            hitPoint = new Vector3(baseHitPoint.x, baseHitPoint.y, -Mathf.Abs(baseHitPoint.z));
        }
    }

    public void SpawnUnSuk()
    {
        // hitPoint(센터)와 반지름 사용
        Vector3 center = new Vector3(hitPoint.x, hitPoint.y + 0.01f, hitPoint.z);
        float radius = Mathf.Max(0f, spawnRadius);

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
            Vector3 target = new Vector3(center.x, center.y, center.z) + offsetXZ;

            Vector3 lookDir = target - pos;
            Quaternion rot = lookDir.sqrMagnitude > 1e-6f
                ? Quaternion.FromToRotation(Vector3.forward, lookDir)
                : transform.rotation;

            Instantiate(unSukPrefab, pos, rot);
        }
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        // Gizmo 색상은 사이드에 따라 변경
        Gizmos.color = (side == CourtSide.Right) ? rightColor : leftColor;

        // 센터/원
        Vector3 center = Application.isPlaying ? new Vector3(hitPoint.x, hitPoint.y + 0.01f, hitPoint.z)
                                               : new Vector3(
                                                     (side == CourtSide.Right ? baseHitPoint.x : baseHitPoint.x),
                                                     baseHitPoint.y + 0.01f,
                                                     (side == CourtSide.Right ?  Mathf.Abs(baseHitPoint.z)
                                                                              : -Mathf.Abs(baseHitPoint.z)));
        DrawCircleXZ(center, Mathf.Max(0f, spawnRadius), circleSegments);

        // forward 화살표
        Vector3 pos = Application.isPlaying ? spawnOriginPoint : baseSpawnOriginPoint;
        if (side == CourtSide.Right) pos.z = -Mathf.Abs(pos.z); else pos.z = Mathf.Abs(pos.z);
        DrawArrow(pos, transform.forward, 2.0f, 0.35f, 0.2f);
    }

    private static void DrawArrow(Vector3 pos, Vector3 dir, float length, float headLen, float headWidth)
    {
        dir = dir.normalized;
        Vector3 tip = pos + dir * length;
        Gizmos.DrawLine(pos, tip);
        // 간단 화살촉
        Vector3 up = Mathf.Abs(Vector3.Dot(dir, Vector3.up)) > 0.99f ? Vector3.right : Vector3.up;
        Vector3 right = Vector3.Cross(dir, up).normalized;
        Vector3 basePt = tip - dir * headLen;
        Gizmos.DrawLine(tip, basePt + right * headWidth);
        Gizmos.DrawLine(tip, basePt - right * headWidth);
    }

    private static void DrawCircleXZ(Vector3 center, float radius, int segments)
    {
        if (radius <= 0f || segments < 3) return;
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
