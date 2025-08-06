using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class RawImageResizer : MonoBehaviour
{
    public RawImage rawImage;
    public PixelationEffect pixelationEffect; // PixelationEffect 참조

    void Awake()
    {
        if (rawImage == null)
            rawImage = GetComponent<RawImage>();
            
        // 시작할 때는 비활성화
        if (rawImage != null)
        {
            rawImage.enabled = false;
        }
    }

    void Start()
    {
        GameObject pixelGameObject = GameObject.FindGameObjectWithTag("PixelationEffect");
        if (pixelGameObject != null)
        {
            pixelationEffect = pixelGameObject.GetComponent<PixelationEffect>();
        }
        //ResizeToScreen();
    }

    void Update()
    {
        // Input System 방식으로 Ctrl 키 체크
        if (Keyboard.current.leftCtrlKey.wasPressedThisFrame || 
            Keyboard.current.rightCtrlKey.wasPressedThisFrame)
        {
            ToggleRawImage();
        }

        // RawImage가 활성화되어 있을 때만 크기 체크
        if (rawImage.enabled)
        {
            if ((int)rawImage.rectTransform.rect.width != Screen.width ||
                (int)rawImage.rectTransform.rect.height != Screen.height)
            {
                //ResizeToScreen();
            }
        }
    }

    void ToggleRawImage()
    {
        // PixelationEffect의 상태와 연동
        if (pixelationEffect != null)
        {
            bool shouldEnable = pixelationEffect.isPixelationEnabled;
            rawImage.enabled = shouldEnable;
            
            if (shouldEnable)
            {
                //ResizeToScreen();
            }
        }
        else
        {
            // PixelationEffect가 없으면 독립적으로 토글
            rawImage.enabled = !rawImage.enabled;
            if (rawImage.enabled)
            {
                //ResizeToScreen();
            }
        }
    }

    void ResizeToScreen()
    {
        if (rawImage == null)
            return;
            
        RectTransform rt = rawImage.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(Screen.width, Screen.height);
    }
}