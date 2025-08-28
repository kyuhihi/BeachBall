using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerInput))]
public class PlayerInputRegistrar : MonoBehaviour
{
    public enum SlotMode
    {
        AutoFromPlayerIndex, // PlayerInput.playerIndex → P1/P2/...
        P1,
        P2,
        CPU,
        Custom               // 아래 customSlot 사용
    }

    [Header("Slot 설정")]
    public SlotMode slotMode = SlotMode.AutoFromPlayerIndex;

    [Tooltip("slotMode가 Custom일 때만 사용. 예: P3, Boss, Spectator 등")]
    public string customSlot;

    [FormerlySerializedAs("slotOverride")] // 기존 필드명을 썼다면 자동 마이그레이션
    [SerializeField, HideInInspector] private string slotOverride_legacy;

    private PlayerInput _playerInput;
    private string _resolvedSlot;

    void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
    }

    void Start()
    {
        // Start에서 등록: PlayerInputManager가 playerIndex를 지정한 뒤라 안전
        _resolvedSlot = ResolveSlot();
        if (GameSettings.Instance != null && _playerInput != null && _playerInput.actions != null && !string.IsNullOrEmpty(_resolvedSlot))
        {
            GameSettings.Instance.RegisterActionsForSlot(_resolvedSlot, _playerInput.actions);
            // 디버그 확인용
            Debug.Log($"[PlayerInputRegistrar] Registered slot={_resolvedSlot}, asset={_playerInput.actions.name}");
        }
        else
        {
            Debug.LogWarning($"[PlayerInputRegistrar] 등록 실패. slot={_resolvedSlot}, playerInput/actions/GameSettings null 확인");
        }
    }

    private string ResolveSlot()
    {
        // 레거시 문자열이 있으면 우선 사용
        if (!string.IsNullOrEmpty(slotOverride_legacy))
            return slotOverride_legacy;

        switch (slotMode)
        {
            case SlotMode.P1: return "P1";
            case SlotMode.P2: return "P2";
            case SlotMode.CPU: return "CPU";
            case SlotMode.Custom:
                return string.IsNullOrWhiteSpace(customSlot) ? null : customSlot.Trim();
            case SlotMode.AutoFromPlayerIndex:
            default:
                // playerIndex(0→P1, 1→P2, …)
                var idx = (_playerInput != null) ? _playerInput.playerIndex : -1;
                return (idx >= 0) ? $"P{idx + 1}" : null;
        }
    }

#if UNITY_EDITOR
    // 인스펙터에서 값 바꿀 때 미리보기
    void OnValidate()
    {
        if (_playerInput == null) _playerInput = GetComponent<PlayerInput>();
        var preview = ResolveSlot();
        // 에디터 콘솔 스팸 방지: 필요 시 주석 처리
        // if (!string.IsNullOrEmpty(preview))
        //     Debug.Log($"[PlayerInputRegistrar] Preview slot = {preview} ({name})");
    }
#endif
}