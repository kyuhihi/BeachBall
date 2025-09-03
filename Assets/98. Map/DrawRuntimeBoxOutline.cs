// filepath: c:\Users\Lenovo\BeachBall\Assets\98. Map\DrawRuntimeBoxOutline.cs
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(LineRenderer))]
public class DrawRuntimeBoxOutline : MonoBehaviour
{
    [Header("���� ���־�")]
    public Color lineColor = Color.yellow;
    public float lineWidth = 0.05f;

    [Tooltip("�ν����Ϳ��� ���� ������ ��Ƽ����(��: URP Unlit).")]
    [SerializeField] private Material sourceMaterial;

    [Tooltip("���� �� ������ �����ؼ� �ν��Ͻ� ��Ƽ���� ��� (���� ���� ���� ��ȭ ����).")]
    [SerializeField] private bool instantiateMaterial = true;

    [Header("��Ÿ�� 1ȸ ȸ�� ����")]
    [SerializeField] private Vector3 m_Turn180OnRunTime = new Vector3(0, 180, 0);

    private LineRenderer _lr;
    private BoxCollider _box;
    private Material _runtimeMat;   // �ν��Ͻ�(�ɼ�)

    private bool rotatedApplied = false;

    void OnEnable()
    {
        _lr = GetComponent<LineRenderer>();
        _box = GetComponent<BoxCollider>();

        // ��Ƽ���� ����
        SetupMaterial();
        ApplyLineAppearance();

        // ��Ÿ�� 1ȸ ȸ��
        if (Application.isPlaying && !rotatedApplied && m_Turn180OnRunTime != Vector3.zero)
        {
            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles + m_Turn180OnRunTime);
            rotatedApplied = true;
        }

        _lr.loop = false;
        _lr.useWorldSpace = true;
    }

    void OnDisable()
    {
        // �ν��Ͻ� ��Ƽ���� ����
        if (Application.isPlaying && instantiateMaterial && _runtimeMat != null)
        {
            Destroy(_runtimeMat);
            _runtimeMat = null;
        }
#if UNITY_EDITOR
        if (!Application.isPlaying && instantiateMaterial && _runtimeMat != null)
        {
            DestroyImmediate(_runtimeMat);
            _runtimeMat = null;
        }
#endif
    }

    void OnValidate()
    {
        if (!_lr) _lr = GetComponent<LineRenderer>();
        ApplyLineAppearance();
    }

    private void SetupMaterial()
    {
        if (sourceMaterial == null)
        {
            return;
        }

        // �̹� ������ ��� ����
        if (_runtimeMat == null)
        {
            _runtimeMat = instantiateMaterial ? Instantiate(sourceMaterial) : sourceMaterial;
        }

        // �������� ����
        if (instantiateMaterial)
            _lr.sharedMaterial = _runtimeMat;          // �ν��Ͻ�(�� ����)
        else
            _lr.sharedMaterial = sourceMaterial;       // ���� ����
    }

    private void ApplyLineAppearance()
    {
        if (_lr == null) return;

        _lr.startWidth = _lr.endWidth = lineWidth;
        _lr.startColor = _lr.endColor = lineColor;

        // ��Ƽ���� �÷�(������ ������Ƽ �� �� ����)
        Material mat = instantiateMaterial ? _runtimeMat : sourceMaterial;
        if (mat != null)
        {
            // HDR ���� ���ϸ� lineColor * �� ����
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", lineColor);
            if (mat.HasProperty("_Color"))     mat.SetColor("_Color", lineColor);
        }
    }

    void Update()
    {
        if (_box == null || _lr == null) return;

        // 8 �ڳ� ���
        Vector3 c = _box.center;
        Vector3 s = _box.size * 0.5f;
        Transform t = transform;

        Vector3[] v = new Vector3[8];
        v[0] = t.TransformPoint(c + new Vector3(-s.x, -s.y, -s.z));
        v[1] = t.TransformPoint(c + new Vector3( s.x, -s.y, -s.z));
        v[2] = t.TransformPoint(c + new Vector3( s.x, -s.y,  s.z));
        v[3] = t.TransformPoint(c + new Vector3(-s.x, -s.y,  s.z));
        v[4] = t.TransformPoint(c + new Vector3(-s.x,  s.y, -s.z));
        v[5] = t.TransformPoint(c + new Vector3( s.x,  s.y, -s.z));
        v[6] = t.TransformPoint(c + new Vector3( s.x,  s.y,  s.z));
        v[7] = t.TransformPoint(c + new Vector3(-s.x,  s.y,  s.z));

        // 12 ������ LineRenderer �� (��ǥ��)
        Vector3[] lines =
        {
            v[0],v[1], v[1],v[2], v[2],v[3], v[3],v[0], // bottom
            v[4],v[5], v[5],v[6], v[6],v[7], v[7],v[4], // top
            v[0],v[4], v[1],v[5], v[2],v[6], v[3],v[7]  // verticals
        };

        _lr.positionCount = lines.Length;
        _lr.SetPositions(lines);
    }
}
