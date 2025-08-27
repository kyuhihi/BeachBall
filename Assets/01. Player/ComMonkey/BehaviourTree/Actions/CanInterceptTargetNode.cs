using UnityEngine;

namespace Kyu_BT
{
    // 타겟을 XZ 평면에서 '가까이(인터셉트 가능)' 한지 판단.
    // 거리가 threshold 이하이면 Success (공격/추격 루틴 선택), 크면 Failure (수비 루틴 선택).
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
