using UnityEngine;

public class UltimatePos
{
    private IPlayerInfo.PlayerType m_ePlayerType;
    private GameManager.UltimatePosition[] m_UltimatePositions;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public UltimatePos(IPlayerInfo.PlayerType ePlayerType, Vector3 rightPos)
    {
        m_ePlayerType = ePlayerType;
        m_UltimatePositions = new GameManager.UltimatePosition[2];
        m_UltimatePositions[0] = new GameManager.UltimatePosition(IPlayerInfo.CourtPosition.COURT_RIGHT, rightPos);
        Vector3 LeftPos = rightPos;
        LeftPos.z *= -1;
        m_UltimatePositions[1] = new GameManager.UltimatePosition(IPlayerInfo.CourtPosition.COURT_LEFT, LeftPos);
    }
    public Vector3 GetUltimatePosition(IPlayerInfo.CourtPosition courtPosition)
    {
        foreach (var position in m_UltimatePositions)
        {
            if (position.GetCourtPosition() == courtPosition)
            {
                return position.GetCutScenePosition();
            }
        }
        return Vector3.zero; // Default value if not found
    }


}
