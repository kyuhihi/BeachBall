using System;

public static class GameInitBarrier
{
    public static bool IsReady { get; private set; }
    public static event Action OnReady;

    public static void Reset()
    {
        IsReady = false;
    }

    public static void SetReady()
    {
        if (IsReady) return;
        IsReady = true;
        OnReady?.Invoke();
    }
}