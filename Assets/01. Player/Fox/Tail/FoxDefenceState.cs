using UnityEngine;

public class FoxDefenceState : StateMachineBehaviour
{
    private FoxTail m_DefenceFoxTail;
    private FoxTail m_IdleFoxTail;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // 한번만 찾기(비활성 포함)
        if (m_DefenceFoxTail == null || m_IdleFoxTail == null)
        {
            Transform idleT = null, defT = null;
            var all = animator.gameObject.GetComponentsInChildren<Transform>(true);
            foreach (var t in all)
            {
                if (t.name == "IdleFoxTail")     idleT = t;
                else if (t.name == "DefenceFoxTail") defT = t;
            }

            if (idleT != null)  m_IdleFoxTail    = idleT.GetComponent<FoxTail>();
            if (defT != null)   m_DefenceFoxTail = defT.GetComponent<FoxTail>();

            if (m_IdleFoxTail == null)    Debug.LogWarning("[FoxDefenceState] 'IdleFoxTail'을 찾지 못했거나 FoxTail 컴포넌트가 없습니다.");
            if (m_DefenceFoxTail == null) Debug.LogWarning("[FoxDefenceState] 'DefenceFoxTail'을 찾지 못했거나 FoxTail 컴포넌트가 없습니다.");
        }

        if (m_DefenceFoxTail != null)
        {
            m_DefenceFoxTail.gameObject.SetActive(true);
            m_DefenceFoxTail.SetTailState(FoxTail.TailState.Defence);
        }
        if (m_IdleFoxTail != null)
        {
            m_IdleFoxTail.gameObject.SetActive(false);
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (m_DefenceFoxTail != null)
            m_DefenceFoxTail.gameObject.SetActive(false);

        if (m_IdleFoxTail != null)
        {
            m_IdleFoxTail.gameObject.SetActive(true);
            m_IdleFoxTail.SetTailState(FoxTail.TailState.Idle);
        }
    }
}
