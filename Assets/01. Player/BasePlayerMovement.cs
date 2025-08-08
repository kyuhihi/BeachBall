using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class BasePlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float turnSpeed = 20f;
    private float walkSpeed = 7f;
    // public float runSpeed = 5f;
    [SerializeField] private float dashSpeed = 100f; // 대시 속도
    [SerializeField] private string ballTag = "Ball"; // 볼 태그명
    private Transform ballTransform;

    private Vector3 dashTargetPosition;
    private bool isDashingToBall = false;
    private float dashArriveDistance = 0.5f; // 도착 판정 거리

    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float groundCheckDistance = 0.1f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer = 1;
    [SerializeField] private Transform groundCheck;


    [SerializeField]
    private Animator m_Animator;
    private Rigidbody m_Rigidbody;

    private TrailRenderer m_TrailRenderer;

    private Vector3 m_Movement;
    private Quaternion m_Rotation = Quaternion.identity;
    private Vector2 m_InputVector = Vector2.zero;
    // private bool m_IsRunning = false;

    private bool m_IsJumping = false;
    private bool isGrounded;

    public enum IdleWalkRunEnum
    {
        Idle = 0,
        Walk = 2,
        // Run = 2
    }
    private IdleWalkRunEnum m_eLocomotionState = IdleWalkRunEnum.Idle;

    // 첫 입력 추적용 변수 추가
    private bool hasReceivedInput = false;

    
    [Header("Individual Key Tracking")]
    private bool isLeftPressed = false;
    private bool isRightPressed = false;
    private bool isUpPressed = false;
    private bool isDownPressed = false;

    private bool m_isMoveByInput = true;
    public bool MoveByInput
    {
        get => m_isMoveByInput;
        set => m_isMoveByInput = value;
    }

    private float leftPressedTime = -1f;
    private float rightPressedTime = -1f;
    private float upPressedTime = -1f;
    private float downPressedTime = -1f;


    void Awake()
    {
        jumpForce = 10f;
        dashSpeed = 100f; // 대시 속도

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

    void Start()
    {
        // 필요한 초기화 코드
    }

    void Update()
    {
        CheckGrounded();
        HandleJump();

        // 우선순위 기반 입력 계산 및 적용
        UpdatePriorityInput();

        // // F1 키로 뒤로가기 토글 (임시)
        // if (Keyboard.current.f1Key.wasPressedThisFrame)
        // {
        //     m_IsBackGo = !m_IsBackGo;
        // }
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


    private void UpdatePriorityInput()
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
    private float GetHorizontalInput()
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

    private float GetVerticalInput()
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
        if (value.isPressed)
        {
            // Debug.Log("Dashing to ball...");

            GameObject ballObj = GameObject.FindWithTag(ballTag);
            if (ballObj != null)
            {
                ballTransform = ballObj.transform;
                dashTargetPosition = ballTransform.position;
                dashTargetPosition.y = transform.position.y; // 수평면만 이동
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
    {//스매시, 다이빙 동시처리.
        if (!m_isMoveByInput)
            return;
        if (!isGrounded)
                m_Animator.SetTrigger("Smash");
            else
            {

                m_Animator.SetTrigger("Diving");
            }
    }

    public void OnJump(InputValue value)
    {
        // Debug.Log("Jump Input: " + value.isPressed);

        if (value.isPressed && isGrounded)
        {
            OnJumpInput(value.isPressed);
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



    void OnFootstep()
    {
        // Debug.Log("발소리 재생");
    }

    // void FixedUpdate()
    // {
    //     float horizontal = m_InputVector.x;
    //     float vertical = m_InputVector.y;

    //     // 이동 입력 벡터 계산
    //     Vector3 rawMovement = new Vector3(horizontal, 0f, vertical);
    //     float inputMagnitude = rawMovement.magnitude;
    //     // 방향만 필요한 m_Movement는 정규화
    //     m_Movement = rawMovement.normalized;

    //     float currentSpeed = m_IsRunning ? runSpeed : walkSpeed;

    //     // 애니메이터에 전달할 속도 (입력 강도에 따라)
    //     float appliedSpeed = inputMagnitude * currentSpeed;

    //     SetCurrentLocomotionState(appliedSpeed);

    //     if (inputMagnitude > 0.01f)
    //     {
    //         Vector3 desiredForward = Vector3.RotateTowards(transform.forward, m_Movement, turnSpeed * Time.deltaTime, 0f);
    //         m_Rotation = Quaternion.LookRotation(desiredForward);
    //     }

    //     OnPlayerMove();
    //     SetAnimatorParameters(inputMagnitude);
    // }
        
    IEnumerator DisableTrailAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        m_TrailRenderer.enabled = false;
    }

    private void HandleDashToBall()
    {
        Vector3 toTarget = dashTargetPosition - transform.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;

        if (distance < dashArriveDistance)
        {
            // 도착!
            isDashingToBall = false;
            // m_TrailRenderer.Clear();
            //m_TrailRenderer.enabled = false;

            StartCoroutine(DisableTrailAfterDelay(m_TrailRenderer.time)); // 트레일이 자연스럽게 사라지게   

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
            m_Rotation = Quaternion.LookRotation(moveDirection);
            m_Rigidbody.MoveRotation(m_Rotation);
        }

        SetCurrentLocomotionState(currentToDashSpeed);
        SetAnimatorParameters(1f);
    }

    void FixedUpdate()
    {
        float horizontal = m_InputVector.x;
        float vertical = m_InputVector.y;

        // Vector3 moveDirection;

        // if (isDashingToBall && ballTransform != null)
        // {
        //             // 볼 방향 벡터 계산 (수평면만)
        //     Vector3 toBall = ballTransform.position - transform.position;
        //     toBall.y = 0f;
        //     float distance = toBall.magnitude;
        //     moveDirection = toBall.normalized;
        //     m_Movement = moveDirection;

        //     // 거리에 따라 속도 보간 (가까울수록 느려짐)
        //     float minSpeed = 2f; // 최소 속도
        //     float maxSpeed = dashSpeed; // 최대 속도
        //     float slowDownDistance = 3f; // 이 거리 이내로 들어오면 감속 시작

        //     float t = Mathf.Clamp01(distance / slowDownDistance);
        //     float currentToDashSpeed = Mathf.Lerp(minSpeed, maxSpeed, t);

        //     // 빠르게 볼 방향으로 이동
        //     m_Rigidbody.MovePosition(m_Rigidbody.position + moveDirection * currentToDashSpeed * Time.fixedDeltaTime);

        //     // 회전도 볼 방향으로
        //     if (moveDirection.sqrMagnitude > 0.01f)
        //     {
        //         m_Rotation = Quaternion.LookRotation(moveDirection);
        //         m_Rigidbody.MoveRotation(m_Rotation);
        //     }

        //     SetCurrentLocomotionState(currentToDashSpeed);
        //     SetAnimatorParameters(1f);
        //     return; // 아래 일반 이동 로직은 건너뜀
        // }
        // else
        // {
        //     isDashingToBall = false; // 대시가 끝나면 플래그 초기화
        // }

        if(!m_isMoveByInput)
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

        OnPlayerMove();
        SetAnimatorParameters(inputMagnitude);
    }   

    // 카메라 기준 이동 방향 계산 메서드 추가
    private Vector3 GetCameraRelativeMovement(float horizontal, float vertical)
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

    private void SetAnimatorParameters(float inputMagnitude)
    {
        if (m_Animator == null) return;

        m_Animator.SetInteger("IdleWalkRunEnum", (int)m_eLocomotionState);
        m_Animator.SetBool("Jump", m_IsJumping); // Jump 상태 추가
        m_Animator.SetBool("IsGrounded", isGrounded); // 추가로 Ground 상태도

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

    private float GetLocalMoveFromWorld()
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

    private Vector2 Get8DirectionVector(float angle)
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

    private void SetCurrentLocomotionState(float appliedSpeed)
    {

        // if (Mathf.Abs(appliedSpeed - runSpeed) < 0.01f)
        // {
        //     m_eLocomotionState = IdleWalkRunEnum.Run;
        // }
        // else if (Mathf.Abs(appliedSpeed - walkSpeed) < 0.01f)
        // {
        //     m_eLocomotionState = IdleWalkRunEnum.Walk;
        // }
        // else
        // {
        //     m_eLocomotionState = IdleWalkRunEnum.Idle;
        // }

        // 입력과 달리기 상태를 직접 확인하는 방식으로 변경
        if (m_InputVector.magnitude < 0.01f)
        {
            m_eLocomotionState = IdleWalkRunEnum.Idle;
        }
        else
        {
            m_eLocomotionState = IdleWalkRunEnum.Walk;
        }
    }

    // void OnPlayerMove()
    // {
    //     float currentSpeed = m_IsRunning ? runSpeed : walkSpeed;

    //     m_Rigidbody.MovePosition(m_Rigidbody.position + m_Movement * currentSpeed * Time.deltaTime);
        
    //     if (!m_IsBackGo)
    //     {
    //         m_Rigidbody.MoveRotation(m_Rotation);
    //     }
    // }

    void OnPlayerMove()
    {
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

    private void HandleJump()
    {
        // OnJump에서 처리하므로 비워둠
    }

    private void CheckGrounded()
    {
        bool wasGrounded = isGrounded;
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckDistance, groundLayer);

        // Debug.Log($"Checking Grounded: wasGrounded = {wasGrounded}, isGrounded = {isGrounded}");

        // 착지했을 때 점프 상태 해제
        if (!wasGrounded && isGrounded && m_IsJumping)
        {
            OnJumpInput(false); // 점프 상태 해제 (Animator도 함께 업데이트됨)
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckDistance);
        }
    }
}