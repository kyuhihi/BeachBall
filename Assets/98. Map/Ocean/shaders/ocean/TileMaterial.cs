using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class TileMaterial : MonoBehaviour
{
    [Header("타일 크기")]
    [SerializeField] private float sizeX = 10f;
    [SerializeField] private float sizeZ = 10f; // +Z로 계산, 메쉬는 -Z로 배치해도 OK

    [Header("카메라 근접 가중(비균일 분할)")]
    [SerializeField] private float minStep = 0.1f;     // 카메라 근처 최소 간격
    [SerializeField] private float maxStep = 1.0f;     // 먼 곳 최대 간격
    [SerializeField] private float focusRadius = 6f;   // 카메라 영향 반경(축 기준)
    [SerializeField] private float falloff = 1.5f;     // 반경에 따른 간격 증가 곡률

    [Header("자동 리빌드")]
    [SerializeField] private bool autoRebuild = true;
    [SerializeField] private float rebuildDistanceThreshold = 0.5f;

    [Header("피봇 옵션")]
    [SerializeField] private bool centerPivot = true; // 피봇을 중앙으로 배치

    [Header("렌더 옵션")]
    [SerializeField] private bool flipWinding = false; // 삼각형 와인딩 반전

    private Vector2 _lastCamLocalXZ;
    private Mesh _mesh;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        BuildAdaptiveMesh(Camera.main);
    }

    void Update()
    {
        if (!autoRebuild) return;
        var cam = Camera.main;
        if (cam == null) return;

        Vector3 camLocal = transform.InverseTransformPoint(cam.transform.position);
        Vector2 camXZ = new Vector2(camLocal.x, camLocal.z);

        if ((camXZ - _lastCamLocalXZ).sqrMagnitude >= rebuildDistanceThreshold * rebuildDistanceThreshold)
        {
            BuildAdaptiveMesh(cam);
        }
    }

    void BuildAdaptiveMesh(Camera cam = null)
    {
        if (cam == null) cam = Camera.main;

        float halfX = sizeX * 0.5f;
        float halfZ = sizeZ * 0.5f;

        // 카메라 로컬 위치(XZ) 계산
        Vector3 camLocal = cam ? transform.InverseTransformPoint(cam.transform.position) : Vector3.zero;

        // 재빌드 거리를 위해 '로컬 좌표' 그대로 저장(Update의 camLocal과 동일 기준)
        _lastCamLocalXZ = new Vector2(camLocal.x, camLocal.z);

        // 비균일 분할용 카메라 좌표는 0~size 범위로 변환
        float camX01 = Mathf.Clamp(camLocal.x + halfX, 0f, sizeX);
        float camZ01 = Mathf.Clamp(camLocal.z + halfZ, 0f, sizeZ);

        // 축별 비균일 좌표 생성(0~size 범위)
        List<float> xs = GenerateAxisCoordinates(sizeX, camX01, minStep, maxStep, focusRadius, falloff);
        List<float> zs = GenerateAxisCoordinates(sizeZ, camZ01, minStep, maxStep, focusRadius, falloff);

        int xCount = xs.Count;
        int zCount = zs.Count;
        int vertCount = xCount * zCount;
        int quadCount = (xCount - 1) * (zCount - 1);

        var vertices = new Vector3[vertCount];
        var normals = new Vector3[vertCount];
        var uv = new Vector2[vertCount];
        var triangles = new int[quadCount * 6];

        // 버텍스 배치: 피봇을 중앙으로 오도록 halfX/halfZ만큼 좌우/앞뒤로 오프셋
        for (int j = 0; j < zCount; j++)
        {
            for (int i = 0; i < xCount; i++)
            {
                int idx = j * xCount + i;
                float x = xs[i];
                float z = zs[j];

                float vx = centerPivot ? (x - halfX) : x;
                float vz = centerPivot ? (z - halfZ) : z;

                vertices[idx] = new Vector3(vx, 0f, vz);
                normals[idx] = Vector3.up;
                uv[idx] = new Vector2(x / Mathf.Max(sizeX, 0.0001f), z / Mathf.Max(sizeZ, 0.0001f));
            }
        }

        // 인덱스 생성
        int t = 0;
        for (int j = 0; j < zCount - 1; j++)
        {
            for (int i = 0; i < xCount - 1; i++)
            {
                int i0 = j * xCount + i;
                int i1 = i0 + 1;
                int i2 = i0 + xCount;
                int i3 = i2 + 1;

                if (!flipWinding)
                {
                    // 앞면이 위를 향하도록(일반적인 CCW 기준)
                    triangles[t++] = i0; triangles[t++] = i2; triangles[t++] = i1;
                    triangles[t++] = i1; triangles[t++] = i2; triangles[t++] = i3;
                }
                else
                {
                    // 와인딩 반전
                    triangles[t++] = i0; triangles[t++] = i1; triangles[t++] = i2;
                    triangles[t++] = i1; triangles[t++] = i3; triangles[t++] = i2;
                }
            }
        }

        if (_mesh == null)
        {
            _mesh = new Mesh();
            _mesh.name = "AdaptiveTileMesh";
        }
        else
        {
            _mesh.Clear();
        }

        _mesh.vertices = vertices;
        _mesh.triangles = triangles;
        _mesh.normals = normals; // 평면이면 재계산 불필요
        _mesh.uv = uv;

        var mf = GetComponent<MeshFilter>();
        var mc = GetComponent<MeshCollider>();

        mf.sharedMesh = _mesh;
        mc.sharedMesh = null;     // 콜라이더 갱신을 위해 null→재할당
        mc.sharedMesh = _mesh;
    }

    // 축 하나를 따라 비균일 좌표 배열 생성
    private List<float> GenerateAxisCoordinates(float length, float camCoord, float minStep, float maxStep, float focusR, float pow)
    {
        var coords = new List<float>(Mathf.CeilToInt(length / minStep) + 2);
        float pos = 0f;
        coords.Add(0f);

        // 안전장치: 과도 루프 방지
        int safety = 0, safetyMax = 100000;

        while (pos < length && safety++ < safetyMax)
        {
            // 카메라에서의 거리 비율 → 스텝 크기 보간
            float dist = Mathf.Abs((pos - camCoord));
            float t = Mathf.Clamp01(dist / Mathf.Max(focusR, 0.0001f));
            t = Mathf.Pow(t, Mathf.Max(0.0001f, pow)); // falloff
            float step = Mathf.Lerp(minStep, maxStep, t);

            // 최소 스텝 보장
            step = Mathf.Max(0.0001f, step);

            float next = pos + step;
            if (next > length) next = length;

            // 동일 좌표 중복 방지
            if (next - coords[coords.Count - 1] > 1e-6f)
            {
                coords.Add(next);
            }

            pos = next;
        }

        // 최종 보정: 꼭 끝 점이 length가 되도록 보장
        if (coords[coords.Count - 1] != length)
        {
            coords.Add(length);
        }

        return coords;
    }
}
