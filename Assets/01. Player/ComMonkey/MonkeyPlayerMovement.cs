using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.Playables;


public class MonkeyPlayerMovement : BasePlayerMovement
{
    protected override void Start()
    {
        base.Start();
        m_PlayableDirector = GetComponent<PlayableDirector>();
        m_PlayerType = IPlayerInfo.PlayerType.Monkey;
        m_PlayerDefaultColor = Color.rosyBrown;
        PlayerUIManager.GetInstance().SetPlayerInfoInUI(this);

    }


    public override void OnAttackSkill(InputValue value)
    {
        if (!m_isMoveByInput)
        {
            return;
        }

        if (value.isPressed)
        {
            m_Animator.SetTrigger("AttackSkill");
        }
    }


    protected override void Update()
    {
        base.Update();
    }

    public override void OnDefenceSkill(InputValue value)
    {
        if (!m_isMoveByInput && value.isPressed)
        {
            return;
        }

        m_Animator.SetBool("DefenceSkill", value.isPressed);


    }

    public override void OnUltimateSkill(InputValue value)
    {
        if (!m_isMoveByInput)
        {
            return;
        }
        if (!PlayerUIManager.GetInstance().UseAbility(IUIInfo.UIType.UltimateBar, m_CourtPosition))
        {
            return;
        }


    }
    public override void OnStartCutScene(IPlayerInfo.PlayerType playerType, IPlayerInfo.CourtPosition courtPosition)
    {
        m_isMoveByInput = false;



    }
    public override void OnEndCutscene(IPlayerInfo.PlayerType playerType, IPlayerInfo.CourtPosition courtPosition)
    {
        m_isMoveByInput = true;
    }//이거 오버라이딩해야함.


    protected override void FixedUpdate()
    {
        base.FixedUpdate();
    }
    public bool IsGroundedForAI() => isGrounded;

    public void AIJump(bool allowDouble = true)
    {
        if (!m_isMoveByInput) return;
        if (m_eLocomotionState == IdleWalkRunEnum.Swim) return;

        // 1단 점프
        if (isGrounded)
        {
            OnJumpInput(true);
            m_Rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            return;
        }

        // 더블 점프
        if (allowDouble && !m_IsDoubleJumping && !isGrounded)
        {
            OnDoubleJumpInput(true);
            Vector3 vel = m_Rigidbody.linearVelocity;
            vel.y = 0f;
            m_Rigidbody.linearVelocity = vel;
            m_Rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

}
