using UnityEngine;

public class ShellThrowEventProxy : MonoBehaviour
{
    public TurtlePlayerMovement turtlePlayer;

    // Animation Event에서 이 함수를 호출
    public void ThrowShellAtOpponent()
    {
        if (turtlePlayer != null)
            turtlePlayer.ThrowShellAtOpponent();
    }
}