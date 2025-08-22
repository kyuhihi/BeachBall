using UnityEngine;
using System.Collections;
public class UltimateWater : MonoBehaviour
{   
    private float startY = -4f;        // 시작 높이
    private float endY = 4f;           // 최종 높이
    private float fillDuration = 10f;  // 기본 차오르는 시간(초)
    private float speedMultiplier = 0.2f; // 속도 배율

    public void StartFillWater()
    {
        StartCoroutine(FillWaterRoutine());
    }

    // 외부에서 호출: 물 차오르는 속도 빠르게
    public void SpeedUpFill()
    {
        speedMultiplier = 2f; // 원하는 만큼 빠르게 (예: 3배)
    }

    private IEnumerator FillWaterRoutine()
    {
        float elapsed = 0f;
        Vector3 pos = transform.position;
        while (elapsed < fillDuration)
        {
            elapsed += Time.deltaTime * speedMultiplier;
            float t = Mathf.Clamp01(elapsed / fillDuration);
            pos.y = Mathf.Lerp(startY, endY, t);
            transform.position = pos;
            yield return null;
        }
        pos.y = endY;
        transform.position = pos;
    }
}