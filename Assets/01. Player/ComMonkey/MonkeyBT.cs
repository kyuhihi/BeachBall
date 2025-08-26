using UnityEngine;
using Kyu_BT;

public class MonkeyBT : MonoBehaviour
{
    private BehaviorTreeRunner _runner;
    private Blackboard _bb;
    private MonkeyPlayerMovement _movement;

    [SerializeField] private Transform target;
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private float stopDistance = 1.5f;

    // Blackboard 키
    private const string KEY_TARGET = "Target";
    private const string KEY_DISTANCE = "Distance";

    void Start()
    {
        _movement = GetComponent<MonkeyPlayerMovement>();

        if (!target && !string.IsNullOrEmpty(targetTag))
        {
            var found = GameObject.FindWithTag(targetTag);
            if (found && found.transform != transform)
                target = found.transform;
        }

        _bb = new Blackboard();
        _bb.Set(KEY_TARGET, target);
        _bb.Set(KEY_DISTANCE, Mathf.Infinity);

        // 단일 Chase 액션 노드
        var chaseNode = new ActionNode(
            bb =>
            {
                var t = bb.Get<Transform>(KEY_TARGET);
                if (!t)
                {
                    SendMoveInput(Vector2.zero);
                    return NodeState.Failure;
                }

                Vector3 to = t.position - transform.position;
                float dist = to.magnitude;
                bb.Set(KEY_DISTANCE, dist);

                if (dist <= stopDistance)
                {
                    SendMoveInput(Vector2.zero);
                    return NodeState.Running; // 혹은 Success 로 끝내고 싶으면 Success
                }

                to.y = 0f;
                if (to.sqrMagnitude < 0.0001f)
                {
                    SendMoveInput(Vector2.zero);
                    return NodeState.Running;
                }

                to.Normalize();

                // 카메라 기준 입력 (카메라 없으면 월드 Z 전방)
                Vector3 camFwd = Camera.main ? Camera.main.transform.forward : Vector3.forward;
                camFwd.y = 0; camFwd.Normalize();
                Vector3 camRight = Vector3.Cross(Vector3.up, camFwd);

                float v = Vector3.Dot(to, camFwd);
                float h = Vector3.Dot(to, camRight);
                SendMoveInput(new Vector2(h, v));

                return NodeState.Running;
            }
        );

        _runner = new BehaviorTreeRunner(chaseNode, _bb);
    }

    void FixedUpdate()
    {
        _runner.Tick();

    }

    private void SendMoveInput(Vector2 input)
    {
        if (_movement)
        {
            _movement.OnMoveInput(input);
        }
    }
}

