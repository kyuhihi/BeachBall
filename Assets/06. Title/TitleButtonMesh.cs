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
    private bool isMouseOver = false;
    [SerializeField] private string buttonName;
    [SerializeField] private string buttonHaviorName;

    [SerializeField] private GameObject keySettingPanel;


    private bool isRotating = false;
    private bool isFixed90 = false;
    private bool isBeforeClicked = false;

    private float yTarget = 0f;
    private float zLerpSpeed = 5f;


    [SerializeField] private GameObject meshLabel; // 텍스트 오브젝트(자식으로 미리 만들어두기)

    private void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend != null)
        {
            originalColor = rend.material.color;
        }
        if (buttonName == "TurtleTitle")
            isRotating = true;
    }

    private void OnEnable()
    {
        if (mouseClickAction != null)
        {
            mouseClickAction.performed += OnMouseAction;
            mouseClickAction.Enable();
        }
    }

    private void OnDisable()
    {
        if (mouseClickAction != null)
        {
            mouseClickAction.performed -= OnMouseAction;
            mouseClickAction.Disable();
        }
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
                        // 클릭 토글
                        isBeforeClicked = !isBeforeClicked;

                        // 색상 변경
                        if (rend != null)
                        {
                            if (isBeforeClicked)
                                rend.material.color = ClickedColor;
                            else
                                rend.material.color = originalColor;
                        }

                        // 자식 토글
                        if (!isBeforeClicked)
                            DestroyChildButtons();
                        else
                            SpawnChildButtons();

                        // TurtleTitle 클릭 시 90도 회전 고정/해제
                        if (buttonName == "TurtleTitle" || buttonName == "FoxTitle" || buttonName == "QuitTitle")
                        {
                            if (isBeforeClicked)
                            {
                                isRotating = false;
                                isFixed90 = true;
                            }
                            else
                            {
                                isRotating = true;
                                isFixed90 = false;
                            }

                            // buttonHaviorName에 따라 Scene 이동 및 세팅 저장
                            if (buttonHaviorName == "1vs1")
                            {
                                GameSettings.Instance.gameMode = "1vs1";
                                SceneManager.LoadScene("1vs1");
                            }
                            else if (buttonHaviorName == "1vsCPU")
                            {
                                GameSettings.Instance.gameMode = "1vsCPU";
                                SceneManager.LoadScene("1vsCPU");
                            }
                            else if (buttonHaviorName == "KeyMapping")
                            {
                                keySettingPanel.SetActive(true);
                            }
                        }

                        // 예시: 캐릭터 선택 버튼이면
                        if (buttonHaviorName == "Turtle" || buttonHaviorName == "Fox")
                        {
                            GameSettings.Instance.selectedCharacter = buttonHaviorName;
                        }

                    }
                }
                isMouseDownOnMe = false;
            }
        }
    }

    private void ShowLabel()
    {
        if (meshLabel != null)
        {
            meshLabel.SetActive(true);
        }
    }

    private void HideLabel()
    {
        if (meshLabel != null)
            meshLabel.SetActive(false);
    }



    protected virtual void SpawnChildButtons()
    {
        // 이 오브젝트의 모든 자식 오브젝트를 활성화하고, 위치 고정
        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i).gameObject;
            child.SetActive(true);
        }
    }

    private void DestroyChildButtons()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i).gameObject;
            child.SetActive(false);
        }
    }

    private void Update()
    {

        // 마우스 위치에서 Raycast
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit hit;
        bool nowOver = false;
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.transform == this.transform)
                nowOver = true;
        }

        // TurtleTitle 자전/정지/고정 처리
        if (buttonName == "TurtleTitle")
        {
            if (!isFixed90 || !isBeforeClicked)
            {
                if (nowOver)
                {
                    isRotating = false;
                    yTarget = 0f;
                }
                else
                {
                    isRotating = true;
                }
            }

            if (isRotating)
            {
                transform.Rotate(Vector3.up, 60f * Time.deltaTime, Space.World);
            }

            Vector3 euler = transform.eulerAngles;
            if (nowOver)
                euler.y = Mathf.LerpAngle(euler.y, yTarget, Time.deltaTime * zLerpSpeed);
            transform.eulerAngles = euler;
        }
        else if (buttonName == "FoxTitle")
        {
            if (!isFixed90 || !isBeforeClicked)
            {
                if (nowOver)
                {
                    isRotating = false;
                    yTarget = 90f;
                }
                else
                {
                    isRotating = true;
                }
            }

            if (isRotating)
            {
                transform.Rotate(Vector3.up, 120f * Time.deltaTime, Space.World);
            }

            Vector3 euler = transform.eulerAngles;
            if (nowOver)
                euler.y = Mathf.LerpAngle(euler.y, yTarget, Time.deltaTime * zLerpSpeed);
            transform.eulerAngles = euler;
        }
        else if (buttonName == "QuitTitle")
        {
            if (!isFixed90 || !isBeforeClicked)
            {
                if (nowOver)
                {
                    isRotating = false;
                    yTarget = 0f;
                }
                else
                {
                    isRotating = true;
                }
            }

            if (isRotating)
            {
                transform.Rotate(Vector3.up, 60f * Time.deltaTime, Space.World);
            }

            Vector3 euler = transform.eulerAngles;
            if (nowOver)
                euler.y = Mathf.LerpAngle(euler.y, yTarget, Time.deltaTime * zLerpSpeed);
            transform.eulerAngles = euler;
        }

        // 색상 상태 관리
        if (nowOver && !isMouseOver)
        {
            // 마우스 오버 시작
            if (rend != null)
            {
                if (isBeforeClicked)
                    rend.material.color = MouseDownColor; // 클릭된 상태에서 오버
                else
                    rend.material.color = MouseDownColor; // 클릭 안된 상태에서 오버
            }
            ShowLabel();
        }
        else if (!nowOver && isMouseOver)
        {
            // 마우스 오버 해제
            if (rend != null)
            {
                if (isBeforeClicked)
                    rend.material.color = ClickedColor; // 클릭된 상태로 복귀
                else
                    rend.material.color = originalColor; // 원래 색으로 복귀
            }
            HideLabel();
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