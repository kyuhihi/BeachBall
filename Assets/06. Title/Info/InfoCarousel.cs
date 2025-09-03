using UnityEngine;
using UnityEngine.InputSystem;

public class InfoCarousel : MonoBehaviour
{
    public void ReverseActive() => gameObject.SetActive(!gameObject.activeSelf);
    GameObject[] _children;
    private int _currentIndex = 0;

    public void Next()
    {
        _children[_currentIndex].SetActive(false);
        _currentIndex = (_currentIndex + 1) % _children.Length;
        _children[_currentIndex].SetActive(true);
    }
    public void Prev()
    {
        _children[_currentIndex].SetActive(false);
        _currentIndex = (_currentIndex - 1 + _children.Length) % _children.Length;
        _children[_currentIndex].SetActive(true);
    }

    void Start()
    {
        _children = new GameObject[transform.childCount-2];
        for (int i = 0; i < transform.childCount; i++)
        {
            if(transform.GetChild(i).name.Contains("BG"))
            {
                _children[i] = transform.GetChild(i).gameObject;
            }
        }
    }

    void Update()
    {
        
    }

   


}
