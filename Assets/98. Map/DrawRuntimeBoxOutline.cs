// filepath: c:\Users\Lenovo\BeachBall\Assets\98. Map\DrawRuntimeBoxOutline.cs
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(LineRenderer))]
public class DrawRuntimeBoxOutline : MonoBehaviour
{
    [Header("라인 비주얼")]
    public Color lineColor = Color.yellow;
    public float lineWidth = 0.05f;

    [Tooltip("인스펙터에서 직접 지정할 머티리얼(예: URP Unlit).")]
    [SerializeField] private Material sourceMaterial;

    [Tooltip("실행 시 원본을 복제해서 인스턴스 머티리얼 사용 (원본 에셋 색상 변화 방지).")]
    [SerializeField] private bool instantiateMaterial = true;

    [Header("런타임 1회 회전 보정")]
    [SerializeField] private Vector3 m_Turn180OnRunTime = new Vector3(0, 180, 0);

    private LineRenderer _lr;
    private BoxCollider _box;
    private Material _runtimeMat;   // 인스턴스(옵션)

    private bool rotatedApplied = false;

    void OnEnable()
    {
        _lr = GetComponent<LineRenderer>();
        _box = GetComponent<BoxCollider>();

        // 머티리얼 세팅
        SetupMaterial();
        ApplyLineAppearance();

        // 런타임 1회 회전
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
        // 인스턴스 머티리얼만 정리
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

        // 이미 생성된 경우 재사용
        if (_runtimeMat == null)
        {
            _runtimeMat = instantiateMaterial ? Instantiate(sourceMaterial) : sourceMaterial;
        }

        // 공유할지 여부
        if (instantiateMaterial)
            _lr.sharedMaterial = _runtimeMat;          // 인스턴스(씬 전용)
        else
            _lr.sharedMaterial = sourceMaterial;       // 원본 공유
    }

    private void ApplyLineAppearance()
    {
        if (_lr == null) return;

        _lr.startWidth = _lr.endWidth = lineWidth;
        _lr.startColor = _lr.endColor = lineColor;

        // 머티리얼 컬러(가능한 프로퍼티 둘 다 대응)
        Material mat = instantiateMaterial ? _runtimeMat : sourceMaterial;
        if (mat != null)
        {
            // HDR 강조 원하면 lineColor * 값 조절
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", lineColor);
            if (mat.HasProperty("_Color"))     mat.SetColor("_Color", lineColor);
        }
    }

    void Update()
    {
        if (_box == null || _lr == null) return;

        // 8 코너 계산
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

        // 12 엣지를 LineRenderer 로 (좌표쌍)
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
