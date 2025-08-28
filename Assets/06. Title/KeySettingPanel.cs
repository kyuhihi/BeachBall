using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.IO;

[System.Serializable]
public class KeyButtonInfo
{
    public Button button;
    public Image keyImage;
    public string actionName;
    public int bindingIndex;
}

public class KeySettingPanel : MonoBehaviour
{
    public GameObject setting1vs1;
    public GameObject setting1vsCPU;
    public InputActionAsset inputActions1vs1_Player1;
    public InputActionAsset inputActions1vs1_Player2;
    public InputActionAsset inputActions1vsCPU;

    private InputActionAsset[] currentInputActions;

    public KeyButtonInfo[] keyButtonInfosP1;
    public KeyButtonInfo[] keyButtonInfosP2;
    public KeyButtonInfo[] keyButtonInfosCPU;

    public Color player1Color = Color.blue;
    public Color player2Color = new Color(1f, 0.5f, 0f);
    public Color cpuColor = Color.black;
    public Color normalColor = Color.white;

    public Transform keyParentP1; // Player1 키보드 버튼 부모
    public Transform keyParentP2; // Player2 키보드 버튼 부모
    public Transform keyParentCPU; // 1vsCPU 키보드 버튼 부모

    public void OnSet1vs1Button()
    {
        Debug.Log("1vs1 모드 선택");
        SetMode("1vs1");
    }

    public void OnSet1vsCPUButton()
    {
        Debug.Log("1vsCPU 모드 선택");
        SetMode("1vsCPU");
    }

    public void SetMode(string mode)
    {
        if (mode == "1vs1")
        {
            currentInputActions = new InputActionAsset[] { inputActions1vs1_Player1, inputActions1vs1_Player2 };
            if (setting1vs1 != null) setting1vs1.SetActive(true);
            if (setting1vsCPU != null) setting1vsCPU.SetActive(false);
        }
        else if (mode == "1vsCPU")
        {
            currentInputActions = new InputActionAsset[] { inputActions1vsCPU };
            if (setting1vs1 != null) setting1vs1.SetActive(false);
            if (setting1vsCPU != null) setting1vsCPU.SetActive(true);
        }
        GameSettings.Instance.LoadKeyBindings();
        UpdateKeyColors();
    }

    void Start()
    {
        // Player1
        if (keyParentP1 != null)
        {
            var buttons = keyParentP1.GetComponentsInChildren<Button>(true);
            keyButtonInfosP1 = new KeyButtonInfo[buttons.Length];
            for (int i = 0; i < buttons.Length; i++)
            {
                keyButtonInfosP1[i] = new KeyButtonInfo
                {
                    button = buttons[i],
                    keyImage = buttons[i].GetComponentInChildren<Image>(),
                    actionName = "", // Inspector에서 직접 입력하거나, 자동 매칭 로직 추가 가능
                    bindingIndex = 0
                };
            }
        }
        // Player2
        if (keyParentP2 != null)
        {
            var buttons = keyParentP2.GetComponentsInChildren<Button>(true);
            keyButtonInfosP2 = new KeyButtonInfo[buttons.Length];
            for (int i = 0; i < buttons.Length; i++)
            {
                keyButtonInfosP2[i] = new KeyButtonInfo
                {
                    button = buttons[i],
                    keyImage = buttons[i].GetComponentInChildren<Image>(),
                    actionName = "",
                    bindingIndex = 0
                };
            }
        }
        // CPU
        if (keyParentCPU != null)
        {
            var buttons = keyParentCPU.GetComponentsInChildren<Button>(true);
            keyButtonInfosCPU = new KeyButtonInfo[buttons.Length];
            for (int i = 0; i < buttons.Length; i++)
            {
                keyButtonInfosCPU[i] = new KeyButtonInfo
                {
                    button = buttons[i],
                    keyImage = buttons[i].GetComponentInChildren<Image>(),
                    actionName = "",
                    bindingIndex = 0
                };
            }
        }

        // 기존 Start() 코드 이어서...
        SetMode("1vs1");

        for (int i = 0; i < keyButtonInfosP1.Length; i++)
        {
            int idx = i;
            keyButtonInfosP1[i].button.onClick.AddListener(() => OnKeyButtonClicked(idx, 0));
        }
        for (int i = 0; i < keyButtonInfosP2.Length; i++)
        {
            int idx = i;
            keyButtonInfosP2[i].button.onClick.AddListener(() => OnKeyButtonClicked(idx, 1));
        }
        for (int i = 0; i < keyButtonInfosCPU.Length; i++)
        {
            int idx = i;
            keyButtonInfosCPU[i].button.onClick.AddListener(() => OnKeyButtonClicked(idx, 0));
        }

        GameSettings.Instance.LoadKeyBindings();
        UpdateKeyColors();

        DebugPrintActionMap(currentInputActions[0], "Player1");
        if (currentInputActions.Length > 1)
            DebugPrintActionMap(currentInputActions[1], "Player2");
    }

    void UpdateKeyColors()
    {
        // Player1
        if (currentInputActions.Length > 0)
        {
            var actionMap = currentInputActions[0];


            foreach (var info in keyButtonInfosP1)
            {
                var action = actionMap.FindAction(info.actionName);
                if (action != null && action.bindings.Count > info.bindingIndex)
                {
                    var binding = action.bindings[info.bindingIndex];
                    var keyLabel = info.button.GetComponentInChildren<TMP_Text>();
                    if (keyLabel != null)
                        keyLabel.text = binding.ToDisplayString();
                    if (info.keyImage != null)
                        info.keyImage.color = player1Color;
                }
                else
                {
                    var keyLabel = info.button.GetComponentInChildren<TMP_Text>();
                    if (keyLabel != null)
                        keyLabel.text = "-";
                    if (info.keyImage != null)
                        info.keyImage.color = normalColor;
                }
            }
        }
        // Player2
        if (currentInputActions.Length > 1)
        {
            var actionMap = currentInputActions[1];
            foreach (var info in keyButtonInfosP2)
            {
                var action = actionMap.FindAction(info.actionName);
                if (action != null && action.bindings.Count > info.bindingIndex)
                {
                    var binding = action.bindings[info.bindingIndex];
                    var keyLabel = info.button.GetComponentInChildren<TMP_Text>();
                    if (keyLabel != null)
                        keyLabel.text = binding.ToDisplayString();
                    if (info.keyImage != null)
                        info.keyImage.color = player2Color;
                }
                else
                {
                    var keyLabel = info.button.GetComponentInChildren<TMP_Text>();
                    if (keyLabel != null)
                        keyLabel.text = "-";
                    if (info.keyImage != null)
                        info.keyImage.color = normalColor;
                }
            }
        }
        // CPU
        if (currentInputActions.Length == 1)
        {
            var actionMap = currentInputActions[0];
            foreach (var info in keyButtonInfosCPU)
            {
                var action = actionMap.FindAction(info.actionName);
                if (action != null && action.bindings.Count > info.bindingIndex)
                {
                    var binding = action.bindings[info.bindingIndex];
                    var keyLabel = info.button.GetComponentInChildren<TMP_Text>();
                    if (keyLabel != null)
                        keyLabel.text = binding.ToDisplayString();
                    if (info.keyImage != null)
                        info.keyImage.color = cpuColor;
                }
                else
                {
                    var keyLabel = info.button.GetComponentInChildren<TMP_Text>();
                    if (keyLabel != null)
                        keyLabel.text = "-";
                    if (info.keyImage != null)
                        info.keyImage.color = normalColor;
                }
            }
        }
    }

    void OnKeyButtonClicked(int idx, int playerIndex)
    {
        KeyButtonInfo[] infos = null;
        if (playerIndex == 0)
            infos = keyButtonInfosP1;
        else if (playerIndex == 1)
            infos = keyButtonInfosP2;
        else
            infos = keyButtonInfosCPU;

        var info = infos[idx];
        var action = currentInputActions[playerIndex].FindAction(info.actionName);

        Debug.Log($"리바인딩 시도: idx={idx}, playerIndex={playerIndex}, actionName={info.actionName}, bindingIndex={info.bindingIndex}");

        if (action != null)
        {
            Debug.Log("어떤 키로 바꾸시겠습니까?");
            action.PerformInteractiveRebinding(info.bindingIndex)
                .WithCancelingThrough("<Mouse>/rightButton")
                .OnComplete(op =>
                {
                    Debug.Log("리바인딩 완료: " + action.bindings[info.bindingIndex].ToDisplayString());
                    op.Dispose();
                    GameSettings.Instance.SaveKeyBindings(); // 리바인딩 후 저장
                    UpdateKeyColors(); // UI 갱신
                })
                .Start();
        }
        else
        {
            Debug.LogWarning("해당 액션을 찾을 수 없습니다: " + info.actionName);
        }
    }


    void DebugPrintActionMap(InputActionAsset actionMap, string tag = "")
    {
        if (actionMap == null)
        {
            Debug.Log($"{tag} actionMap is null");
            return;
        }
        foreach (var action in actionMap)
        {
            Debug.Log($"{tag} 액션: {action.name}");
            for (int i = 0; i < action.bindings.Count; i++)
            {
                var binding = action.bindings[i];
                Debug.Log($"{tag}   바인딩[{i}]: {binding.path} ({binding.ToDisplayString()})");
            }
        }
    }

}