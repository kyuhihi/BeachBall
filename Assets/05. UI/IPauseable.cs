using System;
using UnityEngine;

/// <summary>
/// 일시정지 제어 인터페이스.
/// 구현체는 IsPaused 상태에 따라 Update/Coroutine/애니 등을 스스로 중단하거나 재개하는 로직을 넣어야 한다.
/// </summary>
public interface IPauseable
{
    /// <summary>현재 일시정지 여부</summary>
    bool IsPaused { get; }

    /// <summary>일시정지 요청</summary>
    void Pause();

    /// <summary>재개 요청</summary>
    void Resume();

    /// <summary>강제 설정 (외부 시스템이 직접 상태 동기화할 때)</summary>
    void SetPaused(bool paused);

    /// <summary>상태가 변경될 때 (true=Paused, false=Resumed)</summary>
    event Action<bool> PauseStateChanged;
}