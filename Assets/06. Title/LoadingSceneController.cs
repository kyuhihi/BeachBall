using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingSceneController : MonoBehaviour
{
    [SerializeField] private Slider progressBar; // 선택(없어도 동작)
    [SerializeField] private float minShowTime = 2f;           // 최소 표출 시간
    [SerializeField] private float visualFillSpeed = 0.5f;     // 1초당 시각 증가량(0~1)
    [SerializeField] private float introWarmupSeconds = 0.3f;  // 시작 워밍업 연출 시간
    [SerializeField] private float bootstrapTarget = 0.1f;     // 초반 최소 채움 목표(0~1)
    [SerializeField] private float maxVisualStepPerFrame = 0.08f; // 프레임당 최대 증가량

    private float StepVisual(float current, float target)
    {
        // 큰 dt(프레임 스파이크)에서도 과도한 점프 방지
        float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
        float step = Mathf.Min(visualFillSpeed * dt, maxVisualStepPerFrame);
        return Mathf.MoveTowards(current, target, step);
    }

    private IEnumerator Start()
    {
        yield return null; // 한 프레임 양보

        var targetScene = SceneLoader.NextSceneName;
        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogWarning("[Loading] Target scene is empty.");
            yield break;
        }

        float startTime = Time.unscaledTime;

        var op = SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Single);
        op.allowSceneActivation = false;

        float display01 = 0f; // 화면 표시용
        float target01 = 0f;  // 목표값

        // 1) 인트로 워밍업: 잠깐 0→bootstrapTarget로 부드럽게
        float warm = 0f;
        while (warm < introWarmupSeconds)
        {
            warm += Time.unscaledDeltaTime;
            display01 = StepVisual(display01, bootstrapTarget);
            if (progressBar) progressBar.value = display01;
            yield return null;
        }

        // 2) 실제 진행도 반영(초반 바닥은 유지)
        while (op != null && op.progress < 0.9f)
        {
            target01 = Mathf.Max(bootstrapTarget, Mathf.Clamp01(op.progress / 0.9f));
            display01 = StepVisual(display01, target01);
            if (progressBar) progressBar.value = display01;
            yield return null;
        }

        // 3) 마무리: 1.0까지 채우고 최소 표출 시간 보장
        target01 = 1f;
        while (Time.unscaledTime - startTime < minShowTime || (target01 - display01) > 0.01f)
        {
            display01 = StepVisual(display01, target01);
            if (progressBar) progressBar.value = display01;
            yield return null;
        }

        if (progressBar) progressBar.value = 1f;
        op.allowSceneActivation = true;
    }
}