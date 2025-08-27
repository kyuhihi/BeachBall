using UnityEngine;

namespace Kyu_BT
{
    // Ÿ���� XZ ��鿡�� '������(���ͼ�Ʈ ����)' ���� �Ǵ�.
    // �Ÿ��� threshold �����̸� Success (����/�߰� ��ƾ ����), ũ�� Failure (���� ��ƾ ����).
    public class CanInterceptTargetNode : BTActionNode
    {
        readonly Transform _owner;
        readonly string _keyTarget;
        readonly float _interceptXZDistance;
        readonly string _writeDistanceKey; // �����忡 ���� XZ �Ÿ� ���(����)

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
