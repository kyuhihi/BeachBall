using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class ScoreCounterUI : UIInfoBase
{
    public TextMeshProUGUI scoreText;

    [Header("Idle Float")]
    public float idleFloatAmplitude = 5f;
    public float idleFloatSpeed = 1f;

    [Header("Bump")]
    public float bumpDuration = 0.35f;
    public float bumpHeight = 30f;
    public float bumpScale = 1.2f;
    public Color highlightColor = Color.yellow;

    [Header("Curves")]
    public AnimationCurve bumpPosCurve = new(
        new Keyframe(0f,0f,0f,4f),
        new Keyframe(0.25f,1f,0f,0f),
        new Keyframe(1f,0f,-4f,0f)
    );
    public AnimationCurve bumpScaleCurve = new(
        new Keyframe(0f,0f,0f,4f),
        new Keyframe(0.2f,1f),
        new Keyframe(1f,0f,-4f,0f)
    );
    public AnimationCurve colorFadeCurve = AnimationCurve.EaseInOut(0f,0f,1f,1f);

    [Header("Time")]
    public bool useUnscaledTime = true;
    public bool paused = false;

    [SerializeField] private IPlayerInfo.CourtPosition courtPosition;

    int currentScore;
    enum State { Idle, Bump }
    State state = State.Idle;

    RectTransform rt;
    Vector2 baseAnchoredPos;
    Color baseColor;
    float idleTime;
    float bumpTime;

    // IUIInfo override
    public override IUIInfo.UIType GetUIType() => IUIInfo.UIType.ScoreCount;
    public override IPlayerInfo.CourtPosition GetCourtPosition => courtPosition;
    public override void SetCourtPosition(IPlayerInfo.CourtPosition position) => courtPosition = position;

    public override float GetValueFloat => currentScore;
    public override int GetValueInt => currentScore;

    public override bool CanUseAbility => false;
    public override void UseAbility() { }

    public override void DecreaseValueFloat(float value) { }

    public override void DecreaseValueInt(int value) =>
        SetScore(currentScore - value);

    public void AddScore(int amount) => SetScore(currentScore + amount);
    public void SetValueInt(int v) => SetScore(v);
    public void SetValueFloat(float v) => SetScore(Mathf.RoundToInt(v));

    void Awake()
    {
        if (!scoreText) scoreText = GetComponent<TextMeshProUGUI>();
        rt = scoreText ? scoreText.rectTransform : GetComponent<RectTransform>();
        if (rt) baseAnchoredPos = rt.anchoredPosition;
        if (scoreText) baseColor = scoreText.color;
        ApplyScoreInstant(currentScore);
    }

    void Update()
    {
        if (Keyboard.current.f5Key.wasPressedThisFrame)
            AddScore(1);

        float dt = paused ? 0f : (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime);

        switch (state)
        {
            case State.Idle: IdleUpdate(dt); break;
            case State.Bump: BumpUpdate(dt); break;
        }
    }

    void IdleUpdate(float dt)
    {
        idleTime += dt * idleFloatSpeed * Mathf.PI * 2f;
        float yOffset = Mathf.Sin(idleTime) * idleFloatAmplitude;
        if (rt) rt.anchoredPosition = baseAnchoredPos + new Vector2(0f, yOffset);
    }

    void BumpUpdate(float dt)
    {
        bumpTime += dt;
        float t = bumpDuration > 0 ? Mathf.Clamp01(bumpTime / bumpDuration) : 1f;

        float posFactor = bumpPosCurve.Evaluate(t);
        float scaleFactor = bumpScaleCurve.Evaluate(t);
        float colorLerp = colorFadeCurve.Evaluate(t);

        if (rt) rt.anchoredPosition = baseAnchoredPos + new Vector2(0f, posFactor * bumpHeight);
        float s = Mathf.Lerp(1f, bumpScale, scaleFactor);
        if (scoreText) scoreText.rectTransform.localScale = Vector3.one * s;
        if (scoreText) scoreText.color = Color.Lerp(highlightColor, baseColor, colorLerp);

        if (t >= 1f)
        {
            state = State.Idle;
            bumpTime = 0f;
            idleTime = 0f;
            if (rt) rt.anchoredPosition = baseAnchoredPos;
            if (scoreText)
            {
                scoreText.rectTransform.localScale = Vector3.one;
                scoreText.color = baseColor;
            }
        }
    }

    void SetScore(int newScore)
    {
        if (newScore == currentScore) return;
        currentScore = Mathf.Max(0, newScore);
        ApplyScoreInstant(currentScore);
        StartBump();
    }

    void ApplyScoreInstant(int v)
    {
        if (scoreText) scoreText.text = v.ToString();
    }

    void StartBump()
    {
        state = State.Bump;
        bumpTime = 0f;
        if (rt) rt.anchoredPosition = baseAnchoredPos;
        if (scoreText)
        {
            scoreText.rectTransform.localScale = Vector3.one;
            scoreText.color = highlightColor;
        }
    }

    public void ForceIdleReset()
    {
        state = State.Idle;
        bumpTime = 0f;
        idleTime = 0f;
        if (rt)
        {
            rt.anchoredPosition = baseAnchoredPos;
            rt.localScale = Vector3.one;
        }
        if (scoreText) scoreText.color = baseColor;
    }
}
