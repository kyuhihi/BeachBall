using UnityEngine;

public interface IPlayerInfo
{
    public PlayerType m_PlayerType { get; set; }
    public Color m_PlayerDefaultColor { get; set; }
    public CourtPosition m_CourtPosition { get; set; }

    public enum PlayerType
    {
        Fox = 1,
        Turtle = 2,
        Penguin = 3,
        Monkey = 4
    }

    public enum CourtPosition
    {
        COURT_RIGHT,
        COURT_LEFT,
        COURT_END
    }
}

public interface ICutSceneListener
{
    void OnStartCutScene(IPlayerInfo.PlayerType playerType, IPlayerInfo.CourtPosition courtPosition);
    void OnEndCutscene(IPlayerInfo.PlayerType playerType, IPlayerInfo.CourtPosition courtPosition);
}