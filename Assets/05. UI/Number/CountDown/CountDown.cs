using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Mesh 기반 카운트다운 표시기.
/// 숫자(0~9) Mesh들을 슬롯에 배치하여 startValue → endValue로
/// 일정 간격(stepSeconds)마다 감소시키는 방식.
/// meltAmount(머터리얼 파라미터)를 사용하여 시각적 전환효과 지원.
/// IPauseable 인터페이스를 구현하여 일시정지/재개 가능.
/// </summary>
public class Countdown : MonoBehaviour, IPauseable, IResetAbleListener
{
    [Header("Digit Meshes 0~9")]
    public Mesh[] digitMeshes = new Mesh[10];     // 숫자(0~9)에 대응하는 Mesh들

    [Header("Digit Slots (Left->Right)")]
    public MeshFilter[] slots;                    // 자릿수를 표시할 MeshFilter 슬롯 (없으면 자동 수집)

    [Header("Display")]
    public bool hideLeadingZeros = true;          // true면 맨 앞의 0들은 숨김 처리

    [Header("Count Settings (Down)")]
    public int startValue = 0;                    // 카운트 시작 값
    public int endValue = 0;                      // 카운트 종료 값 (startValue > endValue 권장)
    public float stepSeconds = 1f;                // 값이 감소하는 주기(초)
    public bool autoStart = false;                // true면 시작 시 자동으로 카운트 시작
    public bool loop = false;                     // true면 endValue까지 내려간 뒤 다시 startValue로 반복

    [Header("Time / Pause")]
    public bool useUnscaledTime = true;           // true면 Time.unscaledDeltaTime 사용, false면 Time.deltaTime 사용

    [Header("Material")]
    public Material digitMaterial;                // 숫자에 적용할 머터리얼 (셰이더에 _MeltAmount 파라미터 필요)

    // ---------- IPauseable 구현부 ----------
    private bool _paused;                         // 현재 일시정지 여부
    public bool IsPaused => _paused;              // 외부에서 읽을 수 있는 Pause 상태
    public event System.Action<bool> PauseStateChanged; // Pause 상태 변경 시 알림 이벤트
    public int GetRestSecond() { return currentValue; }

    public void Pause() => SetPaused(true);      // 일시정지
    public void Resume() => SetPaused(false);     // 재개
    public void SetPaused(bool value)             // Pause 상태 변경
    {
        if (_paused == value) return;             // 상태가 같으면 무시
        _paused = value;
        PauseStateChanged?.Invoke(_paused);       // 구독자에게 알림
    }

    // ---------- 내부 상태 ----------
    int currentValue = -1;                        // 현재 표시 중인 값
    bool running;                                 // 카운트다운이 동작 중인지
    float stepTimer;                              // 스텝 사이의 시간 누적
    float meltAmount;                             // 0~1 사이 값. 머터리얼 애니메이션용

    readonly List<MeshRenderer> renderers = new();// 각 자릿수의 MeshRenderer 캐시
    readonly List<MaterialPropertyBlock> pbs = new(); // PropertyBlock 캐시 (머터리얼 공유 방지)

    // ---------- Unity 생명주기 ----------
    void Awake()
    {
        EnsureSlots();                            // 슬롯 자동 수집
        CacheRenderers();                         // 렌더러 및 머터리얼 캐싱
        currentValue = startValue;                // 시작 값 세팅
        if (autoStart) StartCountdown();          // autoStart면 즉시 카운트다운 시작
        else SetValueImmediate(startValue);       // 아니면 값만 바로 표시
        ApplyGlobalMelt(0f);                      // Melt 초기화
    }

    void OnEnable()
    {
        EnsureSlots();                            // 슬롯 확인
        CacheRenderers();                         // 렌더러 캐시
        if (currentValue < 0) currentValue = startValue;
        UpdateDigits(currentValue);               // 현재 값 즉시 갱신
        ApplyGlobalMelt(meltAmount);              // Melt 상태 반영
        AddResetCall();
    }

    void OnDisable()
    {
        RemoveResetCall();
    }

    void Update()
    {
        // deltaTime 계산 (pause 또는 editor일 경우 0)
        float dt = _paused ? 0f :
            (Application.isPlaying ? (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime) : 0f);

        if (running && stepSeconds > 0f)
        {
            stepTimer += dt;
            float cycle = Mathf.Clamp01(stepTimer / stepSeconds);
            meltAmount = cycle;                   // cycle 값(0~1)을 머터리얼에 전달

            if (cycle >= 1f)                      // stepSeconds 경과 시
            {
                stepTimer -= stepSeconds;         // 타이머 초기화
                StepDown();                       // 값 감소
                meltAmount = 0f;                  // meltAmount 초기화
            }
        }

        ApplyGlobalMelt(meltAmount);              // 모든 자릿수에 meltAmount 적용
    }

    // ---------- Public API ----------
    /// <summary>카운트다운 시작</summary>
    public void StartCountdown()
    {
        currentValue = startValue;
        UpdateDigits(currentValue);               // 값 표시
        running = true;
        stepTimer = 0f;
        meltAmount = 0f;
        ApplyGlobalMelt(0f);
    }

    /// <summary>카운트다운 중지</summary>
    public void StopCountdown() => running = false;

    /// <summary>카운트다운 상태와 관계없이 즉시 값 변경</summary>
    public void SetValueImmediate(int v)
    {
        currentValue = Mathf.Max(0, v);
        UpdateDigits(currentValue);
        stepTimer = 0f;
        meltAmount = 0f;
        ApplyGlobalMelt(0f);
    }

    /// <summary>슬롯/렌더러 갱신 및 현재 상태를 다시 표시</summary>
    [ContextMenu("Refresh")]
    public void Refresh()
    {
        EnsureSlots();
        CacheRenderers();
        UpdateDigits(Mathf.Max(0, currentValue < 0 ? startValue : currentValue));
        ApplyGlobalMelt(meltAmount);
    }

    // ---------- 내부 로직 ----------
    /// <summary>값을 1 감소시키고 endValue 도달 시 처리</summary>
    void StepDown()
    {
        currentValue--;
        if (currentValue < endValue)
        {
            if (loop) currentValue = startValue;  // 반복 모드면 다시 시작 값으로
            else
            {
                running = false;
                currentValue = endValue;
                GameManager.GetInstance().FadeStart(ScreenWipeDriver.FadeDirection.In);//라운드 종료.
            } // 아니면 멈춤
        }

        UpdateDigits(currentValue);
    }

    /// <summary>값을 각 슬롯에 숫자 Mesh로 반영</summary>
    void UpdateDigits(int value)
    {
        if (slots == null || slots.Length == 0) return;
        if (digitMeshes == null || digitMeshes.Length < 10) return;
        if (value < 0) value = 0;

        int len = slots.Length;
        int tmp = value;
        int[] digits = new int[len];

        // value를 각 자리수로 분리
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

            // 현재 자리수보다 남은 자릿수 계산
            int remaining = len - i - 1;
            int threshold = (int)Mathf.Pow(10, remaining);

            // 맨 앞 0을 숨길지 여부 결정
            bool hide = hideLeadingZeros && leading && value < threshold && remaining > 0;
            if (!hide) leading = false;

            // slot 활성/비활성
            if (slot.gameObject.activeSelf != !hide)
                slot.gameObject.SetActive(!hide);

            if (!hide)
            {
                int d = digits[i];                // 자리수 선택
                var mesh = digitMeshes[d];
                if (slot.sharedMesh != mesh)      // Mesh 갱신
                    slot.sharedMesh = mesh;
            }
        }
    }

    /// <summary>모든 슬롯 렌더러에 _MeltAmount 값을 반영</summary>
    void ApplyGlobalMelt(float v)
    {
        for (int i = 0; i < renderers.Count; i++)
        {
            var r = renderers[i];
            if (!r) continue;
            var pb = pbs[i];
            pb.SetFloat("_MeltAmount", v);
            r.SetPropertyBlock(pb);              // 머터리얼 인스턴스화 방지용
        }
    }

    /// <summary>slots 배열이 비어있으면 자식 MeshFilter 자동 수집</summary>
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

    /// <summary>각 slot의 MeshRenderer와 MaterialPropertyBlock 캐시</summary>
    void CacheRenderers()
    {
        renderers.Clear();
        pbs.Clear();
        if (slots == null) return;

        foreach (var mf in slots)
        {
            if (!mf) { renderers.Add(null); pbs.Add(null); continue; }

            // MeshRenderer가 없으면 추가
            var r = mf.GetComponent<MeshRenderer>();
            if (!r) r = mf.gameObject.AddComponent<MeshRenderer>();

            // digitMaterial이 지정돼 있으면 덮어쓰기
            if (digitMaterial && r.sharedMaterial != digitMaterial)
                r.sharedMaterial = digitMaterial;

            // PropertyBlock 준비
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

