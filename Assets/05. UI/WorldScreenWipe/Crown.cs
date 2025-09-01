using UnityEngine;

public class Crown : MonoBehaviour, IResetAbleListener
{
    private IPlayerInfo.CourtPosition m_CrownOwner = IPlayerInfo.CourtPosition.COURT_END;
    private MeshRenderer m_MeshRenderer;
    private GameObject m_ChildObj;

    public void Start()
    {
        m_CrownOwner = gameObject.name[0] == 'L' ? IPlayerInfo.CourtPosition.COURT_LEFT : IPlayerInfo.CourtPosition.COURT_RIGHT;
        m_MeshRenderer = GetComponent<MeshRenderer>();
        m_ChildObj = transform.GetChild(0).gameObject;
        m_MeshRenderer.enabled = false;
        m_ChildObj.SetActive(false);
    }
    public void OnRoundStart()
    {
        // Reset the crown's position or any other properties
    }

    public void OnRoundEnd()
    {
        IPlayerInfo.CourtPosition winner = GameManager.GetInstance().GetWinner();
        if (winner == m_CrownOwner)
        {
            m_MeshRenderer.enabled = true;
            m_ChildObj.SetActive(true);
        }
        else
        {
            m_MeshRenderer.enabled = false;
            m_ChildObj.SetActive(false);
        }
    }

    public void AddResetCall()
    {
        Signals.RoundResetAble.AddStart(OnRoundStart);
        Signals.RoundResetAble.AddEnd(OnRoundEnd);
    }

    public void RemoveResetCall()
    {
        Signals.RoundResetAble.RemoveStart(OnRoundStart);
        Signals.RoundResetAble.RemoveEnd(OnRoundEnd);
    }

    public void OnEnable()
    {
        AddResetCall();
    }
    public void OnDisable()
    {
        RemoveResetCall();
    }
}
