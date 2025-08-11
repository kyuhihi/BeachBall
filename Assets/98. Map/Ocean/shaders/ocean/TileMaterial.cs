using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class TileMaterial : MonoBehaviour
{
    [Header("Ÿ�� ũ��")]
    [SerializeField] private float sizeX = 10f;
    [SerializeField] private float sizeZ = 10f; // +Z�� ���, �޽��� -Z�� ��ġ�ص� OK

    [Header("ī�޶� ���� ����(����� ����)")]
    [SerializeField] private float minStep = 0.1f;     // ī�޶� ��ó �ּ� ����
    [SerializeField] private float maxStep = 1.0f;     // �� �� �ִ� ����
    [SerializeField] private float focusRadius = 6f;   // ī�޶� ���� �ݰ�(�� ����)
    [SerializeField] private float falloff = 1.5f;     // �ݰ濡 ���� ���� ���� ���

    [Header("�ڵ� ������")]
    [SerializeField] private bool autoRebuild = true;
    [SerializeField] private float rebuildDistanceThreshold = 0.5f;

    [Header("�Ǻ� �ɼ�")]
    [SerializeField] private bool centerPivot = true; // �Ǻ��� �߾����� ��ġ

    [Header("���� �ɼ�")]
    [SerializeField] private bool flipWinding = false; // �ﰢ�� ���ε� ����

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

        // ī�޶� ���� ��ġ(XZ) ���
        Vector3 camLocal = cam ? transform.InverseTransformPoint(cam.transform.position) : Vector3.zero;

        // ����� �Ÿ��� ���� '���� ��ǥ' �״�� ����(Update�� camLocal�� ���� ����)
        _lastCamLocalXZ = new Vector2(camLocal.x, camLocal.z);

        // ����� ���ҿ� ī�޶� ��ǥ�� 0~size ������ ��ȯ
        float camX01 = Mathf.Clamp(camLocal.x + halfX, 0f, sizeX);
        float camZ01 = Mathf.Clamp(camLocal.z + halfZ, 0f, sizeZ);

        // �ະ ����� ��ǥ ����(0~size ����)
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

        // ���ؽ� ��ġ: �Ǻ��� �߾����� ������ halfX/halfZ��ŭ �¿�/�յڷ� ������
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

        // �ε��� ����
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
                    // �ո��� ���� ���ϵ���(�Ϲ����� CCW ����)
                    triangles[t++] = i0; triangles[t++] = i2; triangles[t++] = i1;
                    triangles[t++] = i1; triangles[t++] = i2; triangles[t++] = i3;
                }
                else
                {
                    // ���ε� ����
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
        _mesh.normals = normals; // ����̸� ���� ���ʿ�
        _mesh.uv = uv;

        var mf = GetComponent<MeshFilter>();
        var mc = GetComponent<MeshCollider>();

        mf.sharedMesh = _mesh;
        mc.sharedMesh = null;     // �ݶ��̴� ������ ���� null�����Ҵ�
        mc.sharedMesh = _mesh;
    }

    // �� �ϳ��� ���� ����� ��ǥ �迭 ����
    private List<float> GenerateAxisCoordinates(float length, float camCoord, float minStep, float maxStep, float focusR, float pow)
    {
        var coords = new List<float>(Mathf.CeilToInt(length / minStep) + 2);
        float pos = 0f;
        coords.Add(0f);

        // ������ġ: ���� ���� ����
        int safety = 0, safetyMax = 100000;

        while (pos < length && safety++ < safetyMax)
        {
            // ī�޶󿡼��� �Ÿ� ���� �� ���� ũ�� ����
            float dist = Mathf.Abs((pos - camCoord));
            float t = Mathf.Clamp01(dist / Mathf.Max(focusR, 0.0001f));
            t = Mathf.Pow(t, Mathf.Max(0.0001f, pow)); // falloff
            float step = Mathf.Lerp(minStep, maxStep, t);

            // �ּ� ���� ����
            step = Mathf.Max(0.0001f, step);

            float next = pos + step;
            if (next > length) next = length;

            // ���� ��ǥ �ߺ� ����
            if (next - coords[coords.Count - 1] > 1e-6f)
            {
                coords.Add(next);
            }

            pos = next;
        }

        // ���� ����: �� �� ���� length�� �ǵ��� ����
        if (coords[coords.Count - 1] != length)
        {
            coords.Add(length);
        }

        return coords;
    }
}
