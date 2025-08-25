using UnityEngine;

public class DropdownMenu : MonoBehaviour
{
    public GameObject subMenu; // 하위 버튼들이 들어있는 패널

    private bool isOpen = false;

    public void ToggleMenu()
    {
        isOpen = !isOpen;
        subMenu.SetActive(isOpen);
    }
}