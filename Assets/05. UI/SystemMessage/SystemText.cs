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
    [SerializeField] private TMPDynamicBackground dynamicBackground; // 같은 오브젝트에 붙어있다면 자동 탐색

    public event Action<string> OnTextChanged;
    public event Action OnAnimationRestart;

    private TextMeshProUGUI _tmp;
    private Vector2 _basePos;          // 최초 기준(변하지 않음)
    private Vector2 _initialBasePos;   // 최초 저장용(디버그/재설정)
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
        // 초기 텍스트 기준 상태 잡기
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
            dynamicBackground.RequestImmediateRefresh(); // 배경 즉시 리사이즈
    }

    // 애니 다시 시작 (기준점 드리프트 방지: _basePos 안 바꿈)
    private void RestartAnimation()
    {
        if (!enablePopFloat) return;

        _state = FloatState.Rising;
        _timer = 0f;

        var rt = (RectTransform)transform;
        // 기준 pos 는 최초 값 유지
        Vector2 p = _basePos;
        p.y = _basePos.y + startY;     // 아래 시작
        rt.anchoredPosition = p;

        OnAnimationRestart?.Invoke();
        dynamicBackground?.RequestImmediateRefresh();
    }

    // 필요 시 외부에서 기준 복구
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
        // 팝 비활성 시 종료
        if (!enablePopFloat) return;

        // Idle 상태면 더 이상 이동 없음 (위 입력으로 재시작 가능)
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
