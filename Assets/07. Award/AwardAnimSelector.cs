using UnityEngine;

public class AwardAnimSelector : MonoBehaviour
{
    bool bSelected = false;
    public bool GetSelected() { return bSelected; }
    private WinLoseCheer m_eWinLoseCheer = WinLoseCheer.Cheer;
    public WinLoseCheer GetWinLoseCheer() { return m_eWinLoseCheer; }
    public void SetWinLoseCheer(AwardCube.AwardType e)
    {
        switch (e)
        {
            case AwardCube.AwardType.Gold:
                m_eWinLoseCheer = WinLoseCheer.Win;
                break;
            case AwardCube.AwardType.Silver:
                m_eWinLoseCheer = WinLoseCheer.Lose;
                break;
            case AwardCube.AwardType.None:
                m_eWinLoseCheer = WinLoseCheer.Cheer;
                break;
        }
        bSelected = true;
    }
    public Animator m_Animator;
    public enum WinLoseCheer
    {
        Win,
        Lose,
        Cheer
    } 


    // Update is called once per frame
    void Update()
    {
        if(bSelected)
            m_Animator.SetInteger("WinLoseCheer", (int)m_eWinLoseCheer);
    }
}
