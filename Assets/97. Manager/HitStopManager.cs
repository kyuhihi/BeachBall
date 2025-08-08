// HitStopManager.cs
using UnityEngine;
using System.Collections;

public class HitStopManager : MonoBehaviour
{
    public static HitStopManager Instance { get; private set; }

    private bool isHitStopping = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    public void DoHitStop(float duration, float slowTimeScale = 0f)
    {
        if (!isHitStopping)
            StartCoroutine(HitStopCoroutine(duration, slowTimeScale));
    }

    private IEnumerator HitStopCoroutine(float duration, float slowTimeScale)
    {
        isHitStopping = true;

        Time.timeScale = slowTimeScale;
        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = 1f;
        isHitStopping = false;
    }
}
