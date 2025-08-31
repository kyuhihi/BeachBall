using Unity.VisualScripting;
using UnityEngine;

public class CheckUltimateSkillTiming : MonoBehaviour
{
    private IPlayerInfo.PlayerType m_PlayerInfo;
    private ParticleSystem m_ParticleSystem;
    private ParticleSystem m_ChildParticleSystem;
    private const string BaseName = "_UltimateBar";
    private UltimateBarDriver m_UltimateBar;

    [SerializeField] Gradient ColorFoxGradient;
    [SerializeField] Gradient ColorTurtleGradient;
    [SerializeField] Gradient ColorMonkeyGradient;

    public void SetPlayerInfo(IPlayerInfo.PlayerType playerInfo)
    {
        this.m_PlayerInfo = playerInfo;
    }

    public void Update()
    {
        if (!m_UltimateBar)
        {
            return;
        }
        if (m_UltimateBar.GetGauge() >= 0.9999f)
        {
            if (!m_ParticleSystem.isPlaying)
            {
                m_ParticleSystem.Play();
                m_ChildParticleSystem.Play();
            }
        }
        else
        {
            if (m_ParticleSystem.isPlaying)
            {
                m_ParticleSystem.Stop();
                m_ChildParticleSystem.Stop();
            }
        }

    }
    public void Initialize()
    {
        if (m_ParticleSystem == null)
        {
            m_ParticleSystem = GetComponent<ParticleSystem>();
            m_ChildParticleSystem = gameObject.transform.GetChild(0).GetComponentInChildren<ParticleSystem>();
        }

        // ColorOverLifetime 모듈 꺼내오기
        var col = m_ParticleSystem.colorOverLifetime;
        col.enabled = true;
        var main = m_ChildParticleSystem.main;
        Gradient grad = new Gradient();
        switch (m_PlayerInfo)
        {
            case IPlayerInfo.PlayerType.Fox:
                grad = ColorFoxGradient;
                main.startColor = Color.orange;
                break;
            case IPlayerInfo.PlayerType.Turtle:
                grad = ColorTurtleGradient;
                main.startColor = Color.blue;
                break;
            case IPlayerInfo.PlayerType.Monkey:
                grad = ColorMonkeyGradient;
                main.startColor = Color.gray;
                break;
        }

        // Gradient를 모듈에 할당
        col.color = new ParticleSystem.MinMaxGradient(grad);

        m_ParticleSystem.Stop();
        m_ChildParticleSystem.Stop();
        string FindObjName = transform.parent.name[0] + BaseName;

        GameObject Parent = transform.parent.gameObject;
        m_UltimateBar = Parent.transform.Find(FindObjName).GetComponent<UltimateBarDriver>();

    }
}

