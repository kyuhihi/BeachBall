using UnityEngine;

public abstract class UIInfoBase : MonoBehaviour, IUIInfo
{
    public abstract IUIInfo.UIType GetUIType();
    public abstract IPlayerInfo.CourtPosition GetCourtPosition { get; }
    public abstract void SetCourtPosition(IPlayerInfo.CourtPosition position);

    public abstract bool CanUseAbility { get; }
    public abstract void UseAbility();

    public abstract float GetValueFloat { get; }
    public abstract void DecreaseValueFloat(float value);

    public abstract int GetValueInt { get; }
    public abstract void DecreaseValueInt(int value);
}
