using UnityEngine;

public class DashBarDriver : UIInfoBase, IPauseable
{
    [SerializeField] private IPlayerInfo.CourtPosition courtPosition;
    [SerializeField] private float fillSpeed = 0.5f;
    [SerializeField] private string materialFloatProp = "_Value";

    private const int SegmentCount = 3;
    private const float SegmentWidth = 1f / SegmentCount;
    private const float Epsilon = 1e-4f;

    private bool isPaused;
    private Material m_DashUIMat;
    private float cachedValue;

    // IPauseable
    public bool IsPaused => isPaused;
    public event System.Action<bool> PauseStateChanged;
    public void Pause()  => SetPaused(true);
    public void Resume() => SetPaused(false);
    public void SetPaused(bool p)
    {
        if (isPaused == p) return;
        isPaused = p;
        PauseStateChanged?.Invoke(isPaused);
    }

    // IUIInfo (override)
    public override IUIInfo.UIType GetUIType() => IUIInfo.UIType.DashBar;
    public override IPlayerInfo.CourtPosition GetCourtPosition => courtPosition;
    public override void SetCourtPosition(IPlayerInfo.CourtPosition position) => courtPosition = position;

    public override float GetValueFloat => cachedValue;
    public override int GetValueInt => Mathf.RoundToInt(cachedValue * 100f);

    public override bool CanUseAbility => cachedValue > SegmentWidth; // 한 칸 이상 충전

    public override void UseAbility()
    {
        // 현재 채워진 세그먼트의 채워진 부분만 소모
        if (!CanUseAbility) return;

        SetValueFloat(cachedValue - SegmentWidth);
    }

    public override void DecreaseValueFloat(float value) =>
        SetValueFloat(cachedValue - Mathf.Abs(value));

    public override void DecreaseValueInt(int value) =>
        SetValueFloat(cachedValue - (Mathf.Abs(value) / 100f));

    // 추가 SetValue API (필요 시)
    public void SetValueFloat(float v)
    {
        float nv = Mathf.Clamp01(v);
        if (Mathf.Approximately(nv, cachedValue)) return;
        cachedValue = nv;
        if (m_DashUIMat) m_DashUIMat.SetFloat(materialFloatProp, cachedValue);
    }
    public void SetValueInt(int v) => SetValueFloat(v / 100f);

    public int CurrentSegmentIndex
    {
        get
        {
            if (cachedValue <= 0f) return 0;
            int idx = Mathf.Min(SegmentCount - 1, (int)(cachedValue / SegmentWidth));
            float mod = cachedValue - idx * SegmentWidth;
            if (mod <= Epsilon && cachedValue > 0f)
                idx = Mathf.Max(0, idx - 1);
            return idx;
        }
    }
    public float CurrentSegmentStart => CurrentSegmentIndex * SegmentWidth;
    public float CurrentSegmentFill => Mathf.Clamp(cachedValue - CurrentSegmentStart, 0f, SegmentWidth);

    void Awake()
    {
        CacheMaterial();
        SetValueFloat(0f);
    }

    void Update()
    {
        if (isPaused) return;
        if (!m_DashUIMat) { CacheMaterial(); if (!m_DashUIMat) return; }
        if (cachedValue < 1f && fillSpeed > 0f)
            SetValueFloat(cachedValue + fillSpeed * Time.deltaTime);
    }

    void CacheMaterial()
    {
        var r = GetComponent<Renderer>();
        if (r) m_DashUIMat = r.material;
    }
}
