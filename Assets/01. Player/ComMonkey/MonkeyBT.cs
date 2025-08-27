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
    [SerializeField] float interceptXZDistance = 6.0f;   // 이 거리 이하면 공격(Chase+Jump)
    [SerializeField] bool enableDefense = true;

    Vector2 moveBuffer;

    const string KEY_TARGET = "Target";
    const string KEY_DISTANCE = "Distance";         // 기존 추적 거리 (3D)
    const string KEY_XZ_DIST = "XZDistToTarget";    // 새로 기록할 평면 거리(옵션)
    const string KEY_ARM_TRANSFORM = "Arm";

    [SerializeField] Transform[] Arm2s = new Transform[2];
    Vector3[] ArmScaleBuffer = new Vector3[2] { Vector3.one, Vector3.one };

    [Header("Arm Stretch Objects")]
    [SerializeField] GameObject stretchPrefab;
    [SerializeField] int stretchCount = 5;
    [SerializeField] float stretchSpacing = 0.5f;
    [SerializeField] float stretchAnimTime = 0.5f;   // 늘어나는 시간
    [SerializeField] float shrinkAnimTime = 0.5f;    // 줄어드는 시간
    [SerializeField] AnimationCurve stretchCurve = AnimationCurve.EaseInOut(0,0,1,1); // 늘어날 때 커브
    [SerializeField] AnimationCurve shrinkCurve = AnimationCurve.EaseInOut(0,0,1,1);  // 줄어들 때 커브
    [SerializeField] Key stretchKey = Key.Q;
    Vector3 stretchOffset = new Vector3(0, 0, 90);

    GameObject[,] spawnedStretchObjs;
    float stretchAnimT = 0f;
    float stretchAnimDir = 0f; // 1: 늘리기, -1: 줄이기, 0: 정지
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

        // --- Arm2s를 z축 방향으로 오브젝트 생성 및 배치 (부모-자식 관계 없이) ---
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
        // --- Arm2s stretch 오브젝트 배치 끝 ---

        InitBT();
    }

    void InitBT()
    {
        _bb = new Blackboard();
        _bb.Set(KEY_TARGET, target);
        _bb.Set(KEY_DISTANCE, Mathf.Infinity);
        _bb.Set(KEY_XZ_DIST, Mathf.Infinity);


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

        // 2) 수비 노드 (간단: 멈춤 유지). 필요 없으면 enableDefense=false
        Node defenseNode = enableDefense
            ? new DefenseHoldNode(v => moveBuffer = v)
            : (Node)chaseJumpSequence; // 수비 비활성시 그냥 공격 사용

        // 3) 인터셉트 가능 여부 판단 노드
        var canInterceptNode = new CanInterceptTargetNode(
            transform,
            KEY_TARGET,
            interceptXZDistance,
            KEY_XZ_DIST
        );

        // 4) 최상위 선택:
        //    Sequence(인터셉트 가능? -> 공격) 실패하면 Selector 가 defenseNode 실행
        //    즉, 성공/진행 중이면 공격 유지, 실패면 수비 유지
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
        // 키 입력 트리거
        if (Keyboard.current != null && Keyboard.current[stretchKey].wasPressedThisFrame && !isStretched)
        {
            stretchAnimDir = 1f;
            isStretched = true;
        }
        else if (Keyboard.current != null && Keyboard.current[stretchKey].wasReleasedThisFrame && isStretched)
        {
            stretchAnimDir = -1f;
        }

        // 애니메이션 진행
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

        // --- Arm2s stretch 오브젝트 위치/회전 LateUpdate에서 직접 갱신 ---
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

                    // 커브 적용
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

