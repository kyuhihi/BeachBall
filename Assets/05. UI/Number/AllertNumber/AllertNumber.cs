using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class AllertNumber : MonoBehaviour, IResetAbleListener
{
    [Header("�ܺ� ����")]
    private PlayerUIManager m_PlayerUIManager;
    private TextMeshProUGUI m_TextMeshPro;

    [Header("ī��Ʈ�ٿ� ����")]
    [SerializeField] private int triggerStartSecond = 5;
    [SerializeField] private float perSecondAnimDuration = 1f;
    [SerializeField] private float baseFontSize = 200f;
    [SerializeField] private float targetFontSize = 300f;
    [SerializeField] private AnimationCurve alphaCurve = AnimationCurve.Linear(0, 1, 1, 0);
    [SerializeField] private AnimationCurve sizeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("GameSet ���� (�ܼ� ���� + �Ϸ� �� ���̵�)")]
    [SerializeField] private string gameSetText = "Finish!!";
    [SerializeField] private Color gameSetColor = new Color(0.2f, 0.85f, 0.95f);
    [SerializeField] private float gameSetDropHeight = 350f;
    [SerializeField] private AnimationCurve dropEase = AnimationCurve.EaseInOut(0, 0, 1, 1); // �ʿ� �� Ŀ�� ����
    private float gameSetDropDuration = 0.5f;          // ������ ���������� (���� 0.6f �� 0.3f)
    private float gameSetFadeDuration = 1.0f;

    private RectTransform _rect;
    private Vector2 _originalAnchoredPos;

    // ���� (�ٿ/Ȧ�� ����)
    private enum State { Idle, Counting, GameSetDrop, GameSetFade }
    private State _state = State.Idle;

    // ī��Ʈ�ٿ� ����
    private int _lastShownSecond = int.MinValue;
    private float _currentNumberStartTime;

    // GameSet ����
    private float _gameSetStartTime;
    private float _gameSetFadeStartTime;
    private Vector2 _gameSetStartPos;
    private Vector2 _gameSetEndPos;

    private int _currentSecond;
    private bool _active = true;
    private bool _gameSetStarted = false; // �߰�: GameSet �� �� ���� �� ��ī��Ʈ ����

  private bool manualGameSetTrigger = false; // LŰ�� ��� GameSet

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

        // ����� Ʈ����
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
                if (!_gameSetStarted) // GameSet ���� �Ŀ��� �ٽ� ī���� �������� ����
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
        if (_gameSetStarted) return;              // ��ȣ
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
        _gameSetStarted = true; // �÷��� ����

        _state = State.GameSetDrop;
        m_TextMeshPro.text = gameSetText;
        m_TextMeshPro.color = gameSetColor;
        m_TextMeshPro.fontSize = targetFontSize;
        _rect.localRotation = Quaternion.identity;

        AudioManager.Instance.PlayFinish(Camera.main.transform.position, 1f);

        _gameSetStartPos = _originalAnchoredPos + Vector2.up * gameSetDropHeight;
        _gameSetEndPos = _originalAnchoredPos;
        _rect.anchoredPosition = _gameSetStartPos;
        _gameSetStartTime = Time.time;
    }

    private void UpdateGameSetDrop()
    {
        Color c = m_TextMeshPro.color; // ���� �߿��� ���� ������ ����
        c.a = 1f;
        m_TextMeshPro.color = c;
        float t = Mathf.Clamp01((Time.time - _gameSetStartTime) / gameSetDropDuration);

        // Ŀ�� ���� (���� �ϰ� �� �ε巯�� ����)
        float eased = dropEase.Evaluate(t);
        _rect.anchoredPosition = Vector2.LerpUnclamped(_gameSetStartPos, _gameSetEndPos, eased);

        if (t >= 1f)
        {
            _rect.anchoredPosition = _gameSetEndPos;
            _state = State.GameSetFade;
            _gameSetFadeStartTime = Time.time; // ���� �� �ٷ� ���� ���� ����
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
            // GameSet ���� �Ŀ��� _gameSetStarted ���� �� 0 ��ǥ�� ����
            m_TextMeshPro.text = "";
            _state = State.Idle;
        }
    }

    public void ResetCountdownVisual()
    {
        // ���� �ʱ�ȭ
        _state = State.Idle;
        _lastShownSecond = int.MinValue;
        _gameSetStartTime = 0f;
        _gameSetFadeStartTime = 0f;

        // �ؽ�Ʈ �� ���־� �ʱ�ȭ
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
        _gameSetStarted = false; // �� ���忡�� �ٽ� ī��Ʈ ���
        ResetCountdownVisual();
    }

    public void OnRoundEnd()
    {
        _active = false;
        ResetCountdownVisual();
        // GameSet ���� ���ο� ���� ����� _gameSetStarted �ʱ�ȭ �� �� (���� �� ����� �� OnRoundStart���� �ʱ�ȭ)
    }
}
