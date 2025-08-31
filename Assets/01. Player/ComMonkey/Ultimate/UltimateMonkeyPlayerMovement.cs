using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.Playables;
using System;
using NUnit.Framework.Internal;
using UnityEngine.WSA;
using UnityEngine.Animations.Rigging;
using Kyu_BT;
using System.Collections.Generic;

public class UltimateMonkeyPlayerMovement : BasePlayerMovement
{
    [SerializeField] GameObject target;
    [SerializeField] UltimateMonkeyType monkeyType = UltimateMonkeyType.TYPE_NORMAL;
    public enum UltimateMonkeyType
    {
        TYPE_NORMAL,
        TYPE_SHOW
    }


    protected override void Start()
    {
        base.Start();
        m_CourtPosition = IPlayerInfo.CourtPosition.COURT_END;
        this.gameObject.SetActive(false);
        base.OnEnable();
    }
    public void OnDestroy()
    {
        base.OnDisable();
    }
    protected override void OnDisable()
    {
        //base.OnDisable();
    }
    protected override void OnEnable()
    {
        //base.OnEnable();
    }


    public override void OnAttackSkill(InputValue value)
    {
        if (!m_isMoveByInput)
        {
            return;
        }
    }
    protected override void Update()
    {
        base.Update();
    }
    public void LateUpdate()
    {
        GameManager.GetInstance().ConfineObjectPosition(this.gameObject, out bool yClamped, out bool zClamped);
    }
    public override void OnDefenceSkill(InputValue value)
    {
        if (!m_isMoveByInput && value.isPressed)
        {
            return;
        }
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
        if (playerType != IPlayerInfo.PlayerType.Monkey)
            return;
        if (monkeyType == UltimateMonkeyType.TYPE_SHOW)
        {
            this.gameObject.SetActive(true);
        }
    }
    public override void OnEndCutscene(IPlayerInfo.PlayerType playerType, IPlayerInfo.CourtPosition courtPosition)
    {
        m_isMoveByInput = true;
        if (playerType != IPlayerInfo.PlayerType.Monkey)
            return;
        if (monkeyType != UltimateMonkeyType.TYPE_SHOW)
        {
            this.gameObject.SetActive(true);
        }
        else
        {
            this.gameObject.SetActive(false);
        }

    }

    public override void OnRoundStart()
    {
        m_isMoveByInput = true;
    }
    public override void OnRoundEnd()
    {
        SetTransformToRoundStart();
        this.gameObject.SetActive(false);

        m_isMoveByInput = false;
        m_Rigidbody.linearVelocity = Vector3.zero;
        m_Rigidbody.angularVelocity = Vector3.zero;

    }

    protected override void FixedUpdate()
    {        // 입력값 기반으로 목표 속도 계산
        Vector3 desiredVelocity = m_Movement * walkSpeed;

        // 중력 영향을 유지하려면 기존 y값은 그대로 둠
        desiredVelocity.y = m_Rigidbody.linearVelocity.y;

        // Rigidbody의 velocity 직접 세팅
        m_Rigidbody.linearVelocity = desiredVelocity;

        float horizontal = m_InputVector.x;
        float vertical = m_InputVector.y;

        if (!m_isMoveByInput)
        {
            // 입력이 비활성화된 상태에서는 이동하지 않음
            return;
        }

        // 카메라 기준으로 이동 방향 계산
        Vector3 cameraForward = GetCameraRelativeMovement(horizontal, vertical);

        // 이동 입력 벡터 계산
        float inputMagnitude = new Vector2(horizontal, vertical).magnitude;

        // 방향만 필요한 m_Movement는 정규화
        m_Movement = cameraForward.normalized;

        // float currentSpeed = m_IsRunning ? runSpeed : walkSpeed;
        float currentSpeed = walkSpeed;

        // 애니메이터에 전달할 속도 (입력 강도에 따라)
        float appliedSpeed = inputMagnitude * currentSpeed;

        SetCurrentLocomotionState(appliedSpeed);

        // 첫 입력이 있었고, 입력 크기가 충분할 때만 회전
        if (hasReceivedInput && inputMagnitude > 0.01f)
        {
            Vector3 desiredForward = Vector3.RotateTowards(transform.forward, m_Movement, turnSpeed * Time.fixedDeltaTime, 0f);
            m_Rotation = Quaternion.LookRotation(desiredForward);
        }
        else
        {
            m_Rotation = transform.rotation;
        }

        if (m_InputVector.magnitude > 0.1f && isGrounded && (m_eLocomotionState != IdleWalkRunEnum.Swim))
        {
            footstepTimer += Time.deltaTime;
            if (footstepTimer > footstepInterval)
            {
                // 왼발/오른발 번갈아가며
                SpawnFootstepEffect((int)(Time.time * 2) % 2 == 0);
                footstepTimer = 0f;
            }
        }
        else
        {
            footstepTimer = 0f;
        }


        OnPlayerMoveVelocity();
        SetAnimatorParameters(inputMagnitude);
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
