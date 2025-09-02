using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-10000)]
public class GameSceneManager : MonoBehaviour
{
    public static bool IsInitialized { get; private set; }

    [Header("Scene Roots")]
    public Transform leftRoot;
    public Transform rightRoot;


    [Tooltip("1vsCPU 모드에서 오른쪽을 스폰할지 여부(끄면 왼쪽만 스폰)")]
    [SerializeField] private bool cpuModeSpawnRight = false;

    [Header("디버그")]
    [SerializeField] private bool debugLog = false;

    private readonly List<PlayerInput> _disabledInputs = new();
    private readonly List<PlayerInput> _forcedEnableInputs = new();
    private readonly Dictionary<PlayerInput, string> _targetMapByInput = new();

    private GameObject _leftChosen, _rightChosen;
    private string _leftSlot, _rightSlot;

   

    private void Awake()
    {
        TitleButtonMesh.ResetSceneLoadingFlag();
        IsInitialized = false;

        var gs = GameSettings.Instance;
        if (gs == null || leftRoot == null || rightRoot == null)
        {
            Debug.LogWarning("[GSM] 초기 조건 부족(GameSettings/leftRoot/rightRoot).");
            return;
        }

        // 1) 슬롯/ID 결정
        _leftSlot = (gs.gameMode == "1vs1") ? "P1" : "CPU";
        _rightSlot = (gs.gameMode == "1vs1") ? "P2" : "CPU";
        string leftId = gs.GetCharacterForSlot(_leftSlot);
        string rightId = gs.GetCharacterForSlot(_rightSlot);
        bool spawnRight = (gs.gameMode == "1vs1") || cpuModeSpawnRight;
        bool strict = (gs.gameMode == "1vs1");

        // 2) 캐릭터 선택
        _leftChosen = SelectCharacterByIdAndSlot(leftRoot, leftId, _leftSlot, strict);
        _rightChosen = spawnRight ? SelectCharacterByIdAndSlot(rightRoot, rightId, _rightSlot, strict) : null;

        // 3) PlayerInput 바인딩(액션 복제/맵 선택/활성)
        if (_leftChosen)
        {
            var reg = _leftChosen.GetComponentInChildren<PlayerInputRegistrar>(true);
            if (reg) reg.slot = _leftSlot;
            BindPlayerInputToSlot(_leftChosen, _leftSlot, gs);
        }
        if (spawnRight && _rightChosen)
        {
            var reg = _rightChosen.GetComponentInChildren<PlayerInputRegistrar>(true);
            if (reg) reg.slot = _rightSlot;
            BindPlayerInputToSlot(_rightChosen, _rightSlot, gs);
        }

        // 4) 저장된 리바인딩 적용
        gs.LoadKeyBindings();
        TryApplySavedOverridesForSlot(_leftSlot, GetActionsOf(_leftChosen));
        if (spawnRight) TryApplySavedOverridesForSlot(_rightSlot, GetActionsOf(_rightChosen));

        // 5) 즉시 활성 + 액션 재활성 + 맵 스위치
        if (_leftChosen) _leftChosen.SetActive(true);
        if (spawnRight && _rightChosen) _rightChosen.SetActive(true);

        ReenableActions(_leftChosen);
        if (spawnRight) ReenableActions(_rightChosen);

        // 현재 맵을 확실히 스위치(바인딩에서 정한 맵으로)
        var lpi = _leftChosen ? _leftChosen.GetComponentInChildren<PlayerInput>(true) : null;
        var rpi = _rightChosen ? _rightChosen.GetComponentInChildren<PlayerInput>(true) : null;
        if (lpi && !string.IsNullOrEmpty(lpi.defaultActionMap)) lpi.SwitchCurrentActionMap(lpi.defaultActionMap);
        if (rpi && !string.IsNullOrEmpty(rpi.defaultActionMap)) rpi.SwitchCurrentActionMap(rpi.defaultActionMap);

        IsInitialized = true;
    }


    private static InputActionAsset GetActionsOf(GameObject go)
    {
        if (!go) return null;
        var pi = go.GetComponentInChildren<PlayerInput>(true);
        return pi ? pi.actions : null;
    }

    private GameObject SelectCharacterByIdAndSlot(Transform root, string targetId, string targetSlot, bool strict)
    {
        if (root == null || root.childCount == 0) return null;

        Debug.Log($"[GSM] SelectCharacterByIdAndSlot: root={root.name}, targetId='{targetId}', targetSlot='{targetSlot}', strict={strict}");

        GameObject exact = null, idOnly = null, firstActive = null, firstChild = root.GetChild(0).gameObject;

        for (int i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i).gameObject;
            var reg = child.GetComponentInChildren<PlayerInputRegistrar>(true);
            var cid = reg != null ? reg.id : null;
            var cslot = reg != null ? reg.slot : null;

            if (debugLog)
                Debug.Log($"[GSM] Scan '{root.name}' -> '{child.name}' id='{cid}' slot='{cslot}' targetId='{targetId}' targetSlot='{targetSlot}'");

            if (reg != null)
            {
                if (!string.IsNullOrEmpty(targetId) && IdEquals(cid, targetId) &&
                    !string.IsNullOrEmpty(targetSlot) && IdEquals(cslot, targetSlot))
                { exact = child; break; }

                if (idOnly == null && !string.IsNullOrEmpty(targetId) && IdEquals(cid, targetId))
                    idOnly = child;
            }
            if (firstActive == null && child.activeSelf) firstActive = child;
        }

        GameObject result;
        if (strict)
            result = exact;
        else
            result = exact ?? idOnly ?? firstActive ?? firstChild;

        // 선택된 것만 활성화, 나머지 비활성화(결과가 있을 때만)
        if (result != null)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i).gameObject;
                bool active = (child == result);
                if (child.activeSelf != active) child.SetActive(active);
            }
            if (debugLog) Debug.Log($"[GSM] Activated '{result.name}' and deactivated others under '{root.name}'");
        }

        return result;
    }

    // PlayerInput.actions 복제 + 슬롯별 등록 + 맵 자동 선택 + 즉시 PlayerInput 활성
    private void BindPlayerInputToSlot(GameObject root, string slot, GameSettings gs)
    {
        var pi = root.GetComponentInChildren<PlayerInput>(true);
        if (pi == null || pi.actions == null) return;

        bool wasEnabled = pi.enabled;
        if (wasEnabled) pi.enabled = false;

        var cloned = ScriptableObject.Instantiate(pi.actions);
        cloned.name = $"{pi.actions.name}_{slot}_Runtime";
        pi.actions = cloned;

        var mapName = ChooseActionMapForSlot(cloned, slot);
        if (!string.IsNullOrEmpty(mapName))
        {
            pi.defaultActionMap = mapName;   // OnEnable 시 적용
            _targetMapByInput[pi] = mapName; // 이후 Switch 보장
        }

        // GameSettings 등록(프로젝트 로직)
        gs.RegisterActionsForSlot(slot, cloned);

        // 슬롯별 저장된 오버라이드 적용(로컬 JSON)
        TryApplySavedOverridesForSlot(slot, cloned);

        // 즉시 리솔브
        cloned.Disable();
        cloned.Enable();

        // PlayerInput은 미리 켜둠(캐릭터 GO 활성 시 OnEnable 정상 동작)
        pi.enabled = true;

        _disabledInputs.Remove(pi);
        if (!_forcedEnableInputs.Contains(pi))
            _forcedEnableInputs.Add(pi);
    }

    private static void TryApplySavedOverridesForSlot(string slot, InputActionAsset asset)
    {
        if (asset == null) return;

        string[] keys =
        {
            $"InputRebinds_{slot}",
            $"Rebinds_{slot}",
            $"RebindingOverrides_{slot}",
            "InputRebinds",
            "Rebinds",
            "RebindingOverrides"
        };

        foreach (var key in keys)
        {
            if (!PlayerPrefs.HasKey(key)) continue;
            var json = PlayerPrefs.GetString(key);
            if (string.IsNullOrEmpty(json)) continue;

            try
            {
                asset.LoadBindingOverridesFromJson(json);
                break;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GSM] ApplySavedOverrides({key}) 실패: {e.Message}");
            }
        }
    }

    private static void ReenableActions(GameObject root)
    {
        if (!root) return;
        var pi = root.GetComponentInChildren<PlayerInput>(true);
        if (pi != null && pi.actions != null)
        {
            pi.actions.Disable();
            pi.actions.Enable();
        }
    }

    private static bool IdEquals(string a, string b)
        => string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);

    private static string ChooseActionMapForSlot(InputActionAsset asset, string slot)
    {
        if (asset == null) return null;

        var candidates = slot == "P1"
            ? new[] { "Player1", "Player", "P1", "Default" }
            : new[] { "Player2", "Player", "P2", "CPU", "Default" };

        foreach (var name in candidates)
        {
            var map = asset.FindActionMap(name, throwIfNotFound: false);
            if (map != null) return map.name;
        }
        return asset.actionMaps.Count > 0 ? asset.actionMaps[0].name : null;
    }

}