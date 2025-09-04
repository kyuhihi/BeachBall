using TMPro;
using UnityEngine;

public class GetFinalResult : MonoBehaviour
{
    [SerializeField] private ResultType m_ResultType = ResultType.FinalScore;
    private TextMeshProUGUI m_TextMeshPro;
    [SerializeField] private bool m_bLeft = true;
    public enum ResultType
    {
        FinalScore,
        HitCnt,
        UltimateSkillCnt,
        AttackSkillCnt
    }

    void Start()
    {
        m_TextMeshPro = GetComponent<TextMeshProUGUI>();
        var gs = GameSettings.Instance;


        int Value = 0;
        switch (m_ResultType)
        {
            case ResultType.FinalScore:
                {
                    if(m_bLeft)
                        Value = gs.GetLeftScore();
                    else
                        Value = gs.GetRightScore();
                }
                break;
            case ResultType.HitCnt:
                {
                    if(m_bLeft)
                        Value = gs.GetLeftBallHitCount();
                    else
                        Value = gs.GetRightBallHitCount();
                }
                break;
            case ResultType.UltimateSkillCnt:
                {
                    if(m_bLeft)
                        Value = gs.GetLeftUltimateSkillCount();
                    else
                        Value = gs.GetRightUltimateSkillCount();
                }
                break;
            case ResultType.AttackSkillCnt:
                {
                    if(m_bLeft)
                        Value = gs.GetLeftAttackSkillCount();
                    else
                        Value = gs.GetRightAttackSkillCount();
                }
                break;
            default:
                break;
        }
        m_TextMeshPro.text = Value.ToString();
    }
}
