using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class PixelationEffect : MonoBehaviour
{
    public Camera mainCamera;      // 픽셀화 할 카메라 (Main Camera)
    public RawImage displayImage;  // 화면에 출력할 UI RawImage

    [Header("Pixelation Settings")]
    public int pixelWidth = 320;   // 가로 해상도
    public int pixelHeight = 180;  // 세로 해상도

    private RenderTexture pixelRT;
    public bool isPixelationEnabled = false;

    void Update()
    {
        // Input System 방식으로 Ctrl 키 체크
        if (Keyboard.current.leftCtrlKey.wasPressedThisFrame || 
            Keyboard.current.rightCtrlKey.wasPressedThisFrame)
        {
            TogglePixelation();
        }
    }

    void TogglePixelation()
    {
        if (isPixelationEnabled)
        {
            DisablePixelation();
        }
        else
        {
            EnablePixelation();
        }
    }

    void EnablePixelation()
    {
        // 화면 종횡비에 맞춘 픽셀화 해상도 계산
        float screenAspect = (float)Screen.width / Screen.height;
        int adjustedPixelWidth = Mathf.RoundToInt(pixelHeight * screenAspect);
        
        // RenderTexture 생성
        if (pixelRT != null)
        {
            pixelRT.Release(); // 기존 것 해제
        }
        
        pixelRT = new RenderTexture(adjustedPixelWidth, pixelHeight, 16);
        pixelRT.filterMode = FilterMode.Point;

        // 카메라 설정
        mainCamera.targetTexture = pixelRT;

        // RawImage 설정
        displayImage.texture = pixelRT;
        displayImage.uvRect = new Rect(0, 0, 1, 1);
        
        // RawImage 크기를 화면에 정확히 맞춤
        RectTransform rt = displayImage.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        
        displayImage.gameObject.SetActive(true);
        isPixelationEnabled = true;
    }

    void DisablePixelation()
    {
        // 카메라 타겟을 다시 화면으로 설정
        mainCamera.targetTexture = null;

        // RawImage 비활성화
        displayImage.gameObject.SetActive(false);
        displayImage.texture = null;

        isPixelationEnabled = false;
        Debug.Log("픽셀화 효과 비활성화");
    }

    void OnDestroy()
    {
        // 해제
        if (pixelRT != null)
        {
            pixelRT.Release();
        }
        
        // 카메라 타겟 텍스처 해제
        if (mainCamera != null)
        {
            mainCamera.targetTexture = null;
        }
    }
}