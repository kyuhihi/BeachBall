using UnityEngine;
using System.Collections;

public class DiveState : StateMachineBehaviour
{
    private Rigidbody m_Rigidbody;
    private BasePlayerMovement m_BasePlayerMovement;

    [SerializeField] private float diveForce = 7f;
    [SerializeField] private float diveDirectionY = 1f;
    [SerializeField] private DiveSequence diveSequence = DiveSequence.DoDive;
    private CapsuleCollider capsuleCollider;
    public enum DiveSequence
    {
        DoDive,
        GetUp
    }


    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (m_Rigidbody == null)
        {
            m_Rigidbody = animator.GetComponentInParent<Rigidbody>();
            m_BasePlayerMovement = animator.GetComponentInParent<BasePlayerMovement>();
            CapsuleCollider[] colliders = animator.transform.parent.GetComponentsInChildren<CapsuleCollider>();
            
            capsuleCollider = colliders[1]; // Ã¹ ¹øÂ° Ä¸½¶ ÄÝ¶óÀÌ´õ¸¦ »ç¿ë
        }

        if (diveSequence == DiveSequence.DoDive)
        {
            capsuleCollider.enabled = true; // Ä¸½¶ ÄÝ¶óÀÌ´õ È°¼ºÈ­
            m_BasePlayerMovement.MoveByInput = false;

            Vector3 forward = animator.gameObject.transform.forward;
            forward.y += diveDirectionY;
            m_Rigidbody.AddForce(forward * diveForce, ForceMode.Impulse);
        }
        else
        {
            capsuleCollider.enabled = false; // Ä¸½¶ ÄÝ¶óÀÌ´õ È°¼ºÈ­

            m_BasePlayerMovement.StartCoroutine(EnableMoveByInputAfterDelay(m_BasePlayerMovement, 1.0f));

        }

    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {

        Vector3 NormalVelocity = m_Rigidbody.linearVelocity.normalized * 2f;
        NormalVelocity.y += -diveDirectionY;
        m_Rigidbody.linearVelocity = NormalVelocity;
    }
    private IEnumerator EnableMoveByInputAfterDelay(BasePlayerMovement movement, float delay)
    {
        yield return new WaitForSeconds(delay);
        movement.MoveByInput = true;
    }
}