using UnityEngine;
using System.Collections.Generic;

public class Countdown : MonoBehaviour
{
    [Header("Digit Meshes 0~9")]
    public Mesh[] digitMeshes = new Mesh[10];

    [Header("Digit Slots (Left->Right)")]
    public MeshFilter[] slots;      // 2자리면 2개만 배치

    [Header("Display")]
    public bool hideLeadingZeros = true;

    [Header("Count Settings (Down)")]
    public int startValue = 0;
    public int endValue = 0;        // startValue > endValue 권장
    public float stepSeconds = 1f;
    public bool autoStart = false;
    public bool loop = false;

    [Header("Time / Pause")]
    public bool paused = false;
    public bool useUnscaledTime = true;

    [Header("Material")]
    public Material digitMaterial;  // MeltWaveClip_World 셰이더 머티리얼

    int currentValue = -1;
    bool running;
    float stepTimer;        // 0 ~ stepSeconds
    float meltAmount;       // 0~1 (stepTimer / stepSeconds 재현)

    // 캐시용
    readonly List<MeshRenderer> renderers = new();
    readonly List<MaterialPropertyBlock> pbs = new();

    void Awake()
    {
        EnsureSlots();
        CacheRenderers();
        if (autoStart)
            StartCountdown();
        else
            SetValueImmediate(startValue);
        ApplyGlobalMelt(0f);
    }

    void OnEnable()
    {
        EnsureSlots();
        CacheRenderers();
        if (currentValue < 0) SetValueImmediate(startValue);
        ApplyGlobalMelt(meltAmount);
    }

    void OnValidate()
    {
        if (stepSeconds < 0.01f) stepSeconds = 0.01f;
        EnsureSlots();
        CacheRenderers();
        if (currentValue < 0) currentValue = startValue;
        UpdateDigits(currentValue);
        ApplyGlobalMelt(meltAmount);
    }

    void Update()
    {
        float dt = paused ? 0f :
            (Application.isPlaying ? (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime) : 0.016f);

        if (running)
        {
            stepTimer += dt;
            float cycle = Mathf.Clamp01(stepTimer / stepSeconds);
            meltAmount = cycle;

            // 숫자 바뀔 시점 (완전히 녹은 후 교체)
            if (cycle >= 1f)
            {
                stepTimer -= stepSeconds;
                StepDown();          // 값 감소 & 새 메쉬 설정
                meltAmount = 0f;     // 다시 0에서 시작
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
    public void Pause() => paused = true;
    public void Resume() => paused = false;

    public void SetValueImmediate(int v)
    {
        currentValue = Mathf.Max(0, v);
        UpdateDigits(currentValue);
        stepTimer = 0f;
        meltAmount = 0f;
        ApplyGlobalMelt(0f);
    }

    // ---------- Logic ----------
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

    // ---------- Digits ----------
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
            slot.gameObject.SetActive(!hide);

            if (!hide)
            {
                int d = digits[i];
                slot.sharedMesh = digitMeshes[d];
            }
        }
    }

    // ---------- Melt (Global) ----------
    void ApplyGlobalMelt(float v)
    {
        int count = renderers.Count;
        for (int i = 0; i < count; i++)
        {
            var r = renderers[i];
            if (!r) continue;
            var pb = pbs[i];
            pb.SetFloat("_MeltAmount", v);
            r.SetPropertyBlock(pb);
        }
    }

    // ---------- Setup ----------
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
            if (!mf) { pbs.Add(null); renderers.Add(null); continue; }
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
