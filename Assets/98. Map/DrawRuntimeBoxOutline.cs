using UnityEngine;

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

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        box = GetComponent<BoxCollider>();

        lr.positionCount = 16;
        lr.loop = false;
        lr.useWorldSpace = true;

        // Emission이 적용된 머티리얼 생성
        Material glowMat = new Material(Shader.Find("Unlit/Color"));
        glowMat.SetColor("_Color", lineColor * 5.0f); // 밝기 증가

        lr.material = glowMat;
        lr.startColor = lr.endColor = lineColor;
        lr.startWidth = lr.endWidth = lineWidth;

        if (m_Turn180OnRunTime != Vector3.zero)
        {
            Vector3 euler = transform.rotation.eulerAngles;
            euler += m_Turn180OnRunTime;
            transform.rotation = Quaternion.Euler(euler);

        }
    }

    void Update()
    {
        Vector3 c = box.center;
        Vector3 s = box.size * 0.5f;
        Transform t = transform;

        Vector3[] corners = new Vector3[8];
        corners[0] = t.TransformPoint(c + new Vector3(-s.x, -s.y, -s.z));
        corners[1] = t.TransformPoint(c + new Vector3(s.x, -s.y, -s.z));
        corners[2] = t.TransformPoint(c + new Vector3(s.x, -s.y, s.z));
        corners[3] = t.TransformPoint(c + new Vector3(-s.x, -s.y, s.z));
        corners[4] = t.TransformPoint(c + new Vector3(-s.x, s.y, -s.z));
        corners[5] = t.TransformPoint(c + new Vector3(s.x, s.y, -s.z));
        corners[6] = t.TransformPoint(c + new Vector3(s.x, s.y, s.z));
        corners[7] = t.TransformPoint(c + new Vector3(-s.x, s.y, s.z));

        // 12 edges + 4 to close the box
        Vector3[] lines = new Vector3[]
        {
            corners[0], corners[1], corners[1], corners[2], corners[2], corners[3], corners[3], corners[0],
            corners[4], corners[5], corners[5], corners[6], corners[6], corners[7], corners[7], corners[4],
            // verticals
            corners[0], corners[4], corners[1], corners[5], corners[2], corners[6], corners[3], corners[7]
        };

        lr.positionCount = lines.Length;
        lr.SetPositions(lines);
    }
}
