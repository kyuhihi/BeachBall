// filepath: c:\Users\Lenovo\BeachBall\Assets\98. [MiddleRipple.cs](http://_vscodecontentref_/0)
using UnityEngine;

public class MiddleRipple : MonoBehaviour
{
    [Header("Ball")]
    [SerializeField] string ballTag = "Ball";
    Transform ball;

    [Header("Ripple Material Target")]
    [Tooltip("���� ��Ƽ������ ���� Quad/Plane �� Renderer")]
    [SerializeField] Renderer rippleRenderer;

    [Header("Trigger Settings")]
    [Tooltip("���� Ʀ ���� �ּ� �ð�")]
    [SerializeField] float triggerCooldown = 0.15f;
    [Tooltip("Plane �� ���� �پ� ���� �� �߻��ϴ� ������ ����(�Ÿ�)")]
    [SerializeField] float dotDeadZone = 0.002f;
    [Tooltip("�̵����� �ʹ� ������ ����")]
    [SerializeField] float minTravelForTrigger = 0.02f;

    [Header("Ripple Reset")]
    [SerializeField] float startRadius = 0f;
    [SerializeField] float maxRadius = 15f;

    // Shader property names
    static readonly int PropImpactPos     = Shader.PropertyToID("_ImpactPos");
    static readonly int PropCurrentRadius = Shader.PropertyToID("_CurrentRadius");
    static readonly int PropMaxRadius     = Shader.PropertyToID("_MaxRadius");

    MaterialPropertyBlock mpb;
    float lastDot;
    bool hasLast;
    float lastTriggerTime;
    Vector3 lastBallPos;

    void Awake()
    {
        if (!rippleRenderer) rippleRenderer = GetComponent<Renderer>();
        mpb = new MaterialPropertyBlock();
    }

    void Start()
    {
        var go = GameObject.FindWithTag(ballTag);
        if (go) ball = go.transform;
        if (ball) lastBallPos = ball.position;

        // �ʱ� MaxRadius ����
        rippleRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(PropMaxRadius, maxRadius);
        rippleRenderer.SetPropertyBlock(mpb);
    }

    void Update()
    {
        if (!ball || !rippleRenderer) return;

        // Plane ������/Normal (Quad�� XY ����̸� forward, XZ�� up)
        Vector3 planePoint  = transform.position;
        Vector3 planeNormal = transform.forward.normalized;

        Vector3 toBall = ball.position - planePoint;
        float dot = Vector3.Dot(toBall, planeNormal);

        float moved = (ball.position - lastBallPos).magnitude;

        if (!hasLast)
        {
            hasLast = true;
            lastDot = dot;
            lastBallPos = ball.position;
            return;
        }

        if (Mathf.Abs(dot) < dotDeadZone && Mathf.Abs(lastDot) < dotDeadZone)
        {
            lastDot = dot;
            lastBallPos = ball.position;
            return;
        }

        bool prevPos = lastDot > 0f;
        bool nowPos  = dot > 0f;
        bool crossed = prevPos != nowPos;

        if (crossed && moved >= minTravelForTrigger && Time.time - lastTriggerTime >= triggerCooldown)
        {
            TriggerRipple(ball.position);
            lastTriggerTime = Time.time;
        }

        lastDot = dot;
        lastBallPos = ball.position;
    }

    void TriggerRipple(Vector3 worldPos)
    {
        rippleRenderer.GetPropertyBlock(mpb);

        // ���̴� PlaneMode�� ��ġ�ϰ� ��ǥ ���� (�⺻: XY ���, worldPos.x/y)
        mpb.SetVector(PropImpactPos, new Vector4(worldPos.x, worldPos.y, worldPos.z, 0f));
        mpb.SetFloat(PropCurrentRadius, startRadius);
        mpb.SetFloat(PropMaxRadius, maxRadius);

        rippleRenderer.SetPropertyBlock(mpb);
    }
}
