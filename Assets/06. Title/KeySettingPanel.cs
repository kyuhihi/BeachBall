using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections; // 추가
using System;

[System.Serializable]
public class KeyButtonInfo
{
    public Button button;
    public Image keyImage;
    public string keyPath;      // 예: "<Keyboard>/c"
    public string actionName;   // 예: "MoveRight"
    public int bindingIndex;
    public int playerIndex;     // 1: Player1, 2: Player2, CPU: 3
}

[System.Serializable]
public class KeyBlockRule
{
    public string layoutPath;   // 예: "<Keyboard>/escape"
    [TextArea] public string message; // 예: "ESC 키는 메뉴 전용이라 사용할 수 없습니다."
}
public class KeySettingPanel : MonoBehaviour
{
    [Header("Block Keys")]
    public KeyBlockRule[] blockRules;
    public GameObject setting1vs1;
    public GameObject setting1vsCPU;

    [Header("Fail Popup")]
    public GameObject failurePopup;
    public TMP_Text failureText;
    public float failureAutoHideSeconds = 2.5f;

    // 진행 중인 리바인딩 오퍼레이션 추적
    private InputActionRebindingExtensions.RebindingOperation _currentRebindOp;



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

    [Header("Action Summary (밑에 표시)")]
    public TMP_Text actionsSummaryLeft;   // P1 또는 CPU 요약 표시용
    public TMP_Text actionsSummaryRight;  // P2 요약 표시용
    [Tooltip("{key} : {action} 형식 지정")]
    public string actionLineFormat = "{key} : {action}";
    [Tooltip("줄 구분자")]
    public string actionLineSeparator = "\n";
    [Tooltip("한 줄에 표시할 항목 수")]
    [Min(1)] public int actionsPerRow = 3;
    [Tooltip("같은 줄에서 항목 사이 구분자")]
    public string columnSeparator = "    ";

    [SerializeField]
    private string[] summaryOrder = new[]
    {
        "MoveLeft","MoveUp","MoveDown","MoveRight", // 이동
        "Smash","Jump","Dash",                      // 이동계 스킬
        "AttackSkill","DefenceSkill","UltimateSkill"// 공격/방어/궁극기
    };

    [SerializeField] private string summaryHeaderLeft = "P1";
    [SerializeField] private string summaryHeaderRight = "P2";

    private bool IsBlockedKey(string layoutPath, out string msg)
    {
        // 인스펙터 규칙 우선
        if (!string.IsNullOrEmpty(layoutPath) && blockRules != null)
        {
            foreach (var r in blockRules)
            {
                if (r != null && r.layoutPath == layoutPath)
                {
                    msg = string.IsNullOrEmpty(r.message) ? "해당 키는 사용할 수 없습니다." : r.message;
                    return true;
                }
            }
        }

        // 예시: ESC 기본 차단(원치 않으면 제거)
        if (layoutPath == "<Keyboard>/escape")
        {
            msg = "ESC 키는 위험할 수 있으니 막아놓을게. 고맙지?";
            return true;
        }
        else if (layoutPath == "<Keyboard>/f1")
        {
            msg = "F1 누르면 도움말 창이 뜨더라고.. 그러니까 안돼";
            return true;
        }
        else if (layoutPath == "<Keyboard>/f3")
        {
            msg = "F3! 눌러보니! 찾기가 떠! 찾기는 Ctrl+F아닌가?";
            return true;
        }
        else if (layoutPath == "<Keyboard>/f5")
        {
            msg = "F5! F5! F5! .. 그만 재시작해!!";
            return true;
        }
        else if (layoutPath == "<Keyboard>/f6")
        {
            msg = "F6은 잘 모르겠는데 뭔가 Tab키랑 비슷한 역할을 하는 것 같아.";
            return true;
        }
        else if (layoutPath == "<Keyboard>/f7")
        {
            msg = "커서/캐럿 브라우징..? 이게 뭔데";
            return true;
        }
        else if (layoutPath == "<Keyboard>/f9")
        {
            msg = "f9는 무려 중단점! 뭐..? 게임이랑 무슨 상관이냐고..? 게임도 코드야!!";
            return true;
        }
        else if (layoutPath == "<Keyboard>/f10")
        {
            msg = "f10도 뭔가 Tab키랑 비슷한 역할을 하는 것 같아. Tab은 만능인가?";
            return true;
        }
        else if (layoutPath == "<Keyboard>/f11")
        {
            msg = "f11은 무려무려 전체화면 전환! 안심해!";
            return true;
        }
        else if (layoutPath == "<Keyboard>/f12")
        {
            msg = "f12는 무서워.. 뭔가 코드들이 튀어나와 ....그게 개발자 도구라니..";
            return true;
        }
        else if (layoutPath == "<Keyboard>/f12")
        {
            msg = "f12는 무서워.. 뭔가 코드들이 튀어나와 ....그게 개발자 도구라니..";
            return true;
        }
        else if (layoutPath == "<Keyboard>/printScreen")
        {
            msg = "너 설마.. 입력 할때마다 캡처하려는 거야? 고맙지만 안돼!";
            return true;
        }
        else if (layoutPath == "<Keyboard>/scrollLock")
        {
            msg = "이건 무슨 키였더라..? 일단 막아놓을게! 내 노트북엔 이 키가 없어서 잘 모르겠어.";
            return true;
        }
        else if (layoutPath == "<Keyboard>/pause")
        {
            msg = "이 게임에서 pause 키는 esc키야! 이 친구는 휴가갔어";
            return true;
        }
        else if (layoutPath == "<Keyboard>/backspace")
        {
            msg = "이 키를 누르면 글자가 지워지잖아! 안돼! 너 설마 움직일때마다 글자를 지우려는 거야? 무서워!";
            return true;
        }
        else if (layoutPath == "<Keyboard>/tab")
        {
            msg = "Tab은 만능이지.. 허나.. 여기선 아니야.";
            return true;
        }
        else if (layoutPath == "<Keyboard>/backslash")
        {
            msg = "이 키는 C 공부 처음에 가장 헷갈리는 키야! 누구는 '원'으로 나오거든! 대체 \\는 어디간거야!";
            return true;
        }
        else if (layoutPath == "<Keyboard>/capsLock")
        {
            msg = "이 키를 누른다고 키가 커지진 않는다네";
            return true;
        }
        else if (layoutPath == "<Keyboard>/enter")
        {
            msg = "XxxXxxxx 탁.  아. 메일 보내버렸다.";
            return true;
        }

        else if (layoutPath == "<Keyboard>/insert")
        {
            msg = "크아악 내 단어들이 밀려나고 있어!!";
            return true;
        }
        else if (layoutPath == "<Keyboard>/delete")
        {
            msg = "아. 모르고 지워버렸다.";
            return true;
        }
        else if (layoutPath == "<Keyboard>/home")
        {
            msg = "하지만 이미 집인걸~ ..뭐 집이 아니라고? 힘내 친구!";
            return true;
        }
        else if (layoutPath == "<Keyboard>/end")
        {
            msg = "아직이다! 아직이야!";
            return true;
        }
        else if (layoutPath == "<Keyboard>/pageUp")
        {
            msg = "이거 Drag보다 편한가? 안써봐서 모르겠어";
            return true;
        }
        else if (layoutPath == "<Keyboard>/pageDown")
        {
            msg = "이거 Drag보다 편한가? 안써봐서 모르겠어";
            return true;
        }
        else if (layoutPath == "<Keyboard>/leftMeta")
        {
            msg = "내 노트북의 window키를 누른건데.. window키가 아니야..?";
            return true;
        }
        else if (layoutPath == "<Keyboard>/leftWindows")
        {
            msg = "마지막 버ㅌ...";
            return true;
        }
        else if (layoutPath == "<Keyboard>/rightWindows")
        {
            msg = "마지막 버ㅌ...";
            return true;
        }
        else if (layoutPath == "<Keyboard>/contextMenu")
        {
            msg = "이건 대체 무슨 키야..? ....오른쪽 클릭이랑 똑같아! 안돼!";
            return true;
        }
        else if (layoutPath == "<Keyboard>/numLock")
        {
            msg = "window만 넣으면 서운할까봐.. 하하.. 사실.. numlock 이미지가 안보이더라..";
            return true;
        }

        msg = null;
        return false;
    }
    private string GetActiveSlotForInfo(KeyButtonInfo info)
    {
        if (currentInputActions == null) return null;

        if (currentInputActions.Length == 1)
            return "CPU"; // 1vsCPU 모드

        // 1vs1 모드
        if (info.playerIndex == 1) return "P1";
        if (info.playerIndex == 2) return "P2";
        return null;
    }

    // 현재 보이는 패널 + 같은 슬롯 내에서만 중복 검사
    private bool IsTargetKeyFreeInActivePanel(string layoutPath, string slot, KeyButtonInfo owner)
    {
        if (string.IsNullOrEmpty(layoutPath)) return false;

        KeyButtonInfo[] list =
            (setting1vs1 != null && setting1vs1.activeSelf) ? keyButtonInfos1vs1 :
            (setting1vsCPU != null && setting1vsCPU.activeSelf) ? keyButtonInfosCPU : null;

        if (list == null) return true;

        bool is1vs1Active = setting1vs1 != null && setting1vs1.activeSelf;

        foreach (var info in list)
        {
            if (info == null || info == owner) continue;

            var infoSlot = GetActiveSlotForInfo(info);

            // 1vsCPU: 같은 슬롯만 검사, 1vs1: 슬롯 구분 없이 양쪽 다 검사
            if (!is1vs1Active && !string.Equals(infoSlot, slot, StringComparison.Ordinal))
                continue;

            if (ResolveRuntimeActionForInfo(info, out var act, out var bindIndex))
            {
                if (act != null && bindIndex >= 0 && bindIndex < act.bindings.Count)
                {
                    var b = act.bindings[bindIndex];
                    var path = string.IsNullOrEmpty(b.effectivePath) ? b.path : b.effectivePath;
                    if (string.Equals(path, layoutPath, StringComparison.Ordinal))
                        return false; // 이미 사용 중
                }
            }
        }
        return true;
    }

    private static string GetBindingPath(InputBinding b)
    {
        return string.IsNullOrEmpty(b.effectivePath) ? b.path : b.effectivePath;
    }

    // 선택된 컨트롤을 "<DeviceLayout>/controlName" 형식으로 변환
    private static string ToLayoutPath(InputControl control)
    {
        if (control == null) return null;
        var deviceLayout = control.device?.layout;   // 예: "Keyboard"
        var controlName = control.name;              // 예: "w"
        if (string.IsNullOrEmpty(deviceLayout) || string.IsNullOrEmpty(controlName))
            return null;
        return $"<{deviceLayout}>/{controlName}";
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
        UpdateActionLists();
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
        UpdateActionLists();
    }

    // 닫기 버튼에서 호출
    public void OnClickClosePanelButton()
    {
        CancelCurrentRebindIfAny();
        gameObject.SetActive(false);
    }

    private void CancelCurrentRebindIfAny()
    {
        if (_currentRebindOp != null)
        {
            _currentRebindOp.Cancel();
            _currentRebindOp.Dispose();
            _currentRebindOp = null;
        }
    }

    void OnDisable()
    {
        // 패널이 비활성화될 때도 리바인딩 취소
        CancelCurrentRebindIfAny();
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
                            info.playerIndex = 3;
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


    private KeyButtonInfo FindKeyInfoByPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        if (keyButtonInfos1vs1 != null)
        {
            foreach (var info in keyButtonInfos1vs1)
                if (info != null && info.keyPath == path)
                    return info;
        }
        if (keyButtonInfosCPU != null)
        {
            foreach (var info in keyButtonInfosCPU)
                if (info != null && info.keyPath == path)
                    return info;
        }
        return null;
    }

    private void SetAllKeyButtonsInteractable(bool value)
    {
        if (keyButtonInfos1vs1 != null)
            foreach (var info in keyButtonInfos1vs1)
                if (info?.button) info.button.interactable = value;

        if (keyButtonInfosCPU != null)
            foreach (var info in keyButtonInfosCPU)
                if (info?.button) info.button.interactable = value;
    }

    private bool IsTargetKeyFree(string layoutPath)
    {
        var info = FindKeyInfoByPath(layoutPath);
        if (info == null) return true;
        return info.playerIndex == 0;        // 0(비어있음)만 허용
    }

    void OnKeyButtonClicked(int idx)
    {
        var info = keyButtonInfos1vs1[idx];

        if (info.playerIndex <= 0)
        {
            ShowFail("이 키는 현재 매핑되지 않았습니다.");
            return;
        }
        if (!ResolveRuntimeActionForInfo(info, out var action, out var bindIndex))
        {
            ShowFail("리바인딩 실패: 액션을 찾을 수 없습니다.");
            return;
        }

        var slot = GetActiveSlotForInfo(info); // "P1" 또는 "P2"

        CancelCurrentRebindIfAny();
        SetAllKeyButtonsInteractable(false);

        action.Disable();
        var op = action.PerformInteractiveRebinding()
            .WithTargetBinding(bindIndex)
            .WithCancelingThrough("<Mouse>/rightButton")
            .WithControlsExcluding("<Mouse>")
            .WithControlsExcluding("<Gamepad>")
            .OnPotentialMatch(p =>
            {
                var layoutPath = ToLayoutPath(p.selectedControl);

                if (!string.IsNullOrEmpty(layoutPath) && IsBlockedKey(layoutPath, out var blockMsg))
                {
                    ShowFail(blockMsg);
                    p.Cancel();
                    return;
                }

                // 현재 보이는 패널 + 같은 슬롯에서만 중복 체크
                if (string.IsNullOrEmpty(layoutPath) || !IsTargetKeyFreeInActivePanel(layoutPath, slot, owner: info))
                {
                    ShowFail(string.IsNullOrEmpty(layoutPath) ? "알 수 없는 키입니다." : "이미 같은 슬롯에서 사용 중인 키입니다.");
                    p.Cancel();
                }
            })
            .OnCancel(p =>
            {
                action.Enable();
                p.Dispose();
                if (_currentRebindOp == p) _currentRebindOp = null;

                SetAllKeyButtonsInteractable(true);
                UpdateKeyColors();
                UpdateActionLists(); // 추가
            })
            .OnComplete(p =>
            {
                p.Dispose();
                action.Enable();
                if (_currentRebindOp == p) _currentRebindOp = null;

                var asset = action.actionMap?.asset;
                GameSettings.Instance?.SaveAndBroadcastOverrides(asset);

                SetAllKeyButtonsInteractable(true);
                MatchKeyInfosWithBindings();
                UpdateKeyColors();
                UpdateActionLists(); // 추가
            });
        _currentRebindOp = op;
        op.Start();

    }



    void OnKeyButtonClickedCPU(int idx)
    {
        var info = keyButtonInfosCPU[idx];

        if (info.playerIndex <= 0)
        {
            ShowFail("이 키는 현재 매핑되지 않았습니다.");
            return;
        }
        if (!ResolveRuntimeActionForInfo(info, out var action, out var bindIndex))
        {
            ShowFail("리바인딩 실패: 액션을 찾을 수 없습니다.");
            return;
        }

        const string slot = "CPU";

        CancelCurrentRebindIfAny();
        SetAllKeyButtonsInteractable(false);

        action.Disable();
        var op = action.PerformInteractiveRebinding()
            .WithTargetBinding(bindIndex)
            .WithCancelingThrough("<Mouse>/rightButton")
            .WithControlsExcluding("<Mouse>")
            .WithControlsExcluding("<Gamepad>")
            .OnPotentialMatch(p =>
            {
                var layoutPath = ToLayoutPath(p.selectedControl);

                if (!string.IsNullOrEmpty(layoutPath) && IsBlockedKey(layoutPath, out var blockMsg))
                {
                    ShowFail(blockMsg);
                    p.Cancel();
                    return;
                }

                // 현재 패널(1vsCPU) 내에서만 중복 체크
                if (string.IsNullOrEmpty(layoutPath) || !IsTargetKeyFreeInActivePanel(layoutPath, slot, owner: info))
                {
                    ShowFail(string.IsNullOrEmpty(layoutPath) ? "알 수 없는 키입니다." : "이미 같은 슬롯에서 사용 중인 키입니다.");
                    p.Cancel();
                }
            })
            .OnCancel(p =>
            {
                action.Enable();
                p.Dispose();
                if (_currentRebindOp == p) _currentRebindOp = null;

                SetAllKeyButtonsInteractable(true);
                UpdateKeyColors();
                UpdateActionLists(); // 추가
            })
            .OnComplete(p =>
            {
                p.Dispose();
                action.Enable();
                if (_currentRebindOp == p) _currentRebindOp = null;

                var asset = action.actionMap?.asset;
                GameSettings.Instance?.SaveAndBroadcastOverrides(asset);

                SetAllKeyButtonsInteractable(true);
                MatchKeyInfosWithBindings();
                UpdateKeyColors();
                UpdateActionLists(); // 추가
            });
        _currentRebindOp = op;
        op.Start();
    }

    private void ShowFail(string message)
    {
        if (failurePopup == null || failureText == null)
        {
            Debug.LogWarning(message);
            return;
        }
        failureText.text = message;
        failurePopup.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(HideFailPopupAfterDelay());
    }

    private IEnumerator HideFailPopupAfterDelay()
    {
        yield return new WaitForSeconds(failureAutoHideSeconds);
        if (failurePopup) failurePopup.SetActive(false);
    }

    private void UpdateActionLists()
    {
        if (actionsSummaryLeft == null && actionsSummaryRight == null) return;
        if (currentInputActions == null) { ClearActionLists(); return; }

        var rowSep = NormalizeSeparator(actionLineSeparator);
        string headerLeft = summaryHeaderLeft;
        string headerRight = summaryHeaderRight;

        if (currentInputActions.Length == 2)
        {
            // 1vs1: 왼쪽=P1, 오른쪽=P2
            var leftBody = BuildSummaryFor1v1Player(1);
            var rightBody = BuildSummaryFor1v1Player(2);

            if (actionsSummaryLeft)
                actionsSummaryLeft.text = string.IsNullOrEmpty(leftBody) ? headerLeft : $"{headerLeft}{rowSep}{leftBody}";

            if (actionsSummaryRight)
            {
                actionsSummaryRight.gameObject.SetActive(true);
                actionsSummaryRight.text = string.IsNullOrEmpty(rightBody) ? headerRight : $"{headerRight}{rowSep}{rightBody}";
            }
        }
        else
        {
            // 1vsCPU: 왼쪽만 표시(헤더는 P1로 유지)
            var cpuBody = BuildSummaryForCPU();

            if (actionsSummaryLeft)
                actionsSummaryLeft.text = string.IsNullOrEmpty(cpuBody) ? headerLeft : $"{headerLeft}{rowSep}{cpuBody}";

            if (actionsSummaryRight)
            {
                actionsSummaryRight.text = "";
                actionsSummaryRight.gameObject.SetActive(false);
            }
        }
    }

    private void ClearActionLists()
    {
        if (actionsSummaryLeft) actionsSummaryLeft.text = "";
        if (actionsSummaryRight) actionsSummaryRight.text = "";
    }

    private string BuildSummaryFor1v1Player(int playerIndex) // 1 or 2
    {
        if (keyButtonInfos1vs1 == null || keyButtonInfos1vs1.Length == 0) return "";

        // 액션명 -> "표시 문자열" 맵 (대소문자 무시)
        var map = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var info in keyButtonInfos1vs1)
        {
            if (info == null || info.playerIndex != playerIndex) continue;

            if (ResolveRuntimeActionForInfo(info, out var action, out var bindIndex) &&
                action != null && bindIndex >= 0 && bindIndex < action.bindings.Count)
            {
                var binding = action.bindings[bindIndex];
                var key = binding.ToDisplayString();
                var actionLabel = string.IsNullOrEmpty(info.actionName) ? action.name : info.actionName;

                var line = actionLineFormat
                    .Replace("{key}", string.IsNullOrEmpty(key) ? "-" : key)
                    .Replace("{action}", actionLabel);

                map[actionLabel] = line; // 같은 액션 중복 시 마지막만 사용
            }
        }

        // 고정 순서대로 수집
        var lines = new System.Collections.Generic.List<string>();
        foreach (var name in summaryOrder)
            if (map.TryGetValue(name, out var line)) lines.Add(line);

        return JoinInColumns(lines, actionsPerRow, columnSeparator, actionLineSeparator);
    }

    private string BuildSummaryForCPU()
    {
        if (keyButtonInfosCPU == null || keyButtonInfosCPU.Length == 0) return "";

        var map = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var info in keyButtonInfosCPU)
        {
            if (info == null) continue;

            if (ResolveRuntimeActionForInfo(info, out var action, out var bindIndex) &&
                action != null && bindIndex >= 0 && bindIndex < action.bindings.Count)
            {
                var binding = action.bindings[bindIndex];
                var key = binding.ToDisplayString();
                var actionLabel = string.IsNullOrEmpty(info.actionName) ? action.name : info.actionName;

                var line = actionLineFormat
                    .Replace("{key}", string.IsNullOrEmpty(key) ? "-" : key)
                    .Replace("{action}", actionLabel);

                map[actionLabel] = line;
            }
        }

        var lines = new System.Collections.Generic.List<string>();
        foreach (var name in summaryOrder)
            if (map.TryGetValue(name, out var line)) lines.Add(line);

        return JoinInColumns(lines, actionsPerRow, columnSeparator, actionLineSeparator);
    }
    
    private static string NormalizeSeparator(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\n", "\n").Replace("\\t", "\t");
    }

    private string JoinInColumns(System.Collections.Generic.List<string> items, int perRow, string colSep, string rowSep)
    {
        if (items == null || items.Count == 0) return "";
        perRow = Mathf.Max(1, perRow);

        // 추가: 구분자 정규화
        colSep = NormalizeSeparator(colSep);
        rowSep = NormalizeSeparator(rowSep);

        if (perRow == 1) return string.Join(rowSep, items);

        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < items.Count; i++)
        {
            sb.Append(items[i]);

            bool endOfRow = ((i + 1) % perRow) == 0;
            bool isLast = i == items.Count - 1;

            if (!endOfRow && !isLast) sb.Append(colSep);
            if (endOfRow && !isLast) sb.Append(rowSep);
        }
        return sb.ToString();
    }

}