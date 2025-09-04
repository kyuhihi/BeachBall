// filepath: c:\Users\Lenovo\BeachBall\Assets\05. UI\SystemMessage\SystemText.cs
using UnityEngine;
using TMPro;
using System;
using UnityEngine.InputSystem;

[RequireComponent(typeof(TextMeshProUGUI))]
public class SystemText : MonoBehaviour
{
    [Header("Pop Float Animation")]
    [SerializeField] private bool enablePopFloat = true;
    private bool useUnscaledTime = true;
    private float startY = -100f;
    private float peakY = 350f;
    [SerializeField] private float riseDuration = 0.4f;
    [SerializeField] private float holdDuration = 1.2f;
    [SerializeField] private float fallDuration = 0.45f;
    [SerializeField] private AnimationCurve riseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve fallCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Background Link (Optional)")]
    [SerializeField] private TMPDynamicBackground dynamicBackground; // ���� ������Ʈ�� �پ��ִٸ� �ڵ� Ž��

    public event Action<string> OnTextChanged;
    public event Action OnAnimationRestart;

    private TextMeshProUGUI _tmp;
    private Vector2 _basePos;          // ���� ����(������ ����)
    private Vector2 _initialBasePos;   // ���� �����(�����/�缳��)
    private string _currentText = "";
    private bool _initialized;

    private enum FloatState { Idle, Rising, Hold, Falling }
    private FloatState _state = FloatState.Idle;
    private float _timer;

    void Awake()
    {
        _tmp = GetComponent<TextMeshProUGUI>();
        if (dynamicBackground == null)
            dynamicBackground = GetComponent<TMPDynamicBackground>();

        var rt = (RectTransform)transform;
        _basePos = rt.anchoredPosition;
        _initialBasePos = _basePos;
        _initialized = true;
    }

    void OnEnable()
    {
        // �ʱ� �ؽ�Ʈ ���� ���� ���
        if (_tmp != null)
        {
            _currentText = _tmp.text;
            NotifyTextChanged();
        }
    }

    public void SetText(string newText, bool restartAnimation = true)
    {
        if (!_initialized) Awake();
        if (newText == null) newText = "";
        _currentText = newText;
        _tmp.text = _currentText;

        if (restartAnimation)
            RestartAnimation();

        NotifyTextChanged();
    }

    public void ClearText(bool restart = false)
    {
        SetText("", restart);
    }

    private void NotifyTextChanged()
    {
        OnTextChanged?.Invoke(_currentText);
        if (dynamicBackground != null)
            dynamicBackground.RequestImmediateRefresh(); // ��� ��� ��������
    }

    // �ִ� �ٽ� ���� (������ �帮��Ʈ ����: _basePos �� �ٲ�)
    private void RestartAnimation()
    {
        if (!enablePopFloat) return;

        _state = FloatState.Rising;
        _timer = 0f;

        var rt = (RectTransform)transform;
        // ���� pos �� ���� �� ����
        Vector2 p = _basePos;
        p.y = _basePos.y + startY;     // �Ʒ� ����
        rt.anchoredPosition = p;

        OnAnimationRestart?.Invoke();
        dynamicBackground?.RequestImmediateRefresh();
    }

    // �ʿ� �� �ܺο��� ���� ����
    public void ResetBasePositionToCurrent()
    {
        var rt = (RectTransform)transform;
        _basePos = rt.anchoredPosition;
    }

    public void RestoreInitialBasePosition()
    {
        _basePos = _initialBasePos;
    }

    void Update()
    {
        if (Keyboard.current.backspaceKey.wasPressedThisFrame)
        {
            SetText("Backspace Pressed!", true);
        }
        // �� ��Ȱ�� �� ����
        if (!enablePopFloat) return;

        // Idle ���¸� �� �̻� �̵� ���� (�� �Է����� ����� ����)
        if (_state == FloatState.Idle) return;

        float dt = useUnscaledTime
            ? (Application.isPlaying ? Time.unscaledDeltaTime : 0f)
            : (Application.isPlaying ? Time.deltaTime : 0f);
        if (dt <= 0f) return;

        _timer += dt;

        var rt = (RectTransform)transform;
        Vector2 pos = rt.anchoredPosition;
        float t;

        switch (_state)
        {
            case FloatState.Rising:
                if (riseDuration <= 0f)
                {
                    pos.y = _basePos.y + peakY;
                    _state = holdDuration > 0f ? FloatState.Hold : FloatState.Falling;
                    _timer = 0f;
                    break;
                }
                t = Mathf.Clamp01(_timer / riseDuration);
                pos.y = _basePos.y + Mathf.Lerp(startY, peakY, riseCurve.Evaluate(t));
                if (t >= 1f)
                {
                    _state = holdDuration > 0f ? FloatState.Hold : FloatState.Falling;
                    _timer = 0f;
                }
                break;

            case FloatState.Hold:
                pos.y = _basePos.y + peakY;
                if (_timer >= holdDuration)
                {
                    _state = FloatState.Falling;
                    _timer = 0f;
                }
                break;

            case FloatState.Falling:
                if (fallDuration <= 0f)
                {
                    pos.y = _basePos.y + startY;
                    _state = FloatState.Idle;
                    break;
                }
                t = Mathf.Clamp01(_timer / fallDuration);
                pos.y = _basePos.y + Mathf.Lerp(peakY, startY, fallCurve.Evaluate(t));
                if (t >= 1f)
                {
                    pos.y = _basePos.y + startY;
                    _state = FloatState.Idle;
                }
                break;
        }

        rt.anchoredPosition = pos;
        dynamicBackground?.RequestImmediateRefresh();
    }
}
