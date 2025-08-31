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

    [Header("Loading UI")]
    [SerializeField] private LoadingOverlay loadingOverlay;
    [SerializeField] private GameObject[] enableAfterInit;

    [Header("Init Locks (초기화 동안 잠금)")]
    [SerializeField] private bool lockTimeAndPhysics = true;
    [SerializeField] private bool disableAllPlayerInputs = true;
    [SerializeField] private bool disableEventSystems = true;

    [Header("1vsCPU 옵션")]
    [Tooltip("1vsCPU 모드에서 왼쪽 캐릭터에 사용할 슬롯 이름(P1/CPU 등)")]
    [SerializeField] private string leftSlotIn1vsCPU = "CPU";
    [Tooltip("1vsCPU 모드에서 오른쪽을 스폰할지 여부(끄면 왼쪽만 스폰)")]
    [SerializeField] private bool cpuModeSpawnRight = false;

    [Header("디버그")]
    [SerializeField] private bool debugLog = false;

    private readonly List<PlayerInput> _disabledInputs = new();
    private readonly List<EventSystem> _disabledEventSystems = new();

    private readonly List<PlayerInput> _forcedEnableInputs = new();
    private readonly Dictionary<PlayerInput, string> _targetMapByInput = new();

    private float _prevTimeScale = 1f;
    private SimulationMode _prevSimulationMode = SimulationMode.FixedUpdate;
    private bool _lockedTime = false;

    private void Awake()
    {
        // 새 씬 진입: 씬 로딩 플래그 리셋(이제 TitleButtonMesh가 파괴되었어도 static은 유지됨)
        TitleButtonMesh.ResetSceneLoadingFlag();


        IsInitialized = false;

        // 0) 로딩 오버레이는 바로 보이게(타임락 전에)
        ShowOverlayImmediate();

        // 1) 이후에 켤 것들 비활성
        if (enableAfterInit != null)
            foreach (var go in enableAfterInit) if (go) go.SetActive(false);

        // 2) 시간/물리/오디오 잠금
        if (lockTimeAndPhysics)
        {
            _prevTimeScale = Time.timeScale;
            _prevSimulationMode = Physics.simulationMode;

            Time.timeScale = 0f;
            Physics.simulationMode = SimulationMode.Script; // 물리 일시정지
            AudioListener.pause = true;
            _lockedTime = true;
        }

        // 3) EventSystem/PlayerInput 비활성
        var roots = SceneManager.GetActiveScene().GetRootGameObjects();

        if (disableEventSystems)
        {
            for (int i = 0; i < roots.Length; i++)
            {
                var evs = roots[i].GetComponentsInChildren<EventSystem>(true);
                foreach (var es in evs)
                {
                    if (es != null && es.enabled)
                    {
                        es.enabled = false;
                        _disabledEventSystems.Add(es);
                    }
                }
            }
        }

        if (disableAllPlayerInputs)
        {
            for (int i = 0; i < roots.Length; i++)
            {
                var inputs = roots[i].GetComponentsInChildren<PlayerInput>(true);
                foreach (var pi in inputs)
                {
                    if (pi != null && pi.enabled)
                    {
                        pi.enabled = false;
                        _disabledInputs.Add(pi);
                    }
                }
            }
        }

        // 4) 캐릭터는 Start 전에 전부 꺼서 Movement.Start 선실행 방지
        DeactivateAllChildren(leftRoot);
        DeactivateAllChildren(rightRoot);
    }

    private System.Collections.IEnumerator Start()
    {
        // 씬 오브젝트 초기화 보장
        yield return null;

        var gs = GameSettings.Instance;
        bool canProceed = (gs != null && leftRoot != null && rightRoot != null);

        GameObject leftChosen = null, rightChosen = null;

        if (!canProceed)
        {
            Debug.LogWarning("GameSceneManager: 초기 조건 부족(GameSettings/leftRoot/rightRoot).");
        }
        else
        {
           // 슬롯/ID 결정
            string leftSlot, rightSlot;
            if (gs.gameMode == "1vs1")
            {
                leftSlot = "P1";
                rightSlot = "P2";
            }
            else
            {
                leftSlot = "CPU";
                rightSlot = "CPU";
            }

            string leftId = gs.GetCharacterForSlot(leftSlot);
            string rightId = gs.GetCharacterForSlot(rightSlot);


            bool spawnRight = (gs.gameMode == "1vs1") || cpuModeSpawnRight;

            // 선택 로직: 1vs1은 strict=true, 1vsCPU는 strict=false(id만 맞아도 선택)
            bool strict = (gs.gameMode == "1vs1");
            leftChosen  = SelectCharacterByIdAndSlot(leftRoot,  leftId,  leftSlot,  strict);
            rightChosen = spawnRight ? SelectCharacterByIdAndSlot(rightRoot, rightId, rightSlot, strict) : null;

            if (debugLog)
                Debug.Log($"[GSM] mode={gs.gameMode}, leftSlot={leftSlot}, rightSlot={rightSlot}, leftId={leftId}, rightId={rightId}, spawnRight={spawnRight}");

            // 슬롯 기록 + 바인딩
            if (leftChosen != null)
            {
                var reg = leftChosen.GetComponentInChildren<PlayerInputRegistrar>(true);
                if (reg != null) reg.slot = leftSlot;
                BindPlayerInputToSlot(leftChosen, leftSlot, gs);
            }
            if (spawnRight && rightChosen != null)
            {
                var reg = rightChosen.GetComponentInChildren<PlayerInputRegistrar>(true);
                if (reg != null) reg.slot = rightSlot;
                BindPlayerInputToSlot(rightChosen, rightSlot, gs);
            }

            // 저장된 리바인딩 적용(프로젝트/로컬 JSON)
            gs.LoadKeyBindings();
            TryApplySavedOverridesForSlot(leftSlot, GetActionsOf(leftChosen));
            if (spawnRight) TryApplySavedOverridesForSlot(rightSlot, GetActionsOf(rightChosen));

            // 안정화 프레임
            yield return null;

            // 선택된 캐릭터만 활성
            if (leftChosen) leftChosen.SetActive(true);
            if (spawnRight && rightChosen) rightChosen.SetActive(true);

            // 액션 재활성 + 최종 오버라이드 재적용
            ReenableActions(leftChosen);
            if (spawnRight) ReenableActions(rightChosen);
            TryApplySavedOverridesForSlot(leftSlot, GetActionsOf(leftChosen));
            if (spawnRight) TryApplySavedOverridesForSlot(rightSlot, GetActionsOf(rightChosen));

            if (debugLog)
            {
                DumpInputState(leftSlot, leftChosen);
                if (spawnRight) DumpInputState(rightSlot, rightChosen);
            }
        }

        // 완료
        IsInitialized = true;

        // 로딩창 닫기(오버레이가 스케일드타임을 쓰면 Unscaled로 처리하도록 내부가 구현돼 있어야 합니다)
        if (loadingOverlay != null)
            yield return loadingOverlay.FadeOut(2f);

        // 잠금 해제
        foreach (var es in _disabledEventSystems) if (es) es.enabled = true;
        _disabledEventSystems.Clear();

        foreach (var pi in _disabledInputs) if (pi) pi.enabled = true;
        _disabledInputs.Clear();

        foreach (var pi in _forcedEnableInputs)
        {
            if (!pi) continue;
            if (!pi.enabled) pi.enabled = true;

            if (_targetMapByInput.TryGetValue(pi, out var mapName) && !string.IsNullOrEmpty(mapName))
            {
                var map = pi.actions?.FindActionMap(mapName, throwIfNotFound: false);
                if (map != null) pi.SwitchCurrentActionMap(mapName);
            }
        }
        _forcedEnableInputs.Clear();
        _targetMapByInput.Clear();

        if (_lockedTime)
        {
            Time.timeScale = _prevTimeScale;
            Physics.simulationMode = _prevSimulationMode;
            AudioListener.pause = false;
            _lockedTime = false;
        }

        if (enableAfterInit != null)
            foreach (var go in enableAfterInit) if (go) go.SetActive(true);
    }

    // ---- Helpers ----

    private void ShowOverlayImmediate()
    {
        if (!loadingOverlay) return;
        var go = loadingOverlay.gameObject;
        go.SetActive(true);
        // 즉시 보이도록 CanvasGroup 보강
        var cg = go.GetComponentInChildren<CanvasGroup>(true);
        if (cg != null) cg.alpha = 1f;
    }

    private static InputActionAsset GetActionsOf(GameObject go)
    {
        if (!go) return null;
        var pi = go.GetComponentInChildren<PlayerInput>(true);
        return pi ? pi.actions : null;
    }

    private void DeactivateAllChildren(Transform root)
    {
        if (!root) return;
        for (int i = 0; i < root.childCount; i++)
            root.GetChild(i).gameObject.SetActive(false);
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

        // 엄격: 둘 다 일치하지 않으면 null 반환(확실한 검증)
        if (strict) return exact;

        // 비엄격 폴백
        return exact ?? idOnly ?? firstActive ?? firstChild;
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

    private void DumpInputState(string slot, GameObject go)
    {
        if (!debugLog || !go) return;
        var pi = go.GetComponentInChildren<PlayerInput>(true);
        if (pi == null || pi.actions == null) { Debug.Log($"[GSM] {slot}: PlayerInput 없음"); return; }
        var currentMap = pi.currentActionMap != null ? pi.currentActionMap.name : "(null)";
        var defaultMap = string.IsNullOrEmpty(pi.defaultActionMap) ? "(null)" : pi.defaultActionMap;
        Debug.Log($"[GSM] {slot}: enabled={pi.enabled}, defaultMap={defaultMap}, currentMap={currentMap}, maps={pi.actions.actionMaps.Count}");
    }
}