using UnityEngine;

public class WaterCannonEventProxy : MonoBehaviour
{
    public TurtlePlayerMovement turtlePlayer;

    // Animation Event에서 이 함수를 호출
    public void FireWaterCannon()
    {
        if (turtlePlayer != null)
            turtlePlayer.FireWaterCannon();
    }
}