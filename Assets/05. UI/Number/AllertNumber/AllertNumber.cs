using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class AllertNumber : MonoBehaviour, IResetAbleListener
{
    [Header("외부 참조")]
    private PlayerUIManager m_PlayerUIManager;
    private TextMeshProUGUI m_TextMeshPro;

    [Header("카운트다운 설정")]
    [SerializeField] private int triggerStartSecond = 5;
    [SerializeField] private float perSecondAnimDuration = 1f;
    [SerializeField] private float baseFontSize = 200f;
    [SerializeField] private float targetFontSize = 300f;
    [SerializeField] private AnimationCurve alphaCurve = AnimationCurve.Linear(0, 1, 1, 0);
    [SerializeField] private AnimationCurve sizeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("GameSet 연출 (단순 낙하 + 완료 후 페이드)")]
    [SerializeField] private string gameSetText = "Finish!!";
    [SerializeField] private Color gameSetColor = new Color(0.2f, 0.85f, 0.95f);
    [SerializeField] private float gameSetDropHeight = 350f;
    [SerializeField] private AnimationCurve dropEase = AnimationCurve.EaseInOut(0, 0, 1, 1); // 필요 시 커브 조정
    private float gameSetDropDuration = 0.5f;          // 빠르게 내려오도록 (기존 0.6f → 0.3f)
    private float gameSetFadeDuration = 1.0f;

    private RectTransform _rect;
    private Vector2 _originalAnchoredPos;

    // 상태 (바운스/홀드 제거)
    private enum State { Idle, Counting, GameSetDrop, GameSetFade }
    private State _state = State.Idle;

    // 카운트다운 관련
    private int _lastShownSecond = int.MinValue;
    private float _currentNumberStartTime;

    // GameSet 관련
    private float _gameSetStartTime;
    private float _gameSetFadeStartTime;
    private Vector2 _gameSetStartPos;
    private Vector2 _gameSetEndPos;

    private int _currentSecond;
    private bool _active = true;
    private bool _gameSetStarted = false; // 추가: GameSet 한 번 실행 후 재카운트 방지

    [Header("디버그 / 수동 트리거")]
    [SerializeField] private bool manualGameSetTrigger = true; // L키로 즉시 GameSet

    private void Awake()
    {
        m_TextMeshPro = GetComponent<TextMeshProUGUI>();
        _rect = GetComponent<RectTransform>();
        if (_rect != null)
            _originalAnchoredPos = _rect.anchoredPosition;
        m_TextMeshPro.text = "";
        m_TextMeshPro.enableVertexGradient = false;
    }

    private void OnEnable()
    {
        AddResetCall();
    }

    private void OnDestroy()
    {
        RemoveResetCall();
    }

    private void Update()
    {
        if (!_active) return;

        // 디버그 트리거
        if (manualGameSetTrigger && Keyboard.current.lKey.wasPressedThisFrame)
        {
            if (!_gameSetStarted && _state != State.GameSetDrop && _state != State.GameSetFade)
                StartGameSet();
        }

        if (m_PlayerUIManager == null)
        {
            m_PlayerUIManager = PlayerUIManager.GetInstance();
            if (m_PlayerUIManager == null) return;
        }

        _currentSecond = m_PlayerUIManager.GetCurrentSecond();

        switch (_state)
        {
            case State.Idle:
                if (!_gameSetStarted) // GameSet 시작 후에는 다시 카운팅 진입하지 않음
                    TryEnterCounting();
                break;
            case State.Counting:
                UpdateCounting();
                break;
            case State.GameSetDrop:
                UpdateGameSetDrop();
                break;
            case State.GameSetFade:
                UpdateGameSetFade();
                break;
        }
    }

    private void TryEnterCounting()
    {
        if (_gameSetStarted) return;              // 보호
        if (_currentSecond <= triggerStartSecond && _currentSecond >= 0)
        {
            _state = State.Counting;
            _lastShownSecond = int.MinValue;
        }
    }

    private void UpdateCounting()
    {
        if (_currentSecond != _lastShownSecond && _currentSecond >= 0)
        {
            _lastShownSecond = _currentSecond;
            ShowNewSecond(_currentSecond);
        }

        if (_currentSecond == 0)
        {
            if (Time.time - _currentNumberStartTime >= perSecondAnimDuration * 0.95f)
                StartGameSet();
        }

        float t = Mathf.Clamp01((Time.time - _currentNumberStartTime) / perSecondAnimDuration);
        AnimateCurrentNumber(t);
    }

    private void ShowNewSecond(int sec)
    {
        m_TextMeshPro.text = sec.ToString();
        m_TextMeshPro.fontSize = baseFontSize;

        float zRot = (sec % 2 == 1) ? 30f : -30f;
        _rect.localRotation = Quaternion.Euler(0, 0, zRot);

        Color c = m_TextMeshPro.color; c.a = 1f;
        m_TextMeshPro.color = c;

        _currentNumberStartTime = Time.time;
    }

    private void AnimateCurrentNumber(float t)
    {
        Color c = m_TextMeshPro.color;
        c.a = alphaCurve.Evaluate(t);
        m_TextMeshPro.color = c;

        float sT = sizeCurve.Evaluate(t);
        m_TextMeshPro.fontSize = Mathf.Lerp(baseFontSize, targetFontSize, sT);
    }

    private void StartGameSet()
    {
        _gameSetStarted = true; // 플래그 설정

        _state = State.GameSetDrop;
        m_TextMeshPro.text = gameSetText;
        m_TextMeshPro.color = gameSetColor;
        m_TextMeshPro.fontSize = targetFontSize;
        _rect.localRotation = Quaternion.identity;

        _gameSetStartPos = _originalAnchoredPos + Vector2.up * gameSetDropHeight;
        _gameSetEndPos = _originalAnchoredPos;
        _rect.anchoredPosition = _gameSetStartPos;
        _gameSetStartTime = Time.time;
    }

    private void UpdateGameSetDrop()
    {
        Color c = m_TextMeshPro.color; // 낙하 중에는 완전 불투명 유지
        c.a = 1f;
        m_TextMeshPro.color = c;
        float t = Mathf.Clamp01((Time.time - _gameSetStartTime) / gameSetDropDuration);

        // 커브 적용 (빠른 하강 → 부드러운 정지)
        float eased = dropEase.Evaluate(t);
        _rect.anchoredPosition = Vector2.LerpUnclamped(_gameSetStartPos, _gameSetEndPos, eased);

        if (t >= 1f)
        {
            _rect.anchoredPosition = _gameSetEndPos;
            _state = State.GameSetFade;
            _gameSetFadeStartTime = Time.time; // 도착 후 바로 알파 감쇠 시작
        }
    }

    private void UpdateGameSetFade()
    {
        float t = Mathf.Clamp01((Time.time - _gameSetFadeStartTime) / gameSetFadeDuration);
        Color c = m_TextMeshPro.color;
        c.a = 1f - t;
        m_TextMeshPro.color = c;

        if (t >= 1f)
        {
            // GameSet 종료 후에도 _gameSetStarted 유지 → 0 재표시 방지
            m_TextMeshPro.text = "";
            _state = State.Idle;
        }
    }

    public void ResetCountdownVisual()
    {
        // 상태 초기화
        _state = State.Idle;
        _lastShownSecond = int.MinValue;
        _gameSetStartTime = 0f;
        _gameSetFadeStartTime = 0f;

        // 텍스트 및 비주얼 초기화
        m_TextMeshPro.text = "";
        m_TextMeshPro.color = Color.black;
        m_TextMeshPro.fontSize = baseFontSize;

        if (_rect != null)
        {
            _rect.anchoredPosition = _originalAnchoredPos;
            _rect.localRotation = Quaternion.identity;
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
        _active = true;
        _gameSetStarted = false; // 새 라운드에서 다시 카운트 허용
        ResetCountdownVisual();
    }

    public void OnRoundEnd()
    {
        _active = false;
        ResetCountdownVisual();
        // GameSet 유지 여부에 따라 여기는 _gameSetStarted 초기화 안 함 (종료 후 재시작 시 OnRoundStart에서 초기화)
    }
}
