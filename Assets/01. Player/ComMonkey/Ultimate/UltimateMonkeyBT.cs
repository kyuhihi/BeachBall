using UnityEngine;
using Kyu_BT;
using Unity.Burst.Intrinsics;
using UnityEngine.InputSystem;

public class UltimateMonkeyBT : MonoBehaviour
{
    BehaviorTreeRunner _runner;
    Blackboard _bb;
    UltimateMonkeyPlayerMovement _movement;

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
        _movement = GetComponent<UltimateMonkeyPlayerMovement>();

        if (!Ball && !string.IsNullOrEmpty(targetTag))
        {
            var found = GameObject.FindWithTag(targetTag);
            if (found && found.transform != transform)
                Ball = found.transform;
        }
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

        if (OtherPlayer == null)
        {
            Debug.LogAssertion("UltimateMonkeyBT: 다른 플레이어를 찾지 못했습니다!");
        }
   }
    void InitBT()
    {
        _bb = new Blackboard();
        _bb.Set(KEY_TARGET, Ball);
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

        Node root = chaseJumpSequence;

        _runner = new BehaviorTreeRunner(root, _bb);

        
    }

    void FixedUpdate()
    {
        _runner?.Tick();
    }

    void Update()
    {
        
    }

    void LateUpdate()
    {
        if (_movement)
            _movement.OnMoveInput(moveBuffer);
    }

    void JumpAction()
    {
        _movement?.AIJump(allowDouble: false);
    }
}

