using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.Playables;
using System.Threading;
using System.Collections.Generic;
using NUnit.Framework;

public class BasePlayerMovement : MonoBehaviour , IPlayerInfo, ICutSceneListener, IResetAbleListener
{
    public IPlayerInfo.PlayerType m_PlayerType { get; set; }
    public Color m_PlayerDefaultColor { get; set; }
    public IPlayerInfo.CourtPosition m_CourtPosition { get; set; }

    [Header("Movement Settings")]
    public float turnSpeed = 20f;
    protected float walkSpeed = 7f;
    protected float baseWalkSpeed = 7f;
    protected float swimMaxSpeed = 0f;

    protected bool isUltimateSkillActiving = false;

    // public float runSpeed = 5f;
    [SerializeField] protected float dashSpeed = 100f; // 대시 속도
    [SerializeField] protected string ballTag = "Ball"; // 볼 태그명
    protected Transform ballTransform;

    protected Vector3 dashTargetPosition;
    protected bool isDashingToBall = false;
    protected bool isSwimming = false;

    public bool IsSwimming
    {
        get => isSwimming;
        set => isSwimming = value;
    }
    protected float dashArriveDistance = 0.5f; // 도착 판정 거리

    [SerializeField] protected float jumpForce = 10f;
    protected float groundCheckDistance = 0.5f;

    [Header("Ground Check")]
    [SerializeField] protected LayerMask groundLayer = 1;
    [SerializeField] protected Transform groundCheck;

    [Header("ParticleSystem")]
    [SerializeField] protected ParticleSystem doubleJumpEffectPrefab;
    [SerializeField] protected ParticleSystem footstepCloudPrefab;
    [SerializeField] protected ParticleSystem swimFootstepCloudPrefab;
    [SerializeField] protected ParticleSystem bubbleMousePrefab;
    [SerializeField] protected Transform bubbleMouseTransform;
    [SerializeField] protected Transform leftFootTransform;
    [SerializeField] protected Transform rightFootTransform;


    [Header("Animator")]
    [SerializeField]
    protected Animator m_Animator;
    protected Rigidbody m_Rigidbody;

    protected TrailRenderer m_TrailRenderer;

    protected Vector3 m_Movement;
    protected Quaternion m_Rotation = Quaternion.identity;
    protected Vector2 m_InputVector = Vector2.zero;
    // private bool m_IsRunning = false;

    protected bool m_IsJumping = false;
    protected bool m_IsDoubleJumping = false;
    protected bool isGrounded;

    protected bool isStunned;
    public bool IsStunned => isStunned;


    public enum IdleWalkRunEnum
    {
        Idle = 0,
        Walk = 2,

        Swim = 3,
        // Run = 2
    }
    protected IdleWalkRunEnum m_eLocomotionState = IdleWalkRunEnum.Idle;

    // 첫 입력 추적용 변수 추가
    protected bool hasReceivedInput = false;


    [Header("Individual Key Tracking")]
    protected bool isLeftPressed = false;
    protected bool isRightPressed = false;
    protected bool isUpPressed = false;
    protected bool isDownPressed = false;
    [SerializeField]
    protected bool m_isMoveByInput = true;
    public bool MoveByInput
    {
        get => m_isMoveByInput;
        set => m_isMoveByInput = value;
    }

    private bool m_hurtTurtleUltimateSkillStun = false;

    public bool HurtTurtleUltimateSkillStun
    {
        get => m_hurtTurtleUltimateSkillStun;
        set => m_hurtTurtleUltimateSkillStun = value;
    }

    protected float leftPressedTime = -1f;
    protected float rightPressedTime = -1f;
    protected float upPressedTime = -1f;
    protected float downPressedTime = -1f;


    protected float footstepTimer = 0f;
    protected float footstepInterval = 0.3f; // 발자국 이펙트 간격   

    protected float swimfootstepTimer = 0f;



    protected float swimfootstepMinInterval = 0.1f; // 수영 중 발자국 이펙트 최소 간격
    protected float swimfootstepMaxInterval = 1f; // 수영 중 발자국 이펙트 최대 간격

    protected float swimfootstepInterval = 1f;

    protected float bubblemouseTimer = 0f;
    protected float bubblemouseInterval = 1f; // 수영 중 발자국 이펙트 간격

    protected PlayableDirector m_PlayableDirector;

    private bool _cutsceneSubscribed = false;

    protected bool m_isTitleScene = false;

    // 컷신 등에서 발소리/이펙트 차단용
    protected bool muteFootSfx = false;

    protected Vector3 m_StartPosition = Vector3.zero;
    protected Vector3 m_StartRotationEuler = Vector3.zero;
    public virtual void OnStartCutScene(IPlayerInfo.PlayerType playerType, IPlayerInfo.CourtPosition courtPosition) { }
    public virtual void OnEndCutscene(IPlayerInfo.PlayerType playerType, IPlayerInfo.CourtPosition courtPosition){}//이거 오버라이딩해야함.
    public virtual void OnRoundStart() { Debug.Log("너 라운드 시작 혹시 구독만 했니? 오버라이딩하렴."); }
    public virtual void OnRoundEnd() { Debug.Log("너 라운드 끝 혹시 구독만 했니? 오버라이딩하렴."); }
    public virtual void AddResetCall()
    {
        Signals.RoundResetAble.AddStart(OnRoundStart);
        Signals.RoundResetAble.AddEnd(OnRoundEnd);
    }
    public virtual void RemoveResetCall()
    {
        Signals.RoundResetAble.RemoveStart(OnRoundStart);
        Signals.RoundResetAble.RemoveEnd(OnRoundEnd);
    }
    protected void SetTransformToRoundStart() { gameObject.transform.position = m_StartPosition; gameObject.transform.rotation = Quaternion.Euler(m_StartRotationEuler); }

    protected virtual void OnInterrupted()
    {
        // 대시 중이면 즉시 종료
        if (isDashingToBall)
            EndDashCall();

        // 점프/더블점프 상태 해제
        m_IsJumping = false;
        m_IsDoubleJumping = false;

        // 발소리 타이머 리셋
        footstepTimer = 0f;
        swimfootstepTimer = 0f;

        // 이동 입력 초기화
        m_InputVector = Vector2.zero;

        // 애니메이터 상태 초기화
        if (m_Animator != null)
        {
            m_Animator.SetBool("Jump", false);
            m_Animator.SetBool("DoubleJump", false);
            m_Animator.SetBool("IsDashing", false);
            m_Animator.SetBool("IsGrounded", isGrounded);
        }

        // 필요하면 추가적으로 공통 이펙트/코루틴 정리
    }


    // protected System.Collections.IEnumerator Co_RegisterPlayerInfoWhenReady()
    // {
    //     // GameSceneManager 초기화 완료 + PlayerUIManager 살아날 때까지 대기
    //     while (this && isActiveAndEnabled &&
    //            (PlayerUIManager.GetInstance() == null || !GameSceneManager.IsInitialized))
    //         yield return null;

    //     if (!this || !isActiveAndEnabled) yield break;

    //     var ui = PlayerUIManager.GetInstance();
    //     if (ui != null)
    //         ui.SetPlayerInfoInUI(this);
    // }

    protected virtual void Start()
    {
        baseWalkSpeed = walkSpeed;
        m_StartPosition = gameObject.transform.position;
        m_StartRotationEuler = Vector3.zero;
        if (gameObject.transform.position.z < 0.0f)
        {
            m_CourtPosition = IPlayerInfo.CourtPosition.COURT_RIGHT;

        }
        else
        {
            m_StartRotationEuler.y = 180.0f;
            m_CourtPosition = IPlayerInfo.CourtPosition.COURT_LEFT;
        }
    }

    protected virtual void OnEnable()
    {
        // 명명된 핸들러로 구독
        if (!_cutsceneSubscribed)
        {
            AddResetCall();
            Signals.Cutscene.AddStart(OnStartCutScene);
            Signals.Cutscene.AddEnd(OnEndCutscene);
            _cutsceneSubscribed = true;

        }
    }

    protected virtual void OnDisable()
    {
        // 명명된 핸들러로 해제(람다 쓰지 말 것)
        if (_cutsceneSubscribed)
        {
            RemoveResetCall();
            Signals.Cutscene.RemoveStart(OnStartCutScene);
            Signals.Cutscene.RemoveEnd(OnEndCutscene);
            _cutsceneSubscribed = false;
        }
    }

 
    protected void Awake()
    {
        jumpForce = 10f;
        dashSpeed = 100f; // 대시 속도
        swimMaxSpeed = walkSpeed * 2f;

        if (m_TrailRenderer == null)
            m_TrailRenderer = GetComponent<TrailRenderer>();

        // 시작할 때는 비활성화
        if (m_TrailRenderer != null)
        {
            m_TrailRenderer.enabled = false;
        }

        if (m_Animator == null)
            m_Animator = GetComponent<Animator>();
        m_Rigidbody = GetComponent<Rigidbody>();



        // Rigidbody 설정
        // m_Rigidbody.freezeRotation = true;
        // m_Rigidbody.drag = 5f;

        // Ground Check가 없으면 자동으로 생성
        if (groundCheck == null)
        {
            GameObject groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.SetParent(transform);
            groundCheckObj.transform.localPosition = new Vector3(0, -0.1f, 0);
            groundCheck = groundCheckObj.transform;
        }
    }



    protected virtual void Update()
    {
        CheckGrounded();
        HandleJump();

        // 우선순위 기반 입력 계산 및 적용
        UpdatePriorityInput();
        //규현 대쉬막아라 진짜 
    }

    public void OnMoveInput(Vector2 moveInput)
    {
        m_InputVector = moveInput;

        // 첫 입력이 들어왔을 때 플래그 설정
        if (moveInput.magnitude > 0.01f && !hasReceivedInput)
        {
            hasReceivedInput = true; 
        }
    }

    protected void SpawnFootstepEffect(bool isLeft)
    {
        if (muteFootSfx) return;

        if (footstepCloudPrefab == null) return;

        Transform foot = isLeft ? leftFootTransform : rightFootTransform;
        if (foot == null) return;

        ParticleSystem effect = Instantiate(footstepCloudPrefab, foot.position, Quaternion.identity);

        AudioManager.Instance?.PlayFootstep(foot.position, swim: false);

        Destroy(effect.gameObject, 1f); // 1초 후 자동 삭제
    }

    protected void SpawnSwimstepEffect(bool isLeft)
    {
        if (swimFootstepCloudPrefab == null) return;

        Transform foot = isLeft ? leftFootTransform : rightFootTransform;
        if (foot == null) return;

        ParticleSystem effect = Instantiate(swimFootstepCloudPrefab, foot.position, transform.rotation);

        Destroy(effect.gameObject, 1f); // 1초 후 자동 삭제
    }

    protected void SpawnBubbleMouseEffect()
    {
        if (bubbleMousePrefab == null) return;
        ParticleSystem effect = Instantiate(bubbleMousePrefab, bubbleMouseTransform.position, Quaternion.identity);
        Destroy(effect.gameObject, 2f); // 2초 후 자동 삭제
    }


    protected void UpdatePriorityInput()
    {
        Vector2 priorityInput = new Vector2(GetHorizontalInput(), GetVerticalInput());

        OnMoveInput(priorityInput);

        // // 디버그 로그
        // if (priorityInput.magnitude > 0.01f)
        // {
        //     Debug.Log($"Priority Input: {priorityInput}, L:{isLeftPressed}, R:{isRightPressed}, U:{isUpPressed}, D:{isDownPressed}");
        // }
    }
    public void OnMoveLeft(InputValue value)
    {
        if (value.isPressed)
        {
            isLeftPressed = true;
            leftPressedTime = Time.time;
            //Debug.Log($"Left pressed at {leftPressedTime}");
        }
        else
        {
            isLeftPressed = false;
            //Debug.Log("Left released");
        }
    }

    public void OnMoveRight(InputValue value)
    {
        if (value.isPressed)
        {
            isRightPressed = true;
            rightPressedTime = Time.time;
            //Debug.Log($"Right pressed at {rightPressedTime}");
        }
        else
        {
            isRightPressed = false;
            //Debug.Log("Right released");
        }
    }

    public void OnMoveUp(InputValue value)
    {
        if (value.isPressed)
        {
            isUpPressed = true;
            upPressedTime = Time.time;
        }
        else
        {
            isUpPressed = false;
        }
    }

    public void OnMoveDown(InputValue value)
    {
        if (value.isPressed)
        {
            isDownPressed = true;
            downPressedTime = Time.time;
        }
        else
        {
            isDownPressed = false;
        }
    }


    // 우선순위 기반 입력 계산
    protected float GetHorizontalInput()
    {
        if (isLeftPressed && isRightPressed)
        {
            // 최근에 누른 방향 우선
            return (rightPressedTime > leftPressedTime) ? 1f : -1f;
        }
        else if (isLeftPressed)
        {
            return -1f;
        }
        else if (isRightPressed)
        {
            return 1f;
        }

        return 0f;
    }

    protected float GetVerticalInput()
    {
        if (isUpPressed && isDownPressed)
        {
            // 최근에 누른 방향 우선
            return (upPressedTime > downPressedTime) ? 1f : -1f;
        }
        else if (isUpPressed)
        {
            return 1f;
        }
        else if (isDownPressed)
        {
            return -1f;
        }

        return 0f;
    }



    public void OnDash(InputValue value)
    {
        if(m_isTitleScene)
        {
            Debug.Log("타이틀 씬에서는 싸움만 하거라");
            // 타이틀 씬에서는 대쉬 사용 불가
            return;
        }
        if (!m_isMoveByInput)
            return;

        if(m_eLocomotionState == IdleWalkRunEnum.Swim)
        {
            return;   // 수영 중 대시
        }
        PlayerUIManager uIManagerInstance = PlayerUIManager.GetInstance();
        if (!uIManagerInstance.UseAbility(IUIInfo.UIType.DashBar, m_CourtPosition))
        {
            return;
        }

        if (value.isPressed)
        {
            // Debug.Log("Dashing to ball...");

            GameObject ballObj = GameObject.FindWithTag(ballTag);
            if (ballObj != null)
            {
                // Ball 컴포넌트 가져오기
                Ball ball = ballObj.GetComponent<Ball>();
                if (ball != null)
                {
                    // LandSpotParticle 프로퍼티로 ParticleSystem 얻기
                    ParticleSystem landSpotParticle = ball.LandSpotParticle;

                    // landSpotParticle을 원하는 대로 사용
                    ballTransform = landSpotParticle.transform;
                    dashTargetPosition = ballTransform.position;
                }

                isDashingToBall = true;
                m_TrailRenderer.enabled = true;
            }
            else
            {
                Debug.LogWarning("Ball not found with tag: " + ballTag);
                isDashingToBall = false;
            }
        }
    }
    public void OnSmash(InputValue value)
    {

        // 막아야 하는가 마는가 고민
        // if (!m_isMoveByInput)
        //     return;

        if (m_eLocomotionState == IdleWalkRunEnum.Swim)
        {
            isSwimSmashPressed = value.isPressed;
            return;
        }



        if (value.isPressed)
        {
            if (!isGrounded)
                m_Animator.SetTrigger("Smash");
            else
            {

                m_Animator.SetTrigger("Diving");
            }
        }

    }

    // 1. 상태 변수 추가
    private bool isSwimJumpPressed = false;
    private bool isSwimSmashPressed = false;

    public void OnJump(InputValue value)
    {
        // Debug.Log("Jump Input: " + value.isPressed);
        if (!m_isMoveByInput)
        {
            return;
        }
        
        if (m_eLocomotionState == IdleWalkRunEnum.Swim)
        {
            isSwimJumpPressed = value.isPressed;
            Debug.Log("Swim Jump Pressed: " + isSwimJumpPressed);
            return;
        }

        if (value.isPressed && isGrounded)
        {
            OnJumpInput(value.isPressed);
            m_Rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
        else if (value.isPressed && !m_IsDoubleJumping && !isGrounded)
        {
            OnDoubleJumpInput(value.isPressed);

            // 기존 y속도를 0으로 만든다
            Vector3 velocity = m_Rigidbody.linearVelocity;
            velocity.y = 0f;
            m_Rigidbody.linearVelocity = velocity;

            // 새로운 점프 힘을 적용
            m_Rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

    }

    public void OnJumpInput(bool jumpInput)
    {
        m_IsJumping = jumpInput;
        // Animator 파라미터 업데이트
        if (m_Animator != null)
        {
            m_Animator.SetBool("Jump", m_IsJumping);
            // 또는 Trigger를 사용한다면:
            // if (m_IsJumping) m_Animator.SetTrigger("Jump");
        }
    }


    public void OnDoubleJumpInput(bool doubleJumpInput)
    {

        m_IsDoubleJumping = doubleJumpInput;
        // Animator 파라미터 업데이트
        if (m_Animator != null)
        {
            m_Animator.SetBool("DoubleJump", m_IsDoubleJumping);
        }

        // 더블점프 시작할 때 이펙트 생성
        if (doubleJumpInput && doubleJumpEffectPrefab != null)
        {
            // 캐릭터 발밑 위치 계산 (y값을 약간 내리면 더 자연스러움)
            Vector3 effectPos = transform.position + Vector3.down * 0.5f;
            ParticleSystem effect = Instantiate(doubleJumpEffectPrefab, effectPos, Quaternion.identity);
            Destroy(effect.gameObject, 0.5f); // 0.5초 후 자동 삭제
        }
    }

    public virtual void OnAttackSkill(InputValue value)
    {
        // 기본 동작(없거나, 공통 이펙트 등)
        Debug.Log("AttackSkill (Base): 아무 동작 없음");
    }

    public virtual void OnDefenceSkill(InputValue value)
    {
        // 기본 동작(없거나, 공통 이펙트 등)
        Debug.Log("DefenceSkill (Base): 아무 동작 없음");
    }

    public virtual void OnUltimateSkill(InputValue value)
    {
        // 기본 동작(없거나, 공통 이펙트 등)
        Debug.Log("UltimateSkill (Base): 아무 동작 없음");
    }



    protected void OnFootstep()
    {
        // Debug.Log("발소리 재생");
    }
        
    protected IEnumerator DisableTrailAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        m_TrailRenderer.enabled = false;
    }

    protected void HandleDashToBall()
    {
        
        Vector3 toTarget = dashTargetPosition - transform.position;
        m_Rigidbody.linearVelocity = Vector3.zero;

        //toTarget.y = 0f;
        float distance = toTarget.magnitude;

        if (distance < dashArriveDistance)
        {
            // 도착!
            EndDashCall();

            return;
        }

        Vector3 moveDirection = toTarget.normalized;
        float minSpeed = 2f;
        float maxSpeed = dashSpeed;
        float slowDownDistance = 3f;

        float t = Mathf.Clamp01(distance / slowDownDistance);
        float currentToDashSpeed = Mathf.Lerp(minSpeed, maxSpeed, t);

        m_Rigidbody.MovePosition(m_Rigidbody.position + moveDirection * currentToDashSpeed * Time.fixedDeltaTime);

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion rotation = Quaternion.LookRotation(moveDirection);

            //Debug.Log($"Rotation: {rotation.eulerAngles}");
            m_Animator.SetFloat("RotationY", rotation.eulerAngles.y, 0.01f, Time.deltaTime);

        }


        SetCurrentLocomotionState(currentToDashSpeed);
        SetAnimatorParameters(1f);
    }

    public void EndDashCall()
    {
        isDashingToBall = false;

        StartCoroutine(DisableTrailAfterDelay(m_TrailRenderer.time)); // 트레일이 자연스럽게 사라지게   
    }

    protected virtual void FixedUpdate()
    {
        float horizontal = m_InputVector.x;
        float vertical = m_InputVector.y;

        if (!m_isMoveByInput)
        {
            // 입력이 비활성화된 상태에서는 이동하지 않음
            return;
        }
        if (isDashingToBall)
        {
            HandleDashToBall();
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
            // 첫 입력 전이거나 입력이 없으면 현재 회전 유지
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

        if (m_PlayerType == IPlayerInfo.PlayerType.Monkey && isGrounded && (m_eLocomotionState != IdleWalkRunEnum.Swim))
        {
            footstepTimer += Time.deltaTime;
            if (footstepTimer > footstepInterval)
            {
                // 왼발/오른발 번갈아가며
                SpawnFootstepEffect((int)(Time.time * 2) % 2 == 0);
                footstepTimer = 0f;
            }
        }

        // *** Swim 상태에서 위/아래 이동 처리 ***
        if (m_eLocomotionState == IdleWalkRunEnum.Swim)
        {
            Vector3 swimMove = m_Movement * currentSpeed * Time.fixedDeltaTime;

            // 위로 이동
            if (isSwimJumpPressed)
                swimMove += Vector3.up * 2f * Time.fixedDeltaTime; // 상승 속도 조절

            // 아래로 이동
            if (isSwimSmashPressed)
                swimMove += Vector3.down * 2f * Time.fixedDeltaTime; // 하강 속도 조절

            m_Rigidbody.MovePosition(m_Rigidbody.position + swimMove);

            // 회전 적용
            if (hasReceivedInput)
            {
                // 기본적으로 이동 방향을 바라보게
                Vector3 lookDir = swimMove.normalized;
                if (lookDir.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);
                    m_Rigidbody.MoveRotation(Quaternion.Slerp(m_Rigidbody.rotation, targetRot, 0.2f));
                }
                else
                {
                    m_Rigidbody.MoveRotation(m_Rotation);
                }
            }

            float speed = m_Movement.magnitude * currentSpeed;

            swimfootstepInterval = Mathf.Lerp(swimfootstepMinInterval, swimfootstepMaxInterval, 1f - Mathf.InverseLerp(0f, swimMaxSpeed, speed));
            swimfootstepTimer += Time.deltaTime;

            // Debug.Log($"Swim Footstep Interval: {currentSpeed},  {speed} , {swimfootstepInterval}, Timer: {swimfootstepTimer}");

            if (swimfootstepTimer > swimfootstepInterval)
            {
                // 왼발/오른발 번갈아가며
                SpawnSwimstepEffect((int)(Time.time * 2) % 2 == 0);
                swimfootstepTimer = 0f;
            }

            bubblemouseTimer += Time.deltaTime;
            if (bubblemouseTimer > bubblemouseInterval)
            {
                SpawnBubbleMouseEffect();
                bubblemouseTimer = 0f;
            }

            return;
        }

        OnPlayerMove();
        SetAnimatorParameters(inputMagnitude);
    }

    // 카메라 기준 이동 방향 계산 메서드 추가
    protected Vector3 GetCameraRelativeMovement(float horizontal, float vertical)
    {
        // 메인 카메라 참조
        Camera mainCamera = Camera.main;
        // 이렇게 하지말고 3인칭으로 하는 것이 좋을듯(등 뒤에 카메라)


        if (mainCamera == null)
        {
            // 카메라가 없으면 월드 좌표계 사용
            return new Vector3(horizontal, 0f, vertical);
        }

        // 카메라의 forward와 right 벡터 가져오기
        Vector3 cameraForward = mainCamera.transform.forward;
        Vector3 cameraRight = mainCamera.transform.right;

        // Y축 제거 (수평 이동만)
        cameraForward.y = 0f;
        cameraRight.y = 0f;

        // 정규화
        cameraForward.Normalize();
        cameraRight.Normalize();

        // 카메라 기준으로 이동 방향 계산
        Vector3 moveDirection = cameraForward * vertical + cameraRight * horizontal;

        return moveDirection;
    }

    protected void SetAnimatorParameters(float inputMagnitude)
    {
        if (m_Animator == null) return;

        m_Animator.SetInteger("IdleWalkRunEnum", (int)m_eLocomotionState);
        m_Animator.SetBool("Jump", m_IsJumping); // Jump 상태 추가
        m_Animator.SetBool("DoubleJump", m_IsDoubleJumping); // Double Jump 상태 추가
        m_Animator.SetBool("IsGrounded", isGrounded); // 추가로 Ground 상태도
        m_Animator.SetBool("IsDashing", isDashingToBall); // 대시 상태 추가

        float localAngle = GetLocalMoveFromWorld();
        Vector2 directionVector = Get8DirectionVector(localAngle);
        float fAnimatorSpeedX = directionVector.x * (int)m_eLocomotionState;
        float fAnimatorSpeedY = directionVector.y * (int)m_eLocomotionState;

        // m_Animator.SetBool("GoBack", m_IsBackGo);

        if (inputMagnitude > 0f)
        {
            m_Animator.SetFloat("SpeedX", fAnimatorSpeedX, 0.01f, Time.deltaTime);
            m_Animator.SetFloat("SpeedY", fAnimatorSpeedY, 0.01f, Time.deltaTime);
        }
    }

    protected float GetLocalMoveFromWorld()
    {
        if (m_Movement.sqrMagnitude < 0.0001f)
            return 0f;

        // 내적값을 이용해 각도 계산 (y축 평면상)
        Vector3 forward = transform.forward;
        Vector3 move = m_Movement;

        // y축 평면 투영
        forward.y = 0f;
        move.y = 0f;

        forward.Normalize();
        move.Normalize();

        float dot = Vector3.Dot(forward, move);
        float angle = Mathf.Acos(Mathf.Clamp(dot, -1f, 1f)) * Mathf.Rad2Deg;

        // 방향(좌/우) 판별을 위해 cross product 사용
        float cross = Vector3.Cross(forward, move).y;
        if (cross < 0)
            angle = -angle;

        return angle; // -180 ~ 180도
    }

    protected Vector2 Get8DirectionVector(float angle)
    {
        // -180 ~ 180 범위의 angle을 0~360으로 변환
        angle = (angle + 360f) % 360f;

        // 8방향 각도 구간에 따라 분기
        if (angle >= 337.5f || angle < 22.5f)
            return new Vector2(0f, 1f);         // 0도 (정면)
        else if (angle >= 22.5f && angle < 67.5f)
            return new Vector2(1f, 1f);         // 45도
        else if (angle >= 67.5f && angle < 112.5f)
            return new Vector2(1f, 0f);         // 90도 (오른쪽)
        else if (angle >= 112.5f && angle < 157.5f)
            return new Vector2(1f, -1f);        // 135도
        else if (angle >= 157.5f && angle < 202.5f)
            return new Vector2(0f, -1f);        // 180도, -180도 (뒤)
        else if (angle >= 202.5f && angle < 247.5f)
            return new Vector2(-1f, -1f);       // -135도
        else if (angle >= 247.5f && angle < 292.5f)
            return new Vector2(-1f, 0f);        // -90도 (왼쪽)
        else // angle >= 292.5f && angle < 337.5f
            return new Vector2(-1f, 1f);        // -45도
    }

    protected void SetCurrentLocomotionState(float appliedSpeed)
    {
        // 입력과 달리기 상태를 직접 확인하는 방식으로 변경
        if(m_eLocomotionState == IdleWalkRunEnum.Swim)
        {
            return;
        }

        if (m_InputVector.magnitude < 0.01f)
        {
            m_eLocomotionState = IdleWalkRunEnum.Idle;
        }
        else
        {
            m_eLocomotionState = IdleWalkRunEnum.Walk;
        }
    }

    protected void OnPlayerMove()
    {
        if (!m_isMoveByInput)
        {
            // 입력이 비활성화된 상태에서는 이동하지 않음
            return;
        }
        // float currentSpeed = m_IsRunning ? runSpeed : walkSpeed;
        float currentSpeed = walkSpeed;

        // Time.deltaTime을 Time.fixedDeltaTime으로 변경 (FixedUpdate에서 호출되므로)
        m_Rigidbody.MovePosition(m_Rigidbody.position + m_Movement * currentSpeed * Time.fixedDeltaTime);

        // 첫 입력이 있었고 뒤로가기 모드가 아닐 때만 회전 적용
        if (hasReceivedInput)
        {
            m_Rigidbody.MoveRotation(m_Rotation);
        }
    }
    protected void OnPlayerMoveVelocity()
    {
        if (!m_isMoveByInput)
        {
            // 입력이 비활성화된 상태에서는 이동하지 않음
            return;
        }
        // float currentSpeed = m_IsRunning ? runSpeed : walkSpeed;
        float currentSpeed = walkSpeed;

        // Time.deltaTime을 Time.fixedDeltaTime으로 변경 (FixedUpdate에서 호출되므로)
        Vector3 targetLinearvelocity = m_Rigidbody.linearVelocity;
        targetLinearvelocity.x = m_Movement.x * currentSpeed;
        targetLinearvelocity.z = m_Movement.z * currentSpeed;
        m_Rigidbody.linearVelocity = targetLinearvelocity;

        // 첫 입력이 있었고 뒤로가기 모드가 아닐 때만 회전 적용
        if (hasReceivedInput)
        {
            m_Rigidbody.MoveRotation(m_Rotation);
        }
    }

    protected void HandleJump()
    {
        // OnJump에서 처리하므로 비워둠
    }

    protected void CheckGrounded()
    {
        bool wasGrounded = isGrounded;
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckDistance, groundLayer);


        // OverlapSphere로 충돌체 목록 얻기
        Collider[] hits = Physics.OverlapSphere(groundCheck.position, groundCheckDistance, groundLayer);

        isGrounded = hits.Length > 0;

        // 디버그: 닿은 오브젝트 이름 출력
        if (hits.Length > 0)
        {
                foreach (var hit in hits)
                {
                }
            
        }
            // 착지했을 때 점프 상태 해제
            if (!wasGrounded && isGrounded && m_IsJumping)
            {
                OnJumpInput(false); // 점프 상태 해제 (Animator도 함께 업데이트됨)
                OnDoubleJumpInput(false);
            }
    }

    public void Stun(float duration)
    {
        // 기절 처리(이동 불가, 애니메이션 등)
        // 예시:
        m_Animator.SetTrigger("Stunned");
        MoveByInput = false;
        isStunned = true;
        OnInterrupted();
        StartCoroutine(StunCoroutine(duration));
    }

    // 이때는 거북이 등껍질 연속못던지게 하기
    public void UltimateStun(float duration)
    {
        // 기절 처리(이동 불가, 애니메이션 등)
        // 예시:
        m_Animator.SetTrigger("Stunned");
        MoveByInput = false;
        isStunned = true;

        // 대시 중이면 즉시 종료
        if (isDashingToBall)
            EndDashCall();

        // 점프/더블점프 상태 해제
        m_IsJumping = false;
        m_IsDoubleJumping = false;

        // 발소리 타이머 리셋
        footstepTimer = 0f;
        swimfootstepTimer = 0f;

        // 이동 입력 초기화
        m_InputVector = Vector2.zero;

        // 애니메이터 상태 초기화
        if (m_Animator != null)
        {
            m_Animator.SetBool("Jump", false);
            m_Animator.SetBool("DoubleJump", false);
            m_Animator.SetBool("IsDashing", false);
            m_Animator.SetBool("IsGrounded", isGrounded);
        }
        StartCoroutine(StunCoroutine(duration));
    }

    private IEnumerator StunCoroutine(float duration)
    {
        // 기절 애니메이션 등 추가 가능
        yield return new WaitForSeconds(duration);
        MoveByInput = true;
        isStunned = false;
    }

    public void SetSwimModeAfterStun(float speedMultiplier)
    {
        StartCoroutine(SwimModeAfterStunRoutine(speedMultiplier));
    }

    private IEnumerator SwimModeAfterStunRoutine(float speedMultiplier)
    {
        yield return new WaitForSeconds(10f);
        SetSwimmingMode(speedMultiplier);
    }

    public void SetSwimmingMode(float speedMultiplier)
    {
        // Swimming 상태로 전환
        // 예: isSwimming = true;
        // 속도 조정

        if (!isSwimming)
        {
            return; // 수영모드 아니면 금지
        }

        m_eLocomotionState = IdleWalkRunEnum.Swim;
        walkSpeed = walkSpeed * speedMultiplier;
        m_Rigidbody.isKinematic = true;
        m_Animator.SetBool("IsSwimming", true);
        m_Animator.SetTrigger("Swimming");
        // 애니메이션 등 추가
    }

    protected void RestoreGravity(bool value)
    {
        var rb  = GetComponent<Rigidbody>();
        if (rb) rb.useGravity = value;
    }

    public void SetResetMode()
    {
        Debug.Log("Reset Mode로 전환");
        // Swimming 상태로 전환
        // 예: isSwimming = true;
        // 속도 조정
        m_eLocomotionState = IdleWalkRunEnum.Idle;
        walkSpeed = baseWalkSpeed;
        isSwimming = false;
        isStunned = false;
        m_Animator.SetBool("IsSwimming", false);
        m_Animator.ResetTrigger("Stunned");
        HurtTurtleUltimateSkillStun = false;
        muteFootSfx = true;
        footstepTimer = 0f;
        swimfootstepTimer = 0f;
        m_Rigidbody.isKinematic = false;
        isUltimateSkillActiving = false;
        
        RestoreGravity(true);
    }

    protected void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckDistance);
        }
    }
}