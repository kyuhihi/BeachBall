using System;
using UnityEngine;

/// <summary>
/// �Ͻ����� ���� �������̽�.
/// ����ü�� IsPaused ���¿� ���� Update/Coroutine/�ִ� ���� ������ �ߴ��ϰų� �簳�ϴ� ������ �־�� �Ѵ�.
/// </summary>
public interface IPauseable
{
    /// <summary>���� �Ͻ����� ����</summary>
    bool IsPaused { get; }

    /// <summary>�Ͻ����� ��û</summary>
    void Pause();

    /// <summary>�簳 ��û</summary>
    void Resume();

    /// <summary>���� ���� (�ܺ� �ý����� ���� ���� ����ȭ�� ��)</summary>
    void SetPaused(bool paused);

    /// <summary>���°� ����� �� (true=Paused, false=Resumed)</summary>
    event Action<bool> PauseStateChanged;
}