using UnityEngine;

public interface IUIInfo
{
    public enum UIType
    {
        Deco,
        UltimateBar,
        DashBar,
        ScoreCount,
    }

    UIType GetUIType();

    IPlayerInfo.CourtPosition GetCourtPosition { get; }
    void SetCourtPosition(IPlayerInfo.CourtPosition position);

    bool CanUseAbility { get; }
    void UseAbility();

    float GetValueFloat { get; }
    void DecreaseValueFloat(float value);

    int GetValueInt { get; }
    void DecreaseValueInt(int value);
}
