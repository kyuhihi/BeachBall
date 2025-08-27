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
    [SerializeField] bool enableDefense = true;

    Vector2 moveBuffer;

    const string KEY_TARGET = "Target";
    const string KEY_DISTANCE = "Distance";         // ���� ���� �Ÿ� (3D)
    const string KEY_XZ_DIST = "XZDistToTarget";    // ���� ����� ��� �Ÿ�(�ɼ�)
    const string KEY_ARM_TRANSFORM = "Arm";

    [SerializeField] Transform[] Arm2s = new Transform[2];
    Vector3[] ArmScaleBuffer = new Vector3[2] { Vector3.one, Vector3.one };

    [Header("Arm Stretch Objects")]
    [SerializeField] GameObject stretchPrefab;
    [SerializeField] int stretchCount = 5;
    [SerializeField] float stretchSpacing = 0.5f;
    [SerializeField] float stretchAnimTime = 0.5f;   // �þ�� �ð�
    [SerializeField] float shrinkAnimTime = 0.5f;    // �پ��� �ð�
    [SerializeField] AnimationCurve stretchCurve = AnimationCurve.EaseInOut(0,0,1,1); // �þ �� Ŀ��
    [SerializeField] AnimationCurve shrinkCurve = AnimationCurve.EaseInOut(0,0,1,1);  // �پ�� �� Ŀ��
    [SerializeField] Key stretchKey = Key.Q;
    Vector3 stretchOffset = new Vector3(0, 0, 90);

    GameObject[,] spawnedStretchObjs;
    float stretchAnimT = 0f;
    float stretchAnimDir = 0f; // 1: �ø���, -1: ���̱�, 0: ����
    bool isStretched = false;

    void Start()
    {
        _movement = GetComponent<MonkeyPlayerMovement>();

        if (!target && !string.IsNullOrEmpty(targetTag))
        {
            var found = GameObject.FindWithTag(targetTag);
            if (found && found.transform != transform)
                target = found.transform;
        }

        // --- Arm2s�� z�� �������� ������Ʈ ���� �� ��ġ (�θ�-�ڽ� ���� ����) ---
        if (stretchPrefab && Arm2s != null && Arm2s.Length > 0 && stretchCount > 0)
        {
            spawnedStretchObjs = new GameObject[Arm2s.Length, stretchCount];
            for (int armIdx = 0; armIdx < Arm2s.Length; armIdx++)
            {
                var arm = Arm2s[armIdx];
                if (!arm) continue;
                for (int i = 0; i < stretchCount; i++)
                {
                    GameObject obj = Instantiate(stretchPrefab);
                    spawnedStretchObjs[armIdx, i] = obj;
                }
            }
        }
        // --- Arm2s stretch ������Ʈ ��ġ �� ---

        InitBT();
    }

    void InitBT()
    {
        _bb = new Blackboard();
        _bb.Set(KEY_TARGET, target);
        _bb.Set(KEY_DISTANCE, Mathf.Infinity);
        _bb.Set(KEY_XZ_DIST, Mathf.Infinity);


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

        // 2) ���� ��� (����: ���� ����). �ʿ� ������ enableDefense=false
        Node defenseNode = enableDefense
            ? new DefenseHoldNode(v => moveBuffer = v)
            : (Node)chaseJumpSequence; // ���� ��Ȱ���� �׳� ���� ���

        // 3) ���ͼ�Ʈ ���� ���� �Ǵ� ���
        var canInterceptNode = new CanInterceptTargetNode(
            transform,
            KEY_TARGET,
            interceptXZDistance,
            KEY_XZ_DIST
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
        // Ű �Է� Ʈ����
        if (Keyboard.current != null && Keyboard.current[stretchKey].wasPressedThisFrame && !isStretched)
        {
            stretchAnimDir = 1f;
            isStretched = true;
        }
        else if (Keyboard.current != null && Keyboard.current[stretchKey].wasReleasedThisFrame && isStretched)
        {
            stretchAnimDir = -1f;
        }

        // �ִϸ��̼� ����
        if (stretchAnimDir != 0f)
        {
            float animTime = stretchAnimDir > 0 ? stretchAnimTime : shrinkAnimTime;
            stretchAnimT += (Time.deltaTime / Mathf.Max(animTime, 0.01f)) * stretchAnimDir;
            stretchAnimT = Mathf.Clamp01(stretchAnimT);

            if (stretchAnimT >= 1f)
            {
                stretchAnimT = 1f;
                stretchAnimDir = 0f;
            }
            else if (stretchAnimT <= 0f)
            {
                stretchAnimT = 0f;
                stretchAnimDir = 0f;
                isStretched = false;
            }
        }
    }

    void LateUpdate()
    {
        if (_movement)
            _movement.OnMoveInput(moveBuffer);

        // --- Arm2s stretch ������Ʈ ��ġ/ȸ�� LateUpdate���� ���� ���� ---
        if (Arm2s != null && spawnedStretchObjs != null)
        {
            for (int armIdx = 0; armIdx < Arm2s.Length; armIdx++)
            {
                var arm = Arm2s[armIdx];
                if (!arm) continue;
                for (int i = 0; i < stretchCount; i++)
                {
                    var obj = spawnedStretchObjs[armIdx, i];
                    if (!obj) continue;

                    // Ŀ�� ����
                    float t = stretchAnimT;
                    float curveT = stretchAnimDir >= 0 ? stretchCurve.Evaluate(t) : shrinkCurve.Evaluate(t);

                    float spread = Mathf.Lerp(0, i * stretchSpacing, curveT);
                    Vector3 localOffset = new Vector3(-spread, 0, 0);
                    Vector3 worldPos = arm.TransformPoint(localOffset);
                    obj.transform.position = worldPos;
                    obj.transform.rotation = arm.rotation;
                    Vector3 objRot = obj.transform.rotation.eulerAngles;
                    objRot += stretchOffset;
                    obj.transform.rotation = Quaternion.Euler(objRot);
                }
            }
        }
    }

    void JumpAction()
    {
        _movement?.AIJump(allowDouble: true);
    }
}

