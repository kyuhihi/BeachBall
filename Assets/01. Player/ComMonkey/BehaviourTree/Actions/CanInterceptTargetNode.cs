using UnityEngine;

namespace Kyu_BT
{
    // Ÿ���� XZ ��鿡�� '������(���ͼ�Ʈ ����)' ���� �Ǵ�.
    // �Ÿ��� threshold �����̸� Success (����/�߰� ��ƾ ����), ũ�� Failure (���� ��ƾ ����).
    public class CanInterceptTargetNode : BTActionNode
    {
        readonly Transform _owner;
        readonly string _keyTarget;
        readonly IPlayerInfo _playerInfo;
        IPlayerInfo.CourtPosition _MyCourtPosition = IPlayerInfo.CourtPosition.COURT_END;

        public CanInterceptTargetNode(
            Transform owner,
            string keyTarget,
            IPlayerInfo info
            )
        {
            _owner = owner;
            _keyTarget = keyTarget;
            _playerInfo = info;
        }

        protected override NodeState OnUpdate(Blackboard bb)
        {
            if (_playerInfo.m_CourtPosition == IPlayerInfo.CourtPosition.COURT_END)
            {
                _MyCourtPosition = _playerInfo.m_CourtPosition;
                return NodeState.Success;
            }
            
            var t = bb.Get<Transform>(_keyTarget);
            if (!t) return NodeState.Success;
            bool bSuccess = false;

            if (_playerInfo.m_CourtPosition == IPlayerInfo.CourtPosition.COURT_RIGHT)
            {
                bSuccess = t.position.z < 0.0f;
            }
            else if (_playerInfo.m_CourtPosition == IPlayerInfo.CourtPosition.COURT_LEFT)
            {
                bSuccess = t.position.z > 0.0f;
            }

            return bSuccess ? NodeState.Success : NodeState.Failure;
        }
    }
}
