using UnityEngine;

[ExecuteAlways] // 에디터/런타임 모두 실행
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(LineRenderer))]
public class DrawRuntimeBoxOutline : MonoBehaviour
{
    public Color lineColor = Color.yellow;
    public float lineWidth = 0.05f;

    [SerializeField]
    private Vector3 m_Turn180OnRunTime = new Vector3(0, 180, 0);

    private LineRenderer lr;
    private BoxCollider box;

    // 에디터에서 머티리얼 누수 방지용
    private Material glowMat;
    private bool rotatedApplied = false;

    void OnEnable()
    {
        lr = GetComponent<LineRenderer>();
        box = GetComponent<BoxCollider>();

        if (glowMat == null)
        {
            glowMat = new Material(Shader.Find("Unlit/Color"));
            glowMat.hideFlags = HideFlags.HideAndDontSave;
        }

        lr.loop = false;
        lr.useWorldSpace = true;
        ApplyLineAppearance();

        // 런타임에서만 회전 1회 적용 (에디터에서는 누적 방지)
        if (Application.isPlaying && !rotatedApplied && m_Turn180OnRunTime != Vector3.zero)
        {
            var euler = transform.rotation.eulerAngles + m_Turn180OnRunTime;
            transform.rotation = Quaternion.Euler(euler);
            rotatedApplied = true;
        }
    }

    void OnDisable()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && glowMat != null)
        {
            DestroyImmediate(glowMat);
            glowMat = null;
        }
#endif
    }

    void OnValidate()
    {
        // 에디터에서 값 변경 시 즉시 반영
        if (lr == null) lr = GetComponent<LineRenderer>();
        ApplyLineAppearance();
    }

    private void ApplyLineAppearance()
    {
        if (lr == null) return;
        if (glowMat == null)
        {
            glowMat = new Material(Shader.Find("Unlit/Color"));
            glowMat.hideFlags = HideFlags.HideAndDontSave;
        }
        glowMat.SetColor("_Color", lineColor * 5.0f);
        lr.material = glowMat;
        lr.startColor = lr.endColor = lineColor;
        lr.startWidth = lr.endWidth = lineWidth;
    }

    void Update()
    {
        if (box == null) return;

        Vector3 c = box.center;
        Vector3 s = box.size * 0.5f;
        Transform t = transform;

        Vector3[] corners = new Vector3[8];
        corners[0] = t.TransformPoint(c + new Vector3(-s.x, -s.y, -s.z));
        corners[1] = t.TransformPoint(c + new Vector3( s.x, -s.y, -s.z));
        corners[2] = t.TransformPoint(c + new Vector3( s.x, -s.y,  s.z));
        corners[3] = t.TransformPoint(c + new Vector3(-s.x, -s.y,  s.z));
        corners[4] = t.TransformPoint(c + new Vector3(-s.x,  s.y, -s.z));
        corners[5] = t.TransformPoint(c + new Vector3( s.x,  s.y, -s.z));
        corners[6] = t.TransformPoint(c + new Vector3( s.x,  s.y,  s.z));
        corners[7] = t.TransformPoint(c + new Vector3(-s.x,  s.y,  s.z));

        // 12개 엣지
        Vector3[] lines =
        {
            // bottom
            corners[0], corners[1], corners[1], corners[2], corners[2], corners[3], corners[3], corners[0],
            // top
            corners[4], corners[5], corners[5], corners[6], corners[6], corners[7], corners[7], corners[4],
            // verticals
            corners[0], corners[4], corners[1], corners[5], corners[2], corners[6], corners[3], corners[7]
        };

        lr.positionCount = lines.Length;
        lr.SetPositions(lines);
    }
}
