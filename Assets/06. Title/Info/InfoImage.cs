using UnityEngine;

public class InfoImage : MonoBehaviour
{
    public void ReverseActive()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }
}
