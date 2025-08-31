using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(CanvasGroup))]
public class LoadingOverlay : MonoBehaviour
{
    [SerializeField] private bool blockRaycasts = true;
    [SerializeField] private int sortingOrder = 32767; // 최상단
    [SerializeField] private Color overlayColor = new Color(0,0,0,1f); 

    private Canvas _canvas;
    private CanvasGroup _group;
    private Image _bg;

    private void Awake()
    {
        EnsureComponents();
        EnsureFullScreenBlocker();
    }

    private void EnsureComponents()
    {
        if (!_canvas) _canvas = GetComponent<Canvas>();
        if (!_group) _group = GetComponent<CanvasGroup>();

        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.overrideSorting = true;
        _canvas.sortingOrder = sortingOrder;

        _group.interactable = false;
        _group.blocksRaycasts = blockRaycasts;
    }

    private void EnsureFullScreenBlocker()
    {
        if (_bg == null)
        {
            var rt = GetComponent<RectTransform>();
            if (!rt) rt = gameObject.AddComponent<RectTransform>();

            if (transform.childCount == 0 || (transform.childCount > 0 && transform.GetChild(0).GetComponent<Image>() == null))
            {
                var go = new GameObject("OverlayBG", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                _bg = go.GetComponent<Image>();
            }
            else
            {
                _bg = transform.GetChild(0).GetComponent<Image>();
            }

            var bgRT = _bg.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;

            _bg.raycastTarget = true; // 다른 UI 차단
            _bg.color = overlayColor;
        }
    }

    public void Show()
    {
        EnsureComponents();
        EnsureFullScreenBlocker();
        gameObject.SetActive(true);
        _group.alpha = 1f;
        _group.blocksRaycasts = blockRaycasts;
    }

    public void HideImmediate()
    {
        EnsureComponents();
        gameObject.SetActive(false);
        _group.alpha = 0f;
        _group.blocksRaycasts = false;
    }

    public System.Collections.IEnumerator FadeOut(float duration = 0.25f)
    {
        EnsureComponents();
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            _group.alpha = 1f - Mathf.Clamp01(t / duration);
            yield return null;
        }
        HideImmediate();
    }
}