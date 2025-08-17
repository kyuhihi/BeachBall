using UnityEngine;

public class FoxAttackStateMachine : StateMachineBehaviour
{
    private FoxPlayerMovement m_FoxPlayerMovement;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (m_FoxPlayerMovement == null)
            m_FoxPlayerMovement = animator.GetComponentInParent<FoxPlayerMovement>();

        m_FoxPlayerMovement.ShootFireBall();
    }


}
