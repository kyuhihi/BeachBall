using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

[ExecuteAlways]
[RequireComponent(typeof(TextMeshProUGUI))]
public class TMPDynamicBackground : MonoBehaviour
{
    [Header("Background")]
    [SerializeField] private Sprite roundRectSprite;        // 9-Sliced 라운드 스프라이트
    [SerializeField] private Color  backgroundColor = new Color(0,0,0,0.55f);
    [SerializeField] private Vector2 padding = new Vector2(24f, 12f); // 좌우, 상하 여백 (pixel)
    [SerializeField] private bool matchTextAlpha = false;
    [SerializeField] private bool updateEveryFrameInEditor = true;

    [Header("Min / Max Size (선택)")]
    [SerializeField] private Vector2 minSize = new Vector2(0,0);
    [SerializeField] private Vector2 maxSize = new Vector2(9999,9999);

    [Header("외부 배경 재사용 옵션")]
    [Tooltip("이미 씬/프리팹에 만들어둔 배경 RectTransform을 직접 넣으면 재생성/검색을 하지 않습니다.")]
    [SerializeField] private RectTransform externalBackground;
    [Tooltip("외부 배경이 없을 때 자동으로 새 배경을 만들지 여부")]
    [SerializeField] private bool autoCreateIfMissing = true;

    private TextMeshProUGUI _tmp;
    private RectTransform   _textRect;
    private RectTransform   _bgRect;
    private Image           _bgImage;
    private string          _lastText;
    private Vector2         _lastPreferred;
    private bool            _dirty = true;

    private Vector2 _lastPivot;
    private Vector2 _lastAnchorMin;
    private Vector2 _lastAnchorMax;
    private Vector2 _lastAnchoredPos;
    public void RequestImmediateRefresh()
    {
        _dirty = true;
    }
    void Awake()
    {
        _tmp = GetComponent<TextMeshProUGUI>();
        _textRect = _tmp.rectTransform;

        // 외부 배경 우선 적용
        if (externalBackground != null)
        {
            _bgRect = externalBackground;
            _bgImage = _bgRect.GetComponent<Image>();
            if (_bgImage == null) _bgImage = _bgRect.gameObject.AddComponent<Image>();
            ApplyBgVisual();
        }

        CacheTransformState();
        EnsureBackground();  // externalBackground 있으면 내부에서 바로 return
        MarkDirty();
    }

    void CacheTransformState()
    {
        if (_textRect == null) return;
        _lastPivot       = _textRect.pivot;
        _lastAnchorMin   = _textRect.anchorMin;
        _lastAnchorMax   = _textRect.anchorMax;
        _lastAnchoredPos = _textRect.anchoredPosition;
    }

    bool TransformStateChanged()
    {
        if (_textRect == null) return false;
        return  _lastPivot       != _textRect.pivot
             || _lastAnchorMin   != _textRect.anchorMin
             || _lastAnchorMax   != _textRect.anchorMax
             || _lastAnchoredPos != _textRect.anchoredPosition;
    }

    void OnEnable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTMPChanged);
        MarkDirty();
        StartCoroutine(CoLateInit());
    }

    void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTMPChanged);
    }

    IEnumerator CoLateInit()
    {
        // 레이아웃 계산 끝난 뒤 한 프레임 후 갱신
        yield return null;
        MarkDirty();
    }

    void OnTMPChanged(Object obj)
    {
        if (obj == _tmp) MarkDirty();
    }

    void EnsureBackground()
    {
        // 이미 참조 존재하면 재생성/검색 안 함
        if (_bgRect != null)
        {
            ApplyBgVisual();
            return;
        }

        // 외부 배경이 없고, 자동 생성도 끈 경우: 아무 작업 하지 않음
        if (!autoCreateIfMissing)
            return;

        // (기존) 부모 밑에서 legacy 이름 검색
        Transform existing = transform.parent ? transform.parent.Find(name + "_BG") : null;
        if (existing != null)
        {
            _bgRect = existing as RectTransform;
            _bgImage = _bgRect.GetComponent<Image>();
        }
        else
        {
            // 새 생성
            var go = new GameObject(name + "_BG", typeof(RectTransform), typeof(Image));
            _bgRect = go.GetComponent<RectTransform>();
            _bgRect.SetParent(transform.parent, false);
            _bgRect.localScale = Vector3.one;
            _bgImage = go.GetComponent<Image>();
        }

        _bgRect.SetAsFirstSibling(); // 텍스트 뒤
        CopyAnchors(_textRect, _bgRect);
        ApplyBgVisual();
    }

    void ApplyBgVisual()
    {
        if (_bgImage == null) return;
        _bgImage.sprite = roundRectSprite;
        _bgImage.type = (roundRectSprite != null) ? Image.Type.Sliced : Image.Type.Simple;
        _bgImage.color = backgroundColor;
        _bgImage.raycastTarget = false;
    }

    void CopyAnchors(RectTransform src, RectTransform dst)
    {
        if (src == null || dst == null) return;
        dst.anchorMin = src.anchorMin;
        dst.anchorMax = src.anchorMax;
        dst.pivot     = src.pivot;
        dst.anchoredPosition = src.anchoredPosition;
    }

    void MarkDirty() => _dirty = true;

    void Update()
    {
        if (!Application.isPlaying && !updateEveryFrameInEditor && !_dirty)
        {
            if (TransformStateChanged()) MarkDirty(); else return;
        }
        if (_tmp == null) return;

        if (TransformStateChanged())
        {
            if (_bgRect) CopyAnchors(_textRect, _bgRect);
            MarkDirty();
            CacheTransformState();
        }

        if (matchTextAlpha && _bgImage != null)
        {
            var c = _bgImage.color;
            float targetA = _tmp.color.a * backgroundColor.a;
            if (!Mathf.Approximately(c.a, targetA))
                _bgImage.color = new Color(backgroundColor.r, backgroundColor.g, backgroundColor.b, targetA);
        }

        if (_dirty || TextChanged() || PreferredChanged())
        {
            RefreshSize();
            _dirty = false;
            CacheTransformState();
        }
    }

    void OnRectTransformDimensionsChange()
    {
        // 크기/피벗/앵커 변경 시점에 호출되므로 표시 갱신
        if (isActiveAndEnabled)
        {
            MarkDirty();
        }
    }

    bool TextChanged()
    {
        if (_lastText != _tmp.text)
        {
            _lastText = _tmp.text;
            return true;
        }
        return false;
    }

    bool PreferredChanged()
    {
        Vector2 pref = new Vector2(_tmp.preferredWidth, _tmp.preferredHeight);
        if ((pref - _lastPreferred).sqrMagnitude > 0.01f)
        {
            _lastPreferred = pref;
            return true;
        }
        return false;
    }

    void RefreshSize()
    {
        EnsureBackground();
        if (_bgRect == null) return; // 외부 배경 없고 autoCreateIfMissing = false 인 경우

        Vector2 pref = new Vector2(_tmp.preferredWidth, _tmp.preferredHeight);
        if (pref.x <= 0f || pref.y <= 0f) pref = Vector2.zero;

        Vector2 target = pref + new Vector2(padding.x * 2f, padding.y * 2f);
        target.x = Mathf.Clamp(target.x, minSize.x, maxSize.x);
        target.y = Mathf.Clamp(target.y, minSize.y, maxSize.y);

        _bgRect.sizeDelta = target;
        CopyAnchors(_textRect, _bgRect);
    }

#if UNITY_EDITOR
    // 에디터에서 변경 감지 (인스펙터 값 수정 시)
    void OnValidate()
    {
        if (_bgRect == null && externalBackground != null)
        {
            _bgRect = externalBackground;
            _bgImage = _bgRect.GetComponent<Image>();
            if (_bgImage == null) _bgImage = _bgRect.gameObject.AddComponent<Image>();
        }
        ApplyBgVisual();
        MarkDirty();
    }
#endif
}
