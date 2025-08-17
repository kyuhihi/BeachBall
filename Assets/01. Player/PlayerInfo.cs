using UnityEngine;

public interface IPlayerInfo
{
    PlayerType m_PlayerType { get; set; }
    Color m_PlayerDefaultColor { get; set; }
    public enum PlayerType
    {
        Fox = 1,
        Turtle = 2,
        Penguin = 3,
        Monkey = 4
    }
}