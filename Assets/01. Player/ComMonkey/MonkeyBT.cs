using UnityEngine;
using Kyu_BT;
using Unity.Burst.Intrinsics;
using UnityEngine.InputSystem;

public class MonkeyBT : MonoBehaviour, IResetAbleListener
{
    BehaviorTreeRunner _runner;
    Blackboard _bb;
    MonkeyPlayerMovement _movement;

    [Header("Target")]
    Transform Ball;
    Transform OtherPlayer;
    string targetTag = "Ball";

    [Header("Chase")]
    [SerializeField] float stopDistance = 0.0f;
    [SerializeField] bool cameraRelative = true;

    private float jumpHeightDiff = 0.1f;
    private float jumpCooldown = 0.1f;
    private bool jumpOnlyWhenGrounded = true;

    Vector2 moveBuffer;

    const string KEY_TARGET = "Ball";
    const string KEY_OTHER_PLAYER = "OtherPlayer";
    const string KEY_DISTANCE = "Distance";         // 기존 추적 거리 (3D)
    void Start()
    {
        _movement = GetComponent<MonkeyPlayerMovement>();

        if (!Ball && !string.IsNullOrEmpty(targetTag))
        {
            var found = GameObject.FindWithTag(targetTag);
            if (found && found.transform != transform)
                Ball = found.transform;
        }
        FindOtherPlayer();
        InitBT();
    }

    private void FindOtherPlayer()
    {
        var found = GameObject.FindGameObjectsWithTag("Player");
        if (found.Length > 0)
        {
            foreach (var player in found)
            {
                if (player.transform != transform)
                {
                    OtherPlayer = player.transform;
                    break;
                }
            }
        }

   }
    void InitBT()
    {
        _bb = new Blackboard();
        _bb.Set(KEY_TARGET, Ball);
        _bb.Set(KEY_OTHER_PLAYER, OtherPlayer);
        _bb.Set(KEY_DISTANCE, Mathf.Infinity);


        // 1) Chase (항상 Success) + Jump (평가/실행) 묶은 공격 시퀀스
        var chaseNode = new ChaseTargetNode(
            transform,
            KEY_TARGET,
            KEY_DISTANCE,
            stopDistance,
            v => moveBuffer = v,
            cameraRelative,
            alwaysSuccess: true
        );

        var jumpNode = new JumpIfHigherNode(
            transform,
            KEY_TARGET,
            jumpHeightDiff,
            JumpAction,
            jumpCooldown,
            jumpOnlyWhenGrounded,
            _movement != null ? _movement.IsGroundedForAI : (System.Func<bool>)null
        );

        Node chaseJumpSequence = new Sequence(chaseNode, jumpNode);

        // 2) 수비 노드 (간단: 멈춤 유지)
        DefenseHoldNode defenseNode = new DefenseHoldNode(
            v => moveBuffer = v,
            bStretch => _movement.DefenceByArm(bStretch),
            _movement.IsStretching,
            KEY_TARGET,
            _movement
        );

        // 3) 인터셉트 가능 여부 판단 노드
        var canInterceptNode = new CanInterceptTargetNode(
            transform,
            KEY_TARGET,
            _movement
        );

        var throwBananaNode = new ThrowBananaNode(
            v => moveBuffer = v,
            KEY_OTHER_PLAYER,
            _movement,
            this.transform
        );  

        //4)바나나던지기 노드

        // 5) 최상위 선택:
        //    Sequence(인터셉트 가능? -> 공격) 실패하면 Selector 가 defenseNode 실행
        //    즉, 성공/진행 중이면 공격 유지, 실패면 수비 유지
        Node root = new Selector(
            new Sequence(canInterceptNode, chaseJumpSequence),
            defenseNode, throwBananaNode
        );

        _runner = new BehaviorTreeRunner(root, _bb);

        
    }

    void FixedUpdate()
    {
        _runner?.Tick();
    }

    void Update()
    {
        if (Keyboard.current.leftAltKey.wasPressedThisFrame)
        {
            PlayerUIManager.GetInstance().UpUltimateBar(_movement.m_CourtPosition,0.1f);
        }
        if (Keyboard.current.rightAltKey.wasPressedThisFrame)
        {
            PlayerUIManager.GetInstance().UpUltimateBar(_movement.m_CourtPosition,-0.1f);
        }

    }

    void LateUpdate()
    {
        if (_movement)
            _movement.OnMoveInput(moveBuffer);

        
    }

    void JumpAction()
    {
        _movement?.AIJump(allowDouble: true);
    }

    public void AddResetCall()
    {
        Signals.RoundResetAble.AddStart(OnRoundStart);
        Signals.RoundResetAble.AddEnd(OnRoundEnd);
    }

    public void RemoveResetCall()
    {
        Signals.RoundResetAble.RemoveStart(OnRoundStart);
        Signals.RoundResetAble.RemoveEnd(OnRoundEnd);
    }

    public void OnRoundStart()
    {
        moveBuffer = Vector2.zero;
        _runner?.Resume();
    }

    public void OnRoundEnd()
    {
        _runner?.Stop();
        moveBuffer = Vector2.zero;
    }
    
}

