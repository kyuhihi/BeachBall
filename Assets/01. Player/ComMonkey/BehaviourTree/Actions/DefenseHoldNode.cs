using UnityEngine;

namespace Kyu_BT
{
    public class DefenseHoldNode : BTActionNode
    {
        private System.Action<Vector2> _setMoveInput;
        private System.Action<bool> _defenceAction;
        readonly System.Func<bool> _CheckArmStretchingAction;
        readonly private string _TargetKey;
        readonly IPlayerInfo _playerInfo;
        IPlayerInfo.CourtPosition _MyCourtPosition = IPlayerInfo.CourtPosition.COURT_END;
        const float _TryHandStretchThreshold = 1.5f;


        public DefenseHoldNode(System.Action<Vector2> setMoveInput,
         System.Action<bool> defenceAction, System.Func<bool> checkArmStretchingAction, string TargetKey, IPlayerInfo info)
        {
            _setMoveInput = setMoveInput;
            _defenceAction = defenceAction;
            _CheckArmStretchingAction = checkArmStretchingAction;
            _TargetKey = TargetKey;
            _playerInfo = info;
        }

        protected override NodeState OnUpdate(Blackboard bb)
        {
            if (_MyCourtPosition == IPlayerInfo.CourtPosition.COURT_END)
            {
                _MyCourtPosition = _playerInfo.m_CourtPosition;
                return NodeState.Success;
            }
            else if (_CheckArmStretchingAction())
            {
                return NodeState.Success;
            }

            bool bHaveToGoAttackNode = CheckAttackTiming(bb);

            if (bHaveToGoAttackNode)
            {
                return NodeState.Failure;
            }
            else
            {
                _setMoveInput(Vector2.zero);
                _defenceAction?.Invoke(true);
                return NodeState.Success;
            }
        }
        private bool CheckAttackTiming(Blackboard bb)
        {
                var target = bb.Get<Transform>(_TargetKey);
            if(_MyCourtPosition == IPlayerInfo.CourtPosition.COURT_RIGHT)
            {
                if(0 < target.position.z && target.position.z > _TryHandStretchThreshold)
                    return true;
            }
            else if(_MyCourtPosition == IPlayerInfo.CourtPosition.COURT_LEFT)
            {
                if(target.position.z < 0 && -_TryHandStretchThreshold < target.position.z)
                    return true;
            }
            return false;
        }

        protected override void OnExit(Blackboard bb)
        {
            _defenceAction?.Invoke(false);
            _setMoveInput(Vector2.zero);
        }
    }
}
