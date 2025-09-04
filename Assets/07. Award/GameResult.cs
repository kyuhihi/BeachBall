using UnityEngine;

public class GameResult
{
    private int m_LeftScore = 0;
    private int m_RightScore = 0;

    private int m_LeftBallHitCount = 0;
    private int m_RightBallHitCount = 0;

    private int m_LeftUltimateSkillCount = 0;
    private int m_RightUltimateSkillCount = 0;

    private int m_LeftAttackSkillCount = 0;
    private int m_RightAttackSkillCount = 0;

    public int GetLeftScore() => m_LeftScore;
    public int GetRightScore() => m_RightScore;
    public void SetLeftScore(int score) => m_LeftScore = score;
    public void SetRightScore(int score) => m_RightScore = score;

    public int GetLeftBallHitCount() => m_LeftBallHitCount;
    public int GetRightBallHitCount() => m_RightBallHitCount;
    public void AddLeftBallHitCount(int count) => m_LeftBallHitCount += count;
    public void AddRightBallHitCount(int count) => m_RightBallHitCount += count;//¹Ù´Ú¿¡ ´êÀºÈ½¼ö

    public int GetLeftUltimateSkillCount() => m_LeftUltimateSkillCount;
    public int GetRightUltimateSkillCount() => m_RightUltimateSkillCount;
    public void AddLeftUltimateSkillCount() => m_LeftUltimateSkillCount += 1;
    public void AddRightUltimateSkillCount() => m_RightUltimateSkillCount += 1;


    public int GetLeftAttackSkillCount() => m_LeftAttackSkillCount;
    public int GetRightAttackSkillCount() => m_RightAttackSkillCount;
    public void AddLeftAttackSkillCount() => m_LeftAttackSkillCount += 1;
    public void AddRightAttackSkillCount() => m_RightAttackSkillCount += 1;

    public GameResult()
    {

    }

    public void Reset()
    {
        m_LeftScore = 0;
        m_RightScore = 0;

        m_LeftBallHitCount = 0;
        m_RightBallHitCount = 0;

        m_LeftUltimateSkillCount = 0;
        m_RightUltimateSkillCount = 0;

        m_LeftAttackSkillCount = 0;
        m_RightAttackSkillCount = 0;
    }

}
