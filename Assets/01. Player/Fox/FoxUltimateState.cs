using UnityEngine;

public class FoxUltimateState : StateMachineBehaviour
{
    Rigidbody m_Rigidbody;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (m_Rigidbody == null)
            m_Rigidbody = animator.GetComponent<Rigidbody>();

        //animator.applyRootMotion = true;
        // Code to execute when the ultimate state is entered
    }
    public override void OnStateExit(Animator animator, AnimatorStateInfo animatorStateInfo, int layerIndex) {
        //animator.applyRootMotion = false;
    }
}