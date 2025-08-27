using UnityEngine;
using Kyu_BT;
using Unity.Burst.Intrinsics;
using UnityEngine.InputSystem;

public class MonkeyBT : MonoBehaviour
{
    BehaviorTreeRunner _runner;
    Blackboard _bb;
    MonkeyPlayerMovement _movement;

    [Header("Target")]
    [SerializeField] Transform target;
    [SerializeField] string targetTag = "Player";

    [Header("Chase")]
    [SerializeField] float stopDistance = 0.0f;
    [SerializeField] bool cameraRelative = true;

    private float jumpHeightDiff = 0.1f;
    private float jumpCooldown = 0.1f;
    private bool jumpOnlyWhenGrounded = true;

    [Header("Decision")]
    [SerializeField] float interceptXZDistance = 6.0f;   // �� �Ÿ� ���ϸ� ����(Chase+Jump)

    Vector2 moveBuffer;

    const string KEY_TARGET = "Target";
    const string KEY_DISTANCE = "Distance";         // ���� ���� �Ÿ� (3D)
    void Start()
    {
        _movement = GetComponent<MonkeyPlayerMovement>();

        if (!target && !string.IsNullOrEmpty(targetTag))
        {
            var found = GameObject.FindWithTag(targetTag);
            if (found && found.transform != transform)
                target = found.transform;
        }
        InitBT();
    }

   
    void InitBT()
    {
        _bb = new Blackboard();
        _bb.Set(KEY_TARGET, target);
        _bb.Set(KEY_DISTANCE, Mathf.Infinity);


        // 1) Chase (�׻� Success) + Jump (��/����) ���� ���� ������
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

        // 2) ���� ��� (����: ���� ����)
        Node defenseNode = new DefenseHoldNode(v => moveBuffer = v,
        bStretch => _movement.DefenceByArm(bStretch), gameObject);

        // 3) ���ͼ�Ʈ ���� ���� �Ǵ� ���
        var canInterceptNode = new CanInterceptTargetNode(
            transform,
            KEY_TARGET,
            _movement
        );

        // 4) �ֻ��� ����:
        //    Sequence(���ͼ�Ʈ ����? -> ����) �����ϸ� Selector �� defenseNode ����
        //    ��, ����/���� ���̸� ���� ����, ���и� ���� ����
        Node root = new Selector(
            new Sequence(canInterceptNode, chaseJumpSequence),
            defenseNode
        );

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
        _movement?.AIJump(allowDouble: true);
    }
}

