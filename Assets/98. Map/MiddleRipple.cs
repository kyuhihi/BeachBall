using Unity.VisualScripting;
using UnityEngine;

public class MiddleRipple : MonoBehaviour
{
    [Header("Ball")]
    [SerializeField] string ballTag = "Ball";
    Transform ball;

    [Header("Ripple Material Target")]
    [Tooltip("���� ��Ƽ������ ���� Quad/Plane �� Renderer")]
    [SerializeField] Renderer rippleRenderer;

    [Header("Ripple Config")]
    [SerializeField] float startRadius = 0f;
    [SerializeField] float maxRadius = 15f;
    [SerializeField] float expandSpeed = 5f;       // �ݰ� ���� �ӵ�
    [SerializeField] float alphaStart = 0.85f;     // ���� ����
    [SerializeField] float alphaFadeSpeed = 0.3f;  // ���� ���� �ӵ�

    // Shader property IDs
    static readonly int PropImpactPos = Shader.PropertyToID("_ImpactPos");
    static readonly int PropCurrentRadius = Shader.PropertyToID("_CurrentRadius");
    static readonly int PropMaxRadius = Shader.PropertyToID("_MaxRadius");
    static readonly int PropAlpha = Shader.PropertyToID("_Alpha");

    MaterialPropertyBlock mpb;
    float lastDot;
    bool hasLast;
    float lastTriggerTime;
    Vector3 lastBallPos;

    // ���� ���� ����
    float currentRadius;
    float currentAlpha;
    bool isRippleActive;
    Vector3 currentImpactPos;

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

        // �ʱⰪ (������ ������ �ʵ��� ����)
        ResetRipple();
    }

    void Update()
    {
        if (!ball || !rippleRenderer) return;

        DetectTrigger();
        UpdateRipple();
    }

    void DetectTrigger()
    {

        TriggerRipple(ball.position);

        lastBallPos = ball.position;
        return;

    }

    void TriggerRipple(Vector3 worldPos)
    {
        if ((lastBallPos.z < 0.0f && worldPos.z < 0.0f) || (lastBallPos.z > 0.0f && worldPos.z > 0.0f))
            return;
        // �׻� ���� ����
            currentImpactPos = worldPos;
        currentRadius = startRadius;
        currentAlpha = alphaStart;
        isRippleActive = true;

        rippleRenderer.GetPropertyBlock(mpb);
        mpb.SetVector(PropImpactPos, new Vector4(worldPos.x, worldPos.y, worldPos.z, 0f));
        mpb.SetFloat(PropCurrentRadius, currentRadius);
        mpb.SetFloat(PropAlpha, currentAlpha);
        mpb.SetFloat(PropMaxRadius, maxRadius);
        rippleRenderer.SetPropertyBlock(mpb);
    }

    void UpdateRipple()
    {
        if (!isRippleActive) return;

        currentRadius += expandSpeed * Time.deltaTime;
        currentAlpha -= alphaFadeSpeed * Time.deltaTime;

        rippleRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(PropCurrentRadius, currentRadius);
        mpb.SetFloat(PropAlpha, Mathf.Max(0, currentAlpha));
        rippleRenderer.SetPropertyBlock(mpb);

        if (currentRadius >= maxRadius || currentAlpha <= 0.01f)
        {
            ResetRipple(); // ������ �ʱ�ȭ
        }
    }

    void ResetRipple()
    {
        isRippleActive = false;
        currentRadius = 0f;
        currentAlpha = 0f;

        rippleRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(PropCurrentRadius, currentRadius);
        mpb.SetFloat(PropAlpha, currentAlpha);
        mpb.SetFloat(PropMaxRadius, maxRadius);
        rippleRenderer.SetPropertyBlock(mpb);
    }
}
