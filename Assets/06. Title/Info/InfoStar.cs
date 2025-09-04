using UnityEngine;
using UnityEngine.UI;

public class InfoStar : MonoBehaviour
{
    RectTransform _rectTransform;
    Image _image;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _rectTransform = GetComponent<RectTransform>();
        _image = GetComponent<Image>();
    }

    // Update is called once per frame
    void Update()
    {
        Color color = _image.color;
        color.a = 0.8f + 0.5f * Mathf.Sin(Time.time);
        _image.color = color;

        _rectTransform.rotation = Quaternion.Euler(0, 0, _rectTransform.rotation.eulerAngles.z + Time.deltaTime * 180f);
    }
}
