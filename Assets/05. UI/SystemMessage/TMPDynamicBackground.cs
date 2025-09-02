using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

[ExecuteAlways]
[RequireComponent(typeof(TextMeshProUGUI))]
public class TMPDynamicBackground : MonoBehaviour
{
    [Header("Background")]
    [SerializeField] private Sprite roundRectSprite;        // 9-Sliced ���� ��������Ʈ
    [SerializeField] private Color  backgroundColor = new Color(0,0,0,0.55f);
    [SerializeField] private Vector2 padding = new Vector2(24f, 12f); // �¿�, ���� ���� (pixel)
    [SerializeField] private bool matchTextAlpha = false;
    [SerializeField] private bool updateEveryFrameInEditor = true;

    [Header("Min / Max Size (����)")]
    [SerializeField] private Vector2 minSize = new Vector2(0,0);
    [SerializeField] private Vector2 maxSize = new Vector2(9999,9999);

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
        CacheTransformState();
        EnsureBackground();
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
        // ���̾ƿ� ��� ���� �� �� ������ �� ����
        yield return null;
        MarkDirty();
    }

    void OnTMPChanged(Object obj)
    {
        if (obj == _tmp) MarkDirty();
    }

    void EnsureBackground()
    {
        if (_bgRect != null) return;

        Transform existing = transform.parent.Find(name + "_BG");
        if (existing != null)
        {
            _bgRect = existing as RectTransform;
            _bgImage = _bgRect.GetComponent<Image>();
        }
        else
        {
            var go = new GameObject(name + "_BG", typeof(RectTransform), typeof(Image));
            _bgRect = go.GetComponent<RectTransform>();
            _bgRect.SetParent(transform.parent, false);
            _bgImage = go.GetComponent<Image>();
            _bgRect.localScale = Vector3.one;
        }

        _bgRect.SetAsFirstSibling(); // �׻� �ؽ�Ʈ ��
        _bgImage.sprite = roundRectSprite;
        _bgImage.type = (roundRectSprite != null) ? Image.Type.Sliced : Image.Type.Simple;
        _bgImage.color = backgroundColor;
        _bgImage.raycastTarget = false;

        // �ؽ�Ʈ�� ������ pivot/anchor ����
        CopyAnchors(_textRect, _bgRect);
    }

    void CopyAnchors(RectTransform src, RectTransform dst)
    {
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
            // �׷��� Transform ��ȭ�� üũ
            if (TransformStateChanged()) { MarkDirty(); }
            else return;
        }
        if (_tmp == null) return;

        // Transform ��ȭ ���� �� ��Ŀ/�ǹ� �絿��
        if (TransformStateChanged())
        {
            CopyAnchors(_textRect, _bgRect);
            MarkDirty();
            CacheTransformState();
        }

        // ���� ��Ī
        if (matchTextAlpha && _bgImage != null)
        {
            var c = _bgImage.color;
            float targetA = _tmp.color.a * backgroundColor.a;
            if (!Mathf.Approximately(c.a, targetA))
            {
                c = new Color(backgroundColor.r, backgroundColor.g, backgroundColor.b, targetA);
                _bgImage.color = c;
            }
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
        // ũ��/�ǹ�/��Ŀ ���� ������ ȣ��ǹǷ� ǥ�� ����
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

        // TMP�� AutoSize / Wrap �� �ݿ� �� preferred ���
        Vector2 pref = new Vector2(_tmp.preferredWidth, _tmp.preferredHeight);
        if (pref.x <= 0f || pref.y <= 0f)
        {
            // ���� ������ �ּ� �е���
            pref = Vector2.zero;
        }

        Vector2 target = pref;
        target += new Vector2(padding.x * 2f, padding.y * 2f);

        // Min/Max clamp
        target.x = Mathf.Clamp(target.x, minSize.x, maxSize.x);
        target.y = Mathf.Clamp(target.y, minSize.y, maxSize.y);

        // Pivot ���� sizeDelta ����
        _bgRect.sizeDelta = target;

        // ��ġ ����
        _bgRect.anchoredPosition = _textRect.anchoredPosition;
        CopyAnchors(_textRect, _bgRect);
    }

#if UNITY_EDITOR
    // �����Ϳ��� ���� ���� (�ν����� �� ���� ��)
    void OnValidate()
    {
        if (_bgImage != null)
        {
            _bgImage.sprite = roundRectSprite;
            _bgImage.type = (roundRectSprite != null) ? Image.Type.Sliced : Image.Type.Simple;
            _bgImage.color = backgroundColor;
        }
        MarkDirty();
    }
#endif
}
