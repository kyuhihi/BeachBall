using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class BasePlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float turnSpeed = 20f;
    public float walkSpeed = 2f;
    public float runSpeed = 5f;
    [SerializeField] private float jumpForce = 2f;
    [SerializeField] private float groundCheckDistance = 0.1f;
    
    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer = 1;
    [SerializeField] private Transform groundCheck;
    

    
    private Animator m_Animator;
    private Rigidbody m_Rigidbody;

    private Vector3 m_Movement;
    private Quaternion m_Rotation = Quaternion.identity;
    private Vector2 m_InputVector = Vector2.zero;
    private bool m_IsRunning = false;

    private bool m_IsJumping = false;
    private bool isGrounded;

    public enum IdleWalkRunEnum
    {
        Idle = 0,
        Walk = 1,
        Run = 2
    }
    private IdleWalkRunEnum m_eLocomotionState = IdleWalkRunEnum.Idle;
    
    [SerializeField]
    private bool m_IsBackGo = false;

    void Awake()
    {
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
        
        // F1 키로 뒤로가기 토글 (임시)
        if (Keyboard.current.f1Key.wasPressedThisFrame)
        {
            m_IsBackGo = !m_IsBackGo;
        }
    }

    // Input System callbacks
    public void OnMove(InputValue value)
    {
        OnMoveInput(value.Get<Vector2>());
    }

    public void OnSprint(InputValue value)
    {
        OnSprintInput(value.isPressed);
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
    // 직접 입력 메서드들
    public void OnMoveInput(Vector2 moveInput)
    {
        m_InputVector = moveInput;
    }

    public void OnSprintInput(bool sprinting)
    {
        m_IsRunning = sprinting;
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

    void FixedUpdate()
    {
        float horizontal = m_InputVector.x;
        float vertical = m_InputVector.y;

        // 이동 입력 벡터 계산
        Vector3 rawMovement = new Vector3(horizontal, 0f, vertical);
        float inputMagnitude = rawMovement.magnitude;

        // 방향만 필요한 m_Movement는 정규화
        m_Movement = rawMovement.normalized;

        float currentSpeed = m_IsRunning ? runSpeed : walkSpeed;

        // 애니메이터에 전달할 속도 (입력 강도에 따라)
        float appliedSpeed = inputMagnitude * currentSpeed;

        SetCurrentLocomotionState(appliedSpeed);

        if (inputMagnitude > 0.01f)
        {
            Vector3 desiredForward = Vector3.RotateTowards(transform.forward, m_Movement, turnSpeed * Time.deltaTime, 0f);
            m_Rotation = Quaternion.LookRotation(desiredForward);
        }
        
        OnPlayerMove();
        SetAnimatorParameters(inputMagnitude);
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

        m_Animator.SetBool("GoBack", m_IsBackGo);

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

        if (Mathf.Abs(appliedSpeed - runSpeed) < 0.01f)
        {
            m_eLocomotionState = IdleWalkRunEnum.Run;
        }
        else if (Mathf.Abs(appliedSpeed - walkSpeed) < 0.01f)
        {
            m_eLocomotionState = IdleWalkRunEnum.Walk;
        }
        else
        {
            m_eLocomotionState = IdleWalkRunEnum.Idle;
        }

    }

    void OnPlayerMove()
    {
        float currentSpeed = m_IsRunning ? runSpeed : walkSpeed;

        m_Rigidbody.MovePosition(m_Rigidbody.position + m_Movement * currentSpeed * Time.deltaTime);
        
        if (!m_IsBackGo)
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