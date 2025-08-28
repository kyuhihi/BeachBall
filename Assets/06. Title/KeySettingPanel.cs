using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

[System.Serializable]
public class KeyButtonInfo
{
    public Button button;
    public Image keyImage;
    public string keyPath;      // 예: "<Keyboard>/c"
    public string actionName;   // 예: "MoveRight"
    public int bindingIndex;
    public int playerIndex;     // 1: Player1, 2: Player2, CPU: 1
}

public class KeySettingPanel : MonoBehaviour
{
    public GameObject setting1vs1;
    public GameObject setting1vsCPU;
    public InputActionAsset inputActions1vs1_Player1;
    public InputActionAsset inputActions1vs1_Player2;
    public InputActionAsset inputActions1vsCPU;

    private InputActionAsset[] currentInputActions;
    public KeyButtonInfo[] keyButtonInfos1vs1;
    public KeyButtonInfo[] keyButtonInfosCPU;

    public Color player1Color = Color.blue;
    public Color player2Color = new Color(1f, 0.5f, 0f);
    public Color cpuColor = Color.black;
    public Color normalColor = Color.white;

    public Transform keyParentP1;
    public Transform keyParentP2;
    public Transform keyParentCPU;

    private static string GetBindingPath(InputBinding b)
    {
        return string.IsNullOrEmpty(b.effectivePath) ? b.path : b.effectivePath;
    }

    public void OnSet1vs1Button() => SetMode("1vs1");
    public void OnSet1vsCPUButton() => SetMode("1vsCPU");

    public void SetMode(string mode)
    {
        if (mode == "1vs1")
        {
            currentInputActions = new InputActionAsset[] { inputActions1vs1_Player1, inputActions1vs1_Player2 };
            if (setting1vs1) setting1vs1.SetActive(true);
            if (setting1vsCPU) setting1vsCPU.SetActive(false);

            // 에디터 자산도 슬롯으로 등록해 두어 오버라이드가 UI에도 반영되도록 함
            GameSettings.Instance?.RegisterActionsForSlot("P1", inputActions1vs1_Player1);
            GameSettings.Instance?.RegisterActionsForSlot("P2", inputActions1vs1_Player2);
        }
        else // 1vsCPU
        {
            currentInputActions = new InputActionAsset[] { inputActions1vsCPU };
            if (setting1vs1) setting1vs1.SetActive(false);
            if (setting1vsCPU) setting1vsCPU.SetActive(true);

            GameSettings.Instance?.RegisterActionsForSlot("CPU", inputActions1vsCPU);
        }

        GameSettings.Instance?.LoadKeyBindings();
        MatchKeyInfosWithBindings();
        UpdateKeyColors();
        EnableAllActions();
    }

    void Start()
    {
        SetMode("1vs1");

        for (int i = 0; i < keyButtonInfos1vs1.Length; i++)
        {
            int idx = i;
            keyButtonInfos1vs1[i].button.onClick.AddListener(() => OnKeyButtonClicked(idx));
        }
        for (int i = 0; i < keyButtonInfosCPU.Length; i++)
        {
            int idx = i;
            keyButtonInfosCPU[i].button.onClick.AddListener(() => OnKeyButtonClickedCPU(idx));
        }

        GameSettings.Instance?.LoadKeyBindings();
        MatchKeyInfosWithBindings();
        UpdateKeyColors();
        EnableAllActions();
    }

    private void EnableAllActions()
    {
        if (currentInputActions == null) return;
        foreach (var asset in currentInputActions)
        {
            if (asset == null) continue;
            foreach (var action in asset) action.Enable();
        }
    }

    // UI 표시에 사용할 에디터 자산 기준 매칭 (effectivePath)
    void MatchKeyInfosWithBindings()
    {
        if (currentInputActions == null) return;

        if (currentInputActions.Length == 2)
        {
            for (int p = 0; p < 2; p++)
            {
                var actionMap = currentInputActions[p];
                foreach (var action in actionMap)
                {
                    for (int i = 0; i < action.bindings.Count; i++)
                    {
                        var binding = action.bindings[i];
                        string path = GetBindingPath(binding);
                        foreach (var info in keyButtonInfos1vs1)
                        {
                            if (info.keyPath == path)
                            {
                                info.actionName = action.name;
                                info.bindingIndex = i;
                                info.playerIndex = p + 1;
                            }
                        }
                    }
                }
            }
        }
        else if (currentInputActions.Length == 1)
        {
            var actionMap = currentInputActions[0];
            foreach (var action in actionMap)
            {
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    var binding = action.bindings[i];
                    string path = GetBindingPath(binding);
                    foreach (var info in keyButtonInfosCPU)
                    {
                        if (info.keyPath == path)
                        {
                            info.actionName = action.name;
                            info.bindingIndex = i;
                            info.playerIndex = 1;
                        }
                    }
                }
            }
        }
    }

    void UpdateKeyColors()
    {
        if (currentInputActions == null) return;

        if (currentInputActions.Length == 2)
        {
            foreach (var info in keyButtonInfos1vs1)
            {
                int matchedPlayer = -1;
                string display = "-";
                string matchedAction = "";
                int matchedBindingIndex = 0;

                for (int p = 0; p < 2; p++)
                {
                    var actionMap = currentInputActions[p];
                    foreach (var action in actionMap)
                    {
                        for (int i = 0; i < action.bindings.Count; i++)
                        {
                            var binding = action.bindings[i];
                            if (GetBindingPath(binding) == info.keyPath)
                            {
                                matchedPlayer = p;
                                matchedAction = action.name;
                                matchedBindingIndex = i;
                                display = binding.ToDisplayString();
                                break;
                            }
                        }
                        if (matchedPlayer != -1) break;
                    }
                    if (matchedPlayer != -1) break;
                }

                info.playerIndex = matchedPlayer != -1 ? matchedPlayer + 1 : 0;
                info.actionName = matchedAction;
                info.bindingIndex = matchedBindingIndex;

                var keyLabel = info.button.GetComponentInChildren<TMP_Text>();
                if (keyLabel) keyLabel.text = display;

                if (info.keyImage)
                {
                    if (matchedPlayer == 0) info.keyImage.color = player1Color;
                    else if (matchedPlayer == 1) info.keyImage.color = player2Color;
                    else info.keyImage.color = normalColor;
                }
            }
        }

        if (currentInputActions.Length == 1)
        {
            var actionMap = currentInputActions[0];
            foreach (var info in keyButtonInfosCPU)
            {
                bool isMatched = false;
                string display = "-";
                string matchedAction = "";
                int matchedBindingIndex = 0;

                foreach (var action in actionMap)
                {
                    for (int i = 0; i < action.bindings.Count; i++)
                    {
                        var binding = action.bindings[i];
                        if (GetBindingPath(binding) == info.keyPath)
                        {
                            isMatched = true;
                            matchedAction = action.name;
                            matchedBindingIndex = i;
                            display = binding.ToDisplayString();
                            break;
                        }
                    }
                    if (isMatched) break;
                }

                info.playerIndex = isMatched ? 1 : 0;
                info.actionName = matchedAction;
                info.bindingIndex = matchedBindingIndex;

                var keyLabel = info.button.GetComponentInChildren<TMP_Text>();
                if (keyLabel) keyLabel.text = display;

                if (info.keyImage) info.keyImage.color = isMatched ? cpuColor : normalColor;
            }
        }
    }

    // 해당 KeyButtonInfo가 가리키는 슬롯의 '런타임' 에셋 가져오기 (없으면 에디터 자산 fallback)
    private InputActionAsset GetRuntimeAssetForInfo(KeyButtonInfo info)
    {
        string slot;
        if (currentInputActions != null && currentInputActions.Length == 1)
            slot = "CPU";
        else
            slot = info.playerIndex == 1 ? "P1" : info.playerIndex == 2 ? "P2" : null;

        var runtime = GameSettings.Instance?.GetFirstAssetInSlot(slot);
        if (runtime != null) return runtime;

        // fallback: 에디터 자산
        int idx = Mathf.Clamp(info.playerIndex - 1, 0, (currentInputActions?.Length ?? 1) - 1);
        return (currentInputActions != null && currentInputActions.Length > idx) ? currentInputActions[idx] : null;
    }

    // 런타임 에셋 기준으로 액션/바인딩 인덱스 찾기
    private bool ResolveRuntimeActionForInfo(KeyButtonInfo info, out InputAction action, out int bindingIndex)
    {
        action = null;
        bindingIndex = -1;

        var asset = GetRuntimeAssetForInfo(info);
        if (asset == null) return false;

        if (!string.IsNullOrEmpty(info.actionName))
            action = asset.FindAction(info.actionName);

        if (action != null)
        {
            // keyPath로 해당 바인딩 인덱스 재계산
            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (GetBindingPath(action.bindings[i]) == info.keyPath)
                {
                    bindingIndex = i;
                    return true;
                }
            }
            // 실패 시 기존 인덱스 fallback
            if (info.bindingIndex >= 0 && info.bindingIndex < action.bindings.Count)
            {
                bindingIndex = info.bindingIndex;
                return true;
            }
            return true; // 액션만 찾았음
        }

        // 액션 이름이 없으면 에셋 전체에서 keyPath로 역탐색
        foreach (var act in asset)
        {
            for (int i = 0; i < act.bindings.Count; i++)
            {
                if (GetBindingPath(act.bindings[i]) == info.keyPath)
                {
                    action = act;
                    bindingIndex = i;
                    return true;
                }
            }
        }
        return false;
    }

    void OnKeyButtonClicked(int idx)
    {
        var info = keyButtonInfos1vs1[idx];

        if (!ResolveRuntimeActionForInfo(info, out var action, out var bindIndex))
        {
            Debug.LogWarning($"리바인딩 실패: 런타임 액션을 찾을 수 없음. keyPath={info.keyPath}, actionName={info.actionName}, playerIndex={info.playerIndex}");
            return;
        }

        action.Disable();
        action.PerformInteractiveRebinding()
              .WithTargetBinding(bindIndex)
              .WithCancelingThrough("<Mouse>/rightButton")
              .OnCancel(op =>
              {
                  action.Enable();
                  op.Dispose();
              })
              .OnComplete(op =>
              {
                  op.Dispose();
                  action.Enable();

                  var asset = action.actionMap?.asset;
                  GameSettings.Instance?.SaveAndBroadcastOverrides(asset); // 같은 슬롯만 적용

                  MatchKeyInfosWithBindings();
                  UpdateKeyColors();
              })
              .Start();
    }

    void OnKeyButtonClickedCPU(int idx)
    {
        var info = keyButtonInfosCPU[idx];

        if (!ResolveRuntimeActionForInfo(info, out var action, out var bindIndex))
        {
            Debug.LogWarning($"리바인딩 실패(CPU): 런타임 액션을 찾을 수 없음. keyPath={info.keyPath}, actionName={info.actionName}");
            return;
        }

        action.Disable();
        action.PerformInteractiveRebinding()
              .WithTargetBinding(bindIndex)
              .WithCancelingThrough("<Mouse>/rightButton")
              .OnCancel(op =>
              {
                  action.Enable();
                  op.Dispose();
              })
              .OnComplete(op =>
              {
                  op.Dispose();
                  action.Enable();

                  var asset = action.actionMap?.asset;
                  GameSettings.Instance?.SaveAndBroadcastOverrides(asset);

                  MatchKeyInfosWithBindings();
                  UpdateKeyColors();
              })
              .Start();
    }
}