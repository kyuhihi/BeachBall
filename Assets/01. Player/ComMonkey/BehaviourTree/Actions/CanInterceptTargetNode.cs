using UnityEngine;

namespace Kyu_BT
{
    // 타겟을 XZ 평면에서 '가까이(인터셉트 가능)' 한지 판단.
    // 거리가 threshold 이하이면 Success (공격/추격 루틴 선택), 크면 Failure (수비 루틴 선택).
    public class CanInterceptTargetNode : BTActionNode
    {
        readonly Transform _owner;
        readonly string _keyTarget;
        readonly float _interceptXZDistance;
        readonly string _writeDistanceKey; // 블랙보드에 실제 XZ 거리 기록(선택)

        public CanInterceptTargetNode(
            Transform owner,
            string keyTarget,
            float interceptXZDistance,
            string writeDistanceKey = null)
        {
            _owner = owner;
            _keyTarget = keyTarget;
            _interceptXZDistance = Mathf.Max(0f, interceptXZDistance);
            _writeDistanceKey = writeDistanceKey;
        }

        protected override NodeState OnUpdate(Blackboard bb)
        {
            var t = bb.Get<Transform>(_keyTarget);
            if (!t) return NodeState.Failure;

            Vector3 d = t.position - _owner.position;


            float sqrXZ = d.x * d.x + d.z * d.z;
            float thresholdSqr = _interceptXZDistance * _interceptXZDistance;

            float distXZ = Mathf.Sqrt(sqrXZ);
            if (!string.IsNullOrEmpty(_writeDistanceKey))
                bb.Set(_writeDistanceKey, distXZ);
            bool bSuccess = sqrXZ <= thresholdSqr;
            return bSuccess ? NodeState.Success : NodeState.Failure;
        }
    }
}
