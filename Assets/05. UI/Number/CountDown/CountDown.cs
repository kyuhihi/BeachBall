using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Mesh ��� ī��Ʈ�ٿ� ǥ�ñ�.
/// ����(0~9) Mesh���� ���Կ� ��ġ�Ͽ� startValue �� endValue��
/// ���� ����(stepSeconds)���� ���ҽ�Ű�� ���.
/// meltAmount(���͸��� �Ķ����)�� ����Ͽ� �ð��� ��ȯȿ�� ����.
/// IPauseable �������̽��� �����Ͽ� �Ͻ�����/�簳 ����.
/// </summary>
public class Countdown : MonoBehaviour, IPauseable, IResetAbleListener
{
    [Header("Digit Meshes 0~9")]
    public Mesh[] digitMeshes = new Mesh[10];     // ����(0~9)�� �����ϴ� Mesh��

    [Header("Digit Slots (Left->Right)")]
    public MeshFilter[] slots;                    // �ڸ����� ǥ���� MeshFilter ���� (������ �ڵ� ����)

    [Header("Display")]
    public bool hideLeadingZeros = true;          // true�� �� ���� 0���� ���� ó��

    [Header("Count Settings (Down)")]
    public int startValue = 0;                    // ī��Ʈ ���� ��
    public int endValue = 0;                      // ī��Ʈ ���� �� (startValue > endValue ����)
    public float stepSeconds = 1f;                // ���� �����ϴ� �ֱ�(��)
    public bool autoStart = false;                // true�� ���� �� �ڵ����� ī��Ʈ ����
    public bool loop = false;                     // true�� endValue���� ������ �� �ٽ� startValue�� �ݺ�

    [Header("Time / Pause")]
    public bool useUnscaledTime = true;           // true�� Time.unscaledDeltaTime ���, false�� Time.deltaTime ���

    [Header("Material")]
    public Material digitMaterial;                // ���ڿ� ������ ���͸��� (���̴��� _MeltAmount �Ķ���� �ʿ�)

    // ---------- IPauseable ������ ----------
    private bool _paused;                         // ���� �Ͻ����� ����
    public bool IsPaused => _paused;              // �ܺο��� ���� �� �ִ� Pause ����
    public event System.Action<bool> PauseStateChanged; // Pause ���� ���� �� �˸� �̺�Ʈ
    public int GetRestSecond() { return currentValue; }

    public void Pause() => SetPaused(true);      // �Ͻ�����
    public void Resume() => SetPaused(false);     // �簳
    public void SetPaused(bool value)             // Pause ���� ����
    {
        if (_paused == value) return;             // ���°� ������ ����
        _paused = value;
        PauseStateChanged?.Invoke(_paused);       // �����ڿ��� �˸�
    }

    // ---------- ���� ���� ----------
    int currentValue = -1;                        // ���� ǥ�� ���� ��
    bool running;                                 // ī��Ʈ�ٿ��� ���� ������
    float stepTimer;                              // ���� ������ �ð� ����
    float meltAmount;                             // 0~1 ���� ��. ���͸��� �ִϸ��̼ǿ�

    readonly List<MeshRenderer> renderers = new();// �� �ڸ����� MeshRenderer ĳ��
    readonly List<MaterialPropertyBlock> pbs = new(); // PropertyBlock ĳ�� (���͸��� ���� ����)

    // ---------- Unity �����ֱ� ----------
    void Awake()
    {
        EnsureSlots();                            // ���� �ڵ� ����
        CacheRenderers();                         // ������ �� ���͸��� ĳ��
        currentValue = startValue;                // ���� �� ����
        if (autoStart) StartCountdown();          // autoStart�� ��� ī��Ʈ�ٿ� ����
        else SetValueImmediate(startValue);       // �ƴϸ� ���� �ٷ� ǥ��
        ApplyGlobalMelt(0f);                      // Melt �ʱ�ȭ
    }

    void OnEnable()
    {
        EnsureSlots();                            // ���� Ȯ��
        CacheRenderers();                         // ������ ĳ��
        if (currentValue < 0) currentValue = startValue;
        UpdateDigits(currentValue);               // ���� �� ��� ����
        ApplyGlobalMelt(meltAmount);              // Melt ���� �ݿ�
        AddResetCall();
    }

    void OnDisable()
    {
        RemoveResetCall();
    }

    void Update()
    {
        // deltaTime ��� (pause �Ǵ� editor�� ��� 0)
        float dt = _paused ? 0f :
            (Application.isPlaying ? (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime) : 0f);

        if (running && stepSeconds > 0f)
        {
            stepTimer += dt;
            float cycle = Mathf.Clamp01(stepTimer / stepSeconds);
            meltAmount = cycle;                   // cycle ��(0~1)�� ���͸��� ����

            if (cycle >= 1f)                      // stepSeconds ��� ��
            {
                stepTimer -= stepSeconds;         // Ÿ�̸� �ʱ�ȭ
                StepDown();                       // �� ����
                meltAmount = 0f;                  // meltAmount �ʱ�ȭ
            }
        }

        ApplyGlobalMelt(meltAmount);              // ��� �ڸ����� meltAmount ����
    }

    // ---------- Public API ----------
    /// <summary>ī��Ʈ�ٿ� ����</summary>
    public void StartCountdown()
    {
        currentValue = startValue;
        UpdateDigits(currentValue);               // �� ǥ��
        running = true;
        stepTimer = 0f;
        meltAmount = 0f;
        ApplyGlobalMelt(0f);
    }

    /// <summary>ī��Ʈ�ٿ� ����</summary>
    public void StopCountdown() => running = false;

    /// <summary>ī��Ʈ�ٿ� ���¿� ������� ��� �� ����</summary>
    public void SetValueImmediate(int v)
    {
        currentValue = Mathf.Max(0, v);
        UpdateDigits(currentValue);
        stepTimer = 0f;
        meltAmount = 0f;
        ApplyGlobalMelt(0f);
    }

    /// <summary>����/������ ���� �� ���� ���¸� �ٽ� ǥ��</summary>
    [ContextMenu("Refresh")]
    public void Refresh()
    {
        EnsureSlots();
        CacheRenderers();
        UpdateDigits(Mathf.Max(0, currentValue < 0 ? startValue : currentValue));
        ApplyGlobalMelt(meltAmount);
    }

    // ---------- ���� ���� ----------
    /// <summary>���� 1 ���ҽ�Ű�� endValue ���� �� ó��</summary>
    void StepDown()
    {
        currentValue--;
        if (currentValue < endValue)
        {
            if (loop) currentValue = startValue;  // �ݺ� ���� �ٽ� ���� ������
            else
            {
                running = false;
                currentValue = endValue;
                GameManager.GetInstance().FadeStart(ScreenWipeDriver.FadeDirection.In);//���� ����.
            } // �ƴϸ� ����
        }

        UpdateDigits(currentValue);
    }

    /// <summary>���� �� ���Կ� ���� Mesh�� �ݿ�</summary>
    void UpdateDigits(int value)
    {
        if (slots == null || slots.Length == 0) return;
        if (digitMeshes == null || digitMeshes.Length < 10) return;
        if (value < 0) value = 0;

        int len = slots.Length;
        int tmp = value;
        int[] digits = new int[len];

        // value�� �� �ڸ����� �и�
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

            // ���� �ڸ������� ���� �ڸ��� ���
            int remaining = len - i - 1;
            int threshold = (int)Mathf.Pow(10, remaining);

            // �� �� 0�� ������ ���� ����
            bool hide = hideLeadingZeros && leading && value < threshold && remaining > 0;
            if (!hide) leading = false;

            // slot Ȱ��/��Ȱ��
            if (slot.gameObject.activeSelf != !hide)
                slot.gameObject.SetActive(!hide);

            if (!hide)
            {
                int d = digits[i];                // �ڸ��� ����
                var mesh = digitMeshes[d];
                if (slot.sharedMesh != mesh)      // Mesh ����
                    slot.sharedMesh = mesh;
            }
        }
    }

    /// <summary>��� ���� �������� _MeltAmount ���� �ݿ�</summary>
    void ApplyGlobalMelt(float v)
    {
        for (int i = 0; i < renderers.Count; i++)
        {
            var r = renderers[i];
            if (!r) continue;
            var pb = pbs[i];
            pb.SetFloat("_MeltAmount", v);
            r.SetPropertyBlock(pb);              // ���͸��� �ν��Ͻ�ȭ ������
        }
    }

    /// <summary>slots �迭�� ��������� �ڽ� MeshFilter �ڵ� ����</summary>
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

    /// <summary>�� slot�� MeshRenderer�� MaterialPropertyBlock ĳ��</summary>
    void CacheRenderers()
    {
        renderers.Clear();
        pbs.Clear();
        if (slots == null) return;

        foreach (var mf in slots)
        {
            if (!mf) { renderers.Add(null); pbs.Add(null); continue; }

            // MeshRenderer�� ������ �߰�
            var r = mf.GetComponent<MeshRenderer>();
            if (!r) r = mf.gameObject.AddComponent<MeshRenderer>();

            // digitMaterial�� ������ ������ �����
            if (digitMaterial && r.sharedMaterial != digitMaterial)
                r.sharedMaterial = digitMaterial;

            // PropertyBlock �غ�
            var pb = new MaterialPropertyBlock();
            r.GetPropertyBlock(pb);

            renderers.Add(r);
            pbs.Add(pb);
        }
    }

    public void AddResetCall()
    {
        Signals.RoundResetAble.AddStart(OnRoundStart);
        Signals.RoundResetAble.AddEnd(OnRoundEnd);
    }

    public void RemoveResetCall()
    {
        Signals.RoundResetAble.RemoveStart(OnRoundStart);
        Signals.RoundResetAble.RemoveEnd(OnRoundEnd);
    }

    public void OnRoundStart()
    {
        running = true;
        currentValue = startValue;
    }

    public void OnRoundEnd()
    {
        running = false;
    }
}

