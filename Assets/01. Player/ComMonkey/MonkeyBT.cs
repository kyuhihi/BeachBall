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
    const string KEY_DISTANCE = "Distance";         // ���� ���� �Ÿ� (3D)
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
        var found = GameObject.FindWithTag("Player");
        if (found && found.transform != transform)
            OtherPlayer = found.transform;

        if (OtherPlayer == null)
        {
            Debug.LogAssertion("MonkeyBT: �ٸ� �÷��̾ ã�� ���߽��ϴ�!");
        }
   }
    void InitBT()
    {
        _bb = new Blackboard();
        _bb.Set(KEY_TARGET, Ball);
        _bb.Set(KEY_OTHER_PLAYER, OtherPlayer);
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
        DefenseHoldNode defenseNode = new DefenseHoldNode(
            v => moveBuffer = v,
            bStretch => _movement.DefenceByArm(bStretch),
            _movement.IsStretching,
            KEY_TARGET,
            _movement
        );

        // 3) ���ͼ�Ʈ ���� ���� �Ǵ� ���
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

        //4)�ٳ��������� ���

        // 5) �ֻ��� ����:
        //    Sequence(���ͼ�Ʈ ����? -> ����) �����ϸ� Selector �� defenseNode ����
        //    ��, ����/���� ���̸� ���� ����, ���и� ���� ����
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

