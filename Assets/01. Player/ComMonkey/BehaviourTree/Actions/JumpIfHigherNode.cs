using UnityEngine;

namespace Kyu_BT
{

    public class JumpIfHigherNode : BTActionNode
    {
        readonly Transform _owner;
        readonly string _keyTarget;
        readonly float _heightDiff;
        readonly System.Action _jumpAction;
        readonly float _cooldown;
        readonly bool _onlyWhenGrounded;
        readonly System.Func<bool> _isGroundedFunc; // Ground 체크 전달(없으면 무시)

        float _nextAllowedTime;


        public JumpIfHigherNode(
            Transform owner,
            string keyTarget,
            float heightDiffThreshold,
            System.Action jumpAction,
            float cooldownSec,
            bool onlyWhenGrounded,
            System.Func<bool> isGroundedFunc = null)
        {
            _owner = owner;
            _keyTarget = keyTarget;
            _heightDiff = heightDiffThreshold;
            _jumpAction = jumpAction;
            _cooldown = Mathf.Max(0f, cooldownSec);
            _onlyWhenGrounded = onlyWhenGrounded;
            _isGroundedFunc = isGroundedFunc;
            _nextAllowedTime = 0f;
        }

        protected override NodeState OnUpdate(Blackboard bb)
        {
            var t = bb.Get<Transform>(_keyTarget);
            if (t)
            {
                float diff = t.position.y - _owner.position.y;

                if (diff >= _heightDiff)
                {
                    bool timeOK = Time.time >= _nextAllowedTime;
                    bool groundOK = !_onlyWhenGrounded || (_isGroundedFunc == null || _isGroundedFunc());

                    if (timeOK && groundOK)
                    {
                        _jumpAction?.Invoke();
                        _nextAllowedTime = Time.time + _cooldown;
                    }
                }
            }
            return NodeState.Success;
        }
    }
}
