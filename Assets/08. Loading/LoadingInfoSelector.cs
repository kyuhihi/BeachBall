// filepath: c:\Users\Lenovo\BeachBall\Assets\08. Loading\LoadingInfoSelector.cs
using UnityEngine;
using System.Collections.Generic;

public class LoadingInfoSelector : MonoBehaviour
{
    [Header("��ȯ ����")]
    [SerializeField] private float fadeOutDuration = 0.25f;
    [SerializeField] private float fadeInDuration  = 0.25f;
    [SerializeField] private float holdDuration    = 2.0f;
    [SerializeField] private AnimationCurve fadeOutCurve = AnimationCurve.EaseInOut(0,1,1,0);
    [SerializeField] private AnimationCurve fadeInCurve  = AnimationCurve.EaseInOut(0,0,1,1);
    [SerializeField] private bool useUnscaledTime = false;

    [Header("ǥ�� �ɼ�")]
    [SerializeField] private bool onlyCenterActive = true;
    [SerializeField] private bool autoPlayOnStart = false;
    [SerializeField] private bool loop = true;
    [SerializeField] private float minHoldClamp = 0.05f; // �ʹ� ª�� �����ϴ� �� ����

    [Header("�����")]
    [SerializeField] private bool debugLog = false;

    private RectTransform[] _items;
    private CanvasGroup[] _groups;
    private int _count;
    private int _currentIndex = 0;
    private bool _transitioning = false;

    // �� ����: AutoPlayRoutine ����, Ÿ�ӽ����� ���
    private float _nextSwitchTime = -1f;
    private int _lastSwitchFrame = -1;

    void Start()
    {
        CollectChildren();
        InitVisibility();
        if (autoPlayOnStart) ScheduleNextAuto();
        if (debugLog) Debug.Log($"[LoadingInfoSelector] Start - items:{_count}", this);
    }

    void OnEnable()
    {
        // �� ��Ȱ�� �� �ڵ���� ����
        if (autoPlayOnStart && _nextSwitchTime < 0f)
            ScheduleNextAuto();
    }

    void OnDisable()
    {
        // ��Ȱ��ȭ �� ���� ���� ����
        _nextSwitchTime = -1f;
    }

    void Update()
    {
        float now = useUnscaledTime ? Time.unscaledTime : Time.time;

        // �ڵ� ��ȯ
        if (autoPlayOnStart && !_transitioning && _count > 1 && _nextSwitchTime > 0f && now >= _nextSwitchTime)
        {
            if (debugLog) Debug.Log("[LoadingInfoSelector] Auto Next()", this);
            NextInternal(auto:true);
        }
    }

    void CollectChildren()
    {
        var list = new List<RectTransform>();
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).gameObject.name.Contains("Info"))
            {
                var rt = transform.GetChild(i) as RectTransform;
                if (rt) list.Add(rt);
            }
        }
        _items = list.ToArray();
        _count = _items.Length;
        _groups = new CanvasGroup[_count];
        for (int i = 0; i < _count; i++)
        {
            var cg = _items[i].GetComponent<CanvasGroup>();
            if (!cg) cg = _items[i].gameObject.AddComponent<CanvasGroup>();
            _groups[i] = cg;
        }
    }

    void InitVisibility()
    {
        for (int i = 0; i < _count; i++)
        {
            bool active = (i == _currentIndex);
            _groups[i].alpha = active ? 1f : 0f;
            if (onlyCenterActive) _items[i].gameObject.SetActive(active);
        }
    }

    void ScheduleNextAuto()
    {
        if (!autoPlayOnStart || _count <= 1) return;
        float hold = Mathf.Max(holdDuration, minHoldClamp);
        float now = useUnscaledTime ? Time.unscaledTime : Time.time;
        _nextSwitchTime = now + hold;
    }

    public void Next() => NextInternal(auto:false);
    public void Prev()
    {
        if (_transitioning || _count <= 1) return;
        int prev = _currentIndex - 1;
        if (prev < 0)
        {
            if (!loop) return;
            prev = _count - 1;
        }
        StartTransition(_currentIndex, prev);
    }

    void NextInternal(bool auto)
    {
        if (_transitioning || _count <= 1) return;
        // �ߺ� ���� ȣ�� ������ġ(���� ������ ��ȣ�� ����)
        if (Time.frameCount == _lastSwitchFrame) return;

        int next = _currentIndex + 1;
        if (next >= _count)
        {
            if (!loop) return;
            next = 0;
        }
        StartTransition(_currentIndex, next);
        _lastSwitchFrame = Time.frameCount;
        if (auto) ScheduleNextAuto(); // ���� ���� ����
    }

    void StartTransition(int from, int to)
    {
        if (debugLog) Debug.Log($"[LoadingInfoSelector] Transition {from} -> {to}", this);
        StartCoroutine(FadeSwitch(from, to));
    }

    System.Collections.IEnumerator FadeSwitch(int from, int to)
    {
        _transitioning = true;
        if (onlyCenterActive) _items[to].gameObject.SetActive(true);

        CanvasGroup gFrom = _groups[from];
        CanvasGroup gTo = _groups[to];

        gTo.alpha = 0f;
        float t = 0f;

        while (t < Mathf.Max(fadeOutDuration, fadeInDuration))
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            t += dt;

            if (fadeOutDuration > 0f)
            {
                float kOut = Mathf.Clamp01(t / fadeOutDuration);
                gFrom.alpha = fadeOutCurve.Evaluate(kOut);
            }
            if (fadeInDuration > 0f)
            {
                float kIn = Mathf.Clamp01(t / fadeInDuration);
                gTo.alpha = fadeInCurve.Evaluate(kIn);
            }
            yield return null;
        }

        gFrom.alpha = 0f;
        gTo.alpha = 1f;
        if (onlyCenterActive) _items[from].gameObject.SetActive(false);

        _currentIndex = to;
        _transitioning = false;

        // ����� ���� ��ȯ �Ŀ��� �ڵ� ��� ����
        ScheduleNextAuto();
    }

#if UNITY_EDITOR
    [ContextMenu("Recollect & Reset")]
    void Recollect()
    {
        CollectChildren();
        _currentIndex = Mathf.Clamp(_currentIndex, 0, Mathf.Max(0, _count - 1));
        InitVisibility();
        ScheduleNextAuto();
        if (debugLog) Debug.Log("[LoadingInfoSelector] Recollect & Reset", this);
    }
#endif
}
