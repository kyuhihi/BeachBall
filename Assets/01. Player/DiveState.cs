using UnityEngine;

public class DiveState : StateMachineBehaviour
{
    private Rigidbody m_Rigidbody;
    private BasePlayerMovement m_BasePlayerMovement;

    [SerializeField] private float diveForce = 5f;
    [SerializeField] private float diveDirectionY = 0.5f;

    // This class is currently empty, but can be used to define behaviors for the Dive state in a state machine.
    // You can add methods like OnStateEnter, OnStateUpdate, and OnStateExit to handle specific behaviors when entering, updating, or exiting the Dive state.
    // Example:
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        m_Rigidbody = animator.GetComponentInParent<Rigidbody>();
        m_BasePlayerMovement = animator.GetComponentInParent<BasePlayerMovement>();
        m_BasePlayerMovement.MoveByInput = false; // Disable input movement during dive

        Vector3 forward = animator.gameObject.transform.forward;

        forward.y += diveDirectionY;
        m_Rigidbody.AddForce(forward * diveForce, ForceMode.Impulse);

    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Vector3 NormalVelocity =  m_Rigidbody.linearVelocity.normalized * 2f;
        NormalVelocity.y += -diveDirectionY;
        m_Rigidbody.linearVelocity = NormalVelocity;
        m_BasePlayerMovement.MoveByInput = true; // Disable input movement during dive

        // Reset any necessary parameters or states when exiting the Dive state.
    }
}
