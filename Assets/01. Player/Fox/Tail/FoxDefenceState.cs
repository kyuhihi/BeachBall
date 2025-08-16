using UnityEngine;

public class FoxDefenceState : StateMachineBehaviour
{
    private FoxTail m_FoxTail;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (m_FoxTail == null)
        {
            m_FoxTail = animator.gameObject.GetComponentInChildren<FoxTail>();
        }
        m_FoxTail.SetTailState(FoxTail.TailState.Defence);
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        m_FoxTail.SetTailState(FoxTail.TailState.Idle);
    }
}
