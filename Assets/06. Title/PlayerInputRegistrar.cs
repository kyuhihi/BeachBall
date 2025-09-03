using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerInput))]
public class PlayerInputRegistrar : MonoBehaviour
{

    public string id;

    public string slot;
    public enum SlotMode
    {
        AutoFromPlayerIndex, // PlayerInput.playerIndex → P1/P2/...
        P1,
        P2,
        CPU,

        REALCPU

    }

    [Header("Slot 오브젝트 프리팹")]
    public GameObject slotP1Prefab;
    public GameObject slotP2Prefab;
    public GameObject slotCPUPrefab;

    private GameObject _slotObjInstance;

    [Header("Slot 설정")]
    public SlotMode slotMode = SlotMode.AutoFromPlayerIndex;

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

        if (_resolvedSlot != "REALCPU")
        {
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


        // 슬롯별 텍스처 오브젝트 생성
        GameObject prefab = null;
        switch (_resolvedSlot)
        {
            case "P1": prefab = slotP1Prefab; break;
            case "P2": prefab = slotP2Prefab; break;
            case "CPU": prefab = slotP1Prefab; break;
            case "REALCPU": prefab = slotCPUPrefab; break;
        }
        if (prefab != null)
        {
            _slotObjInstance = Instantiate(prefab, transform);
            _slotObjInstance.transform.localPosition = new Vector3(0, 2.2f, 0); // 머리 위 위치(조정)

            // 회전을 (90, 0, 90)으로 고정
            _slotObjInstance.transform.localRotation = Quaternion.Euler(90f, 0f, 90f);
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
            case SlotMode.REALCPU: return "REALCPU";

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