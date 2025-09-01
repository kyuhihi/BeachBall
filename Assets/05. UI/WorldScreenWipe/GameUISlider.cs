using UnityEngine;

public class GameUISlider : MonoBehaviour, IResetAbleListener
{
    PlayerUIManager m_PlayerUIManager;
    private RectTransform _rect;

    private int lastNumber  = int.MinValue;
    private const float ShowPivotY = 0.0f;
    private const float HidePivotY = -300.0f;
    private float TargetPivotY = 0.0f;

    public void RemoveResetCall()
    {
        Signals.RoundResetAble.RemoveStart(OnRoundStart);
        Signals.RoundResetAble.RemoveEnd(OnRoundEnd);
    }

    public void AddResetCall()
    {
        Signals.RoundResetAble.AddStart(OnRoundStart);
        Signals.RoundResetAble.AddEnd(OnRoundEnd);
    }
    public void OnRoundStart()
    {
        TargetPivotY = ShowPivotY;
    }

    public void OnRoundEnd()
    {
        lastNumber = int.MinValue;
    }

    public void OnEnable()
    {
        AddResetCall();
    }
    public void OnDisable()
    {
        RemoveResetCall();
    }

    // Update is called once per frame
    void Update()
    {
        if (m_PlayerUIManager == null || _rect == null)
        {
            m_PlayerUIManager = PlayerUIManager.GetInstance();
            _rect = GetComponent<RectTransform>();
        }
        int currentSecond = m_PlayerUIManager.GetCurrentSecond();
        if (lastNumber == 1 && currentSecond == 0)
            TargetPivotY = HidePivotY;

        _rect.anchoredPosition = new Vector2(_rect.anchoredPosition.x, Mathf.Lerp(_rect.anchoredPosition.y, TargetPivotY, Time.deltaTime * 10f));
        lastNumber = currentSecond;

    }
}
