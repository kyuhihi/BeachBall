// filepath: c:\Users\Lenovo\BeachBall\Assets\98. [MiddleRipple.cs](http://_vscodecontentref_/0)
using UnityEngine;

public class MiddleRipple : MonoBehaviour
{
    [Header("Ball")]
    [SerializeField] string ballTag = "Ball";
    Transform ball;

    [Header("Ripple Material Target")]
    [Tooltip("리플 머티리얼이 붙은 Quad/Plane 의 Renderer")]
    [SerializeField] Renderer rippleRenderer;

    [Header("Trigger Settings")]
    [Tooltip("연속 튐 방지 최소 시간")]
    [SerializeField] float triggerCooldown = 0.15f;
    [Tooltip("Plane 에 거의 붙어 있을 때 발생하는 노이즈 억제(거리)")]
    [SerializeField] float dotDeadZone = 0.002f;
    [Tooltip("이동량이 너무 작으면 무시")]
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

        // 초기 MaxRadius 셋팅
        rippleRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(PropMaxRadius, maxRadius);
        rippleRenderer.SetPropertyBlock(mpb);
    }

    void Update()
    {
        if (!ball || !rippleRenderer) return;

        // Plane 기준점/Normal (Quad가 XY 평면이면 forward, XZ면 up)
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

        // 쉐이더 PlaneMode와 일치하게 좌표 전달 (기본: XY 평면, worldPos.x/y)
        mpb.SetVector(PropImpactPos, new Vector4(worldPos.x, worldPos.y, worldPos.z, 0f));
        mpb.SetFloat(PropCurrentRadius, startRadius);
        mpb.SetFloat(PropMaxRadius, maxRadius);

        rippleRenderer.SetPropertyBlock(mpb);
    }
}
