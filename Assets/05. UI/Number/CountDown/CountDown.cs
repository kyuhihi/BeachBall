using UnityEngine;
using System.Collections.Generic;

public class Countdown : MonoBehaviour, IPauseable
{
    [Header("Digit Meshes 0~9")]
    public Mesh[] digitMeshes = new Mesh[10];

    [Header("Digit Slots (Left->Right)")]
    public MeshFilter[] slots;          // 비우면 자동 수집

    [Header("Display")]
    public bool hideLeadingZeros = true;

    [Header("Count Settings (Down)")]
    public int startValue = 0;
    public int endValue = 0;            // startValue > endValue 권장
    public float stepSeconds = 1f;
    public bool autoStart = false;
    public bool loop = false;

    [Header("Time / Pause")]
    public bool useUnscaledTime = true;

    [Header("Material")]
    public Material digitMaterial;      // 셰이더에 _MeltAmount 지원

    // IPauseable -------------------------------------------------
    private bool _paused;
    public bool IsPaused => _paused;
    public event System.Action<bool> PauseStateChanged;
    public void Pause()  => SetPaused(true);
    public void Resume() => SetPaused(false);
    public void SetPaused(bool value)
    {
        if (_paused == value) return;
        _paused = value;
        PauseStateChanged?.Invoke(_paused);
    }
    // ------------------------------------------------------------

    int currentValue = -1;
    bool running;
    float stepTimer;
    float meltAmount;

    readonly List<MeshRenderer> renderers = new();
    readonly List<MaterialPropertyBlock> pbs = new();

    // ---------- Unity ----------
    void Awake()
    {
        EnsureSlots();
        CacheRenderers();
        currentValue = startValue;
        if (autoStart) StartCountdown();
        else SetValueImmediate(startValue);
        ApplyGlobalMelt(0f);
    }

    void OnEnable()
    {
        EnsureSlots();
        CacheRenderers();
        if (currentValue < 0) currentValue = startValue;
        UpdateDigits(currentValue);
        ApplyGlobalMelt(meltAmount);
    }

    void Update()
    {
        float dt = _paused ? 0f :
            (Application.isPlaying ? (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime) : 0f);

        if (running && stepSeconds > 0f)
        {
            stepTimer += dt;
            float cycle = Mathf.Clamp01(stepTimer / stepSeconds);
            meltAmount = cycle;

            if (cycle >= 1f)
            {
                stepTimer -= stepSeconds;
                StepDown();
                meltAmount = 0f;
            }
        }

        ApplyGlobalMelt(meltAmount);
    }

    // ---------- Public API ----------
    public void StartCountdown()
    {
        currentValue = startValue;
        UpdateDigits(currentValue);
        running = true;
        stepTimer = 0f;
        meltAmount = 0f;
        ApplyGlobalMelt(0f);
    }

    public void StopCountdown() => running = false;

    public void SetValueImmediate(int v)
    {
        currentValue = Mathf.Max(0, v);
        UpdateDigits(currentValue);
        stepTimer = 0f;
        meltAmount = 0f;
        ApplyGlobalMelt(0f);
    }

    [ContextMenu("Refresh")]
    public void Refresh()
    {
        EnsureSlots();
        CacheRenderers();
        UpdateDigits(Mathf.Max(0, currentValue < 0 ? startValue : currentValue));
        ApplyGlobalMelt(meltAmount);
    }

    // ---------- Internal Logic ----------
    void StepDown()
    {
        currentValue--;
        if (currentValue < endValue)
        {
            if (loop) currentValue = startValue;
            else { running = false; currentValue = endValue; }
        }
        UpdateDigits(currentValue);
    }

    void UpdateDigits(int value)
    {
        if (slots == null || slots.Length == 0) return;
        if (digitMeshes == null || digitMeshes.Length < 10) return;
        if (value < 0) value = 0;

        int len = slots.Length;
        int tmp = value;
        int[] digits = new int[len];
        for (int i = len - 1; i >= 0; i--)
        {
            digits[i] = tmp % 10;
            tmp /= 10;
        }

        bool leading = true;
        for (int i = 0; i < len; i++)
        {
            var slot = slots[i];
            if (!slot) continue;

            int remaining = len - i - 1;
            int threshold = (int)Mathf.Pow(10, remaining);
            bool hide = hideLeadingZeros && leading && value < threshold && remaining > 0;

            if (!hide) leading = false;

            if (slot.gameObject.activeSelf != !hide)
                slot.gameObject.SetActive(!hide);

            if (!hide)
            {
                int d = digits[i];
                var mesh = digitMeshes[d];
                if (slot.sharedMesh != mesh)
                    slot.sharedMesh = mesh;
            }
        }
    }

    void ApplyGlobalMelt(float v)
    {
        for (int i = 0; i < renderers.Count; i++)
        {
            var r = renderers[i];
            if (!r) continue;
            var pb = pbs[i];
            pb.SetFloat("_MeltAmount", v);
            r.SetPropertyBlock(pb);
        }
    }

    void EnsureSlots()
    {
        if (slots != null && slots.Length > 0) return;
        var all = GetComponentsInChildren<MeshFilter>(true);
        var me = GetComponent<MeshFilter>();
        var list = new List<MeshFilter>();
        foreach (var mf in all)
            if (mf != me) list.Add(mf);
        slots = list.ToArray();
    }

    void CacheRenderers()
    {
        renderers.Clear();
        pbs.Clear();
        if (slots == null) return;
        foreach (var mf in slots)
        {
            if (!mf) { renderers.Add(null); pbs.Add(null); continue; }
            var r = mf.GetComponent<MeshRenderer>();
            if (!r) r = mf.gameObject.AddComponent<MeshRenderer>();
            if (digitMaterial && r.sharedMaterial != digitMaterial)
                r.sharedMaterial = digitMaterial;
            var pb = new MaterialPropertyBlock();
            r.GetPropertyBlock(pb);
            renderers.Add(r);
            pbs.Add(pb);
        }
    }
}
