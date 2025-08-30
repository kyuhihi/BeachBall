using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class TitleButtonMesh : MonoBehaviour
{
    private Renderer rend;
    private bool isMouseDownOnMe = false;
    public InputAction mouseClickAction;

    private Color originalColor;
    [SerializeField] private Color ClickedColor = Color.red;
    [SerializeField] private Color MouseDownColor = Color.yellow;
    [SerializeField] private Color TakenColor = new Color(0.7f, 0.7f, 0.7f); // 캐릭터가 이미 선택되었을 때 표시 색
    private bool isMouseOver = false;

    [SerializeField] private string buttonName;       // 회전/연출용 식별자("TurtleTitle","FoxTitle","QuitTitle" 등)
    [SerializeField] private string buttonHaviorName; // 모드/기능 이름("1vs1","1vsCPU","KeyMapping") 또는 캐릭터ID로도 사용 가능

    [SerializeField] private GameObject keySettingPanel;

    // 캐릭터 선택 버튼으로 사용할지 여부(체크 시 이 메시 클릭이 캐릭터 선택으로 동작)
    [SerializeField] private bool isCharacterButton = false;
    // 캐릭터 ID(비워두면 buttonHaviorName을 캐릭터ID로 사용)
    [SerializeField] private string characterId;

    // 선택 완료 시 자동 씬 진입 여부
    [SerializeField] private bool autoLoadOnComplete = true;
    [SerializeField] private string scene1vs1 = "Cho_Scene";
    [SerializeField] private string scene1vsCPU = "Cho_Scene";

    private bool isRotating = false;
    private bool isFixed90 = false;
    private bool isBeforeClicked = false;

    private float yTarget = 0f;
    private float zLerpSpeed = 5f;

    [SerializeField] private GameObject meshLabel;
    [SerializeField] private GameObject characterSelectRoot; // 캐릭터 선택용 그룹(패널/오브젝트)

    private void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend != null)
            originalColor = rend.material.color;

        if (buttonName == "TurtleTitle")
            isRotating = true;

        // 시작 시 선택 상태 반영
        UpdateVisualBySelection();
    }

    private void OnEnable()
    {
        if (mouseClickAction != null)
        {
            mouseClickAction.performed += OnMouseAction;
            mouseClickAction.Enable();
        }

        if (GameSettings.Instance != null)
            GameSettings.Instance.SelectionChanged += OnSelectionChanged;
    }

    private void OnDisable()
    {
        if (mouseClickAction != null)
        {
            mouseClickAction.performed -= OnMouseAction;
            mouseClickAction.Disable();
        }

        if (GameSettings.Instance != null)
            GameSettings.Instance.SelectionChanged -= OnSelectionChanged;

    
        if (characterSelectRoot != null && characterSelectRoot.activeSelf)
        {
            characterSelectRoot.SetActive(false);
        }

    }

    private void OnSelectionChanged()
    {
        UpdateVisualBySelection();
    }

    // 캐릭터 선택 상태 기반으로 색/콜라이더 갱신
    private void UpdateVisualBySelection()
    {
        if (!isCharacterButton || rend == null) return;

        var gs = GameSettings.Instance;
        string id = string.IsNullOrEmpty(characterId) ? buttonHaviorName : characterId;

        bool taken = gs != null && gs.IsCharacterTaken(id);
        bool disable =
            gs != null &&
            gs.forbidDuplicateChars &&
            taken &&
            (
                string.IsNullOrEmpty(gs.CurrentSelectSlot) ||
                gs.GetCharacterForSlot(gs.CurrentSelectSlot) != id
            );

        // 콜라이더 잠금(중복 금지 시 이미 선택된 캐릭터는 클릭 불가)
        var col = GetComponent<Collider>();
        if (col) col.enabled = !disable;

        // 시각 표시
        if (disable) rend.material.color = TakenColor;
        else rend.material.color = originalColor;
    }

    private void OnMouseAction(InputAction.CallbackContext ctx)
    {
        if (keySettingPanel != null && keySettingPanel.activeSelf)
            return;

        float value = ctx.ReadValue<float>();
        if (value > 0.5f)
        {
            // Mouse Down
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
                isMouseDownOnMe = (hit.transform == this.transform);
            else
                isMouseDownOnMe = false;
        }
        else
        {
            // Mouse Up
            if (isMouseDownOnMe)
            {
                Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.transform == this.transform)
                    {
                        // 캐릭터 버튼이면: 선택 플로우 처리 후 종료
                        if (isCharacterButton)
                        {
                            HandleCharacterSelectionClick();
                            isMouseDownOnMe = false;
                            return;
                        }

                        // 일반 버튼(모드/키세팅 등) 기존 동작
                        isBeforeClicked = !isBeforeClicked;

                        if (rend != null)
                            rend.material.color = isBeforeClicked ? ClickedColor : originalColor;

                        if (!isBeforeClicked) DestroyChildButtons();
                        else SpawnChildButtons();

                        // 특정 버튼 연출 + 동작
                        if (buttonName == "TurtleTitle" || buttonName == "FoxTitle" || buttonName == "QuitTitle")
                        {
                            if (isBeforeClicked) { isRotating = false; isFixed90 = true; }
                            else { isRotating = true; isFixed90 = false; }

                            // 모드 버튼: 선택 패널 토글 + 선택 플로우 시작
                            if (buttonHaviorName == "1vs1")
                            {
                                if (characterSelectRoot != null && characterSelectRoot.activeSelf)
                                {
                                    // 이미 열려있으면 닫기만
                                    characterSelectRoot.SetActive(false);
                                }
                                else
                                {
                                    // 닫혀 있으면 열고 선택 플로우 시작
                                    GameSettings.Instance.StartCharacterSelection("1vs1", forbidDuplicate: true, clearExisting: true);
                                    if (characterSelectRoot != null) characterSelectRoot.SetActive(true);
                                }
                            }
                            else if (buttonHaviorName == "1vsCPU")
                            {
                                if (characterSelectRoot != null && characterSelectRoot.activeSelf)
                                {
                                    characterSelectRoot.SetActive(false);
                                }
                                else
                                {
                                    GameSettings.Instance.StartCharacterSelection("1vsCPU", forbidDuplicate: true, clearExisting: true);
                                    if (characterSelectRoot != null) characterSelectRoot.SetActive(true);
                                }
                            }
                            else if (buttonHaviorName == "KeyMapping")
                            {
                                if (keySettingPanel != null) keySettingPanel.SetActive(true);
                            }
                        }
                    }
                }
                isMouseDownOnMe = false;
            }
        }
    }

    private void HandleCharacterSelectionClick()
    {
        var gs = GameSettings.Instance;
        if (gs == null) return;

        string id = string.IsNullOrEmpty(characterId) ? buttonHaviorName : characterId;

        if (string.IsNullOrEmpty(gs.CurrentSelectSlot))
        {
            Debug.Log("현재는 캐릭터 선택 단계가 아닙니다.");
            return;
        }

        if (gs.forbidDuplicateChars && gs.IsCharacterTaken(id) && gs.GetCharacterForSlot(gs.CurrentSelectSlot) != id)
        {
            Debug.Log("이미 선택된 캐릭터입니다.");
            return;
        }

        if (gs.TrySelectCurrent(id, out var completed, out var err))
        {
            UpdateVisualBySelection(); // 내 버튼 갱신

            // 완료 시 자동 씬 진입
            if (completed && autoLoadOnComplete)
            {
                if (gs.gameMode == "1vs1") SceneManager.LoadScene(scene1vs1);
                else SceneManager.LoadScene(scene1vsCPU);
            }
        }
        else
        {
            Debug.Log(err ?? "선택 실패");
        }
    }

    private void ShowLabel()
    {
        // 라벨이 본인 오브젝트라면 비활성화하지 않도록 가드
        if (meshLabel != null && meshLabel != this.gameObject) meshLabel.SetActive(true);
    }


    private void HideLabel()
    {
        if (meshLabel != null && meshLabel != this.gameObject) meshLabel.SetActive(false);
    }

    protected virtual void SpawnChildButtons()
    {
        for (int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(true);
    }

    private void DestroyChildButtons()
    {
        for (int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(false);
    }

    private void Update()
    {
        // 마우스 위치에서 Raycast
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit hit;
        bool nowOver = false;
        if (Physics.Raycast(ray, out hit))
            nowOver = (hit.transform == this.transform);

        // 회전/고정(기존 연출)
        if (buttonName == "TurtleTitle")
        {
            if (!isFixed90 || !isBeforeClicked)
            {
                if (nowOver) { isRotating = false; yTarget = 0f; }
                else { isRotating = true; }
            }
            if (isRotating) transform.Rotate(Vector3.up, 60f * Time.deltaTime, Space.World);

            Vector3 euler = transform.eulerAngles;
            if (nowOver) euler.y = Mathf.LerpAngle(euler.y, yTarget, Time.deltaTime * zLerpSpeed);
            transform.eulerAngles = euler;
        }
        else if (buttonName == "FoxTitle")
        {
            if (!isFixed90 || !isBeforeClicked)
            {
                if (nowOver) { isRotating = false; yTarget = 90f; }
                else { isRotating = true; }
            }
            if (isRotating) transform.Rotate(Vector3.up, 120f * Time.deltaTime, Space.World);

            Vector3 euler = transform.eulerAngles;
            if (nowOver) euler.y = Mathf.LerpAngle(euler.y, yTarget, Time.deltaTime * zLerpSpeed);
            transform.eulerAngles = euler;
        }
        else if (buttonName == "QuitTitle")
        {
            if (!isFixed90 || !isBeforeClicked)
            {
                if (nowOver) { isRotating = false; yTarget = 0f; }
                else { isRotating = true; }
            }
            if (isRotating) transform.Rotate(Vector3.up, 60f * Time.deltaTime, Space.World);

            Vector3 euler = transform.eulerAngles;
            if (nowOver) euler.y = Mathf.LerpAngle(euler.y, yTarget, Time.deltaTime * zLerpSpeed);
            transform.eulerAngles = euler;
        }

        // 색상 상태(호버)
        if (nowOver && !isMouseOver)
        {
            if (rend != null)
            {
                // 캐릭터 버튼은 호버 색으로 덮어쓰지 않고 선택 상태를 유지
                if (!isCharacterButton)
                    rend.material.color = MouseDownColor;
            }
            ShowLabel();
        }
        else if (!nowOver && isMouseOver)
        {
            if (rend != null)
            {
                if (!isCharacterButton)
                    rend.material.color = isBeforeClicked ? ClickedColor : originalColor;
            }
            HideLabel();
            // 캐릭터 버튼은 선택 상태 색을 다시 반영
            UpdateVisualBySelection();
        }
        isMouseOver = nowOver;
    }

    public void OnClickKeySettingEscButton()
    {
        isBeforeClicked = !isBeforeClicked;
        isRotating = !isRotating;
        isFixed90 = !isFixed90;
    }
    
 
}