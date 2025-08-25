using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine;
using UnityEngine.InputSystem;

public class UltimateBarDriver : UIInfoBase
{
    [SerializeField] private IPlayerInfo.CourtPosition courtPosition;
    [SerializeField] private Material mat;
    [SerializeField][Range(0,1)] private float gauge = 0f;
    [SerializeField] private float chipLerp = 0.5f;
    [SerializeField] private float prevLerp = 1.5f;

    private float chip;
    private float prev;
    const float FullThreshold = 0.9999f;

    public override IUIInfo.UIType GetUIType() => IUIInfo.UIType.UltimateBar;
    public override IPlayerInfo.CourtPosition GetCourtPosition => courtPosition;
    public override void SetCourtPosition(IPlayerInfo.CourtPosition position) => courtPosition = position;

    public override float GetValueFloat => gauge;
    public override int GetValueInt => Mathf.RoundToInt(gauge * 100f);

    public override bool CanUseAbility => gauge >= FullThreshold;

    public override void UseAbility()
    {
        if (!CanUseAbility) return;
        SetGauge(0f);
    }

    public override void DecreaseValueFloat(float value) => SetGauge(gauge - value);
    public override void DecreaseValueInt(int value) => DecreaseValueFloat(value / 100f);

    public void SetValueFloat(float v) => SetGauge(v);
    public void SetValueInt(int v) => SetGauge(v / 100f);
    public void AddValueFloat(float v) => SetGauge(gauge + Mathf.Max(0f, v));

    void Awake()
    {
        chip = prev = gauge;
        PushToMaterial();
    }
    void Start()
    {
        mat = GetComponent<UnityEngine.UI.Image>().materialForRendering;
    }

    void Update()
    {

        prev = Mathf.MoveTowards(prev, gauge, Time.deltaTime * prevLerp);
        chip = Mathf.MoveTowards(chip, gauge, Time.deltaTime * chipLerp);
        PushToMaterial();
    }

    void SetGauge(float v)
    {
        float nv = Mathf.Clamp01(v);
        if (Mathf.Approximately(nv, gauge)) return;
        gauge = nv;
        Debug.Log($"objName{gameObject.name}[UltimateBarDriver] SetGauge: {gauge}");
        if (chip < gauge) chip = gauge;
        if (prev < gauge) prev = gauge;
        PushToMaterial();
    }

    void PushToMaterial()
    {
        if (!mat) return;
        mat.SetFloat("_Health", gauge);
        mat.SetFloat("_PrevHealth", prev);
        mat.SetFloat("_ChipHealth", chip);
    }


}
