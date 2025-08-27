using UnityEngine;

namespace Kyu_BT
{
    public class ChaseTargetNode : BTActionNode
    {
        readonly Transform _owner;
        readonly string _keyTarget;
        readonly string _keyDistance;
        readonly float _stopDistance;
        readonly System.Action<Vector2> _setMoveInput;
        readonly bool _cameraRelative;
        readonly bool _alwaysSuccess;

        public ChaseTargetNode(
            Transform owner,
            string keyTarget,
            string keyDistance,
            float stopDistance,
            System.Action<Vector2> setMoveInput,
            bool cameraRelative = true,
            bool alwaysSuccess = false)
        {
            _owner = owner;
            _keyTarget = keyTarget;
            _keyDistance = keyDistance;
            _stopDistance = Mathf.Max(0f, stopDistance);
            _setMoveInput = setMoveInput;
            _cameraRelative = cameraRelative;
            _alwaysSuccess = alwaysSuccess;
        }

        protected override NodeState OnUpdate(Blackboard bb)
        {
            var t = bb.Get<Transform>(_keyTarget);
            if (!t)
            {
                _setMoveInput(Vector2.zero);
                return _alwaysSuccess ? NodeState.Success : NodeState.Failure;
            }

            Vector3 to = t.position - _owner.position;
            float dist = to.magnitude;
            bb.Set(_keyDistance, dist);

            // 멈춰야 하는 거리
            if (dist <= _stopDistance)
            {
                _setMoveInput(Vector2.zero);
                return _alwaysSuccess ? NodeState.Success : NodeState.Running;
            }

            to.y = 0f;
            if (to.sqrMagnitude < 1e-6f)
            {
                _setMoveInput(Vector2.zero);
                return _alwaysSuccess ? NodeState.Success : NodeState.Running;
            }

            to.Normalize();

            Vector3 fwd, right;
            if (_cameraRelative && Camera.main)
            {
                fwd = Camera.main.transform.forward; fwd.y = 0; fwd.Normalize();
                right = Vector3.Cross(Vector3.up, fwd);
            }
            else
            {
                fwd = Vector3.forward;
                right = Vector3.right;
            }

            float v = Vector3.Dot(to, fwd);
            float h = Vector3.Dot(to, right);
            _setMoveInput(new Vector2(h, v));

            return _alwaysSuccess ? NodeState.Success : NodeState.Running;
        }

        protected override void OnExit(Blackboard bb)
        {
            // 항상 Success 로 매 프레임 종료되는 모드에서는 입력을 0 으로 지우지 않는다.
            if (!_alwaysSuccess)
                _setMoveInput(Vector2.zero);
        }
    }
}
