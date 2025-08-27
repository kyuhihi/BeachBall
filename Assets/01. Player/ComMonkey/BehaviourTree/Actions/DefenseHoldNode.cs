using UnityEngine;

namespace Kyu_BT
{
    public class DefenseHoldNode : BTActionNode
    {
        private System.Action<Vector2> _setMoveInput;
        private System.Action<bool> _defenceAction;
        private GameObject _owner;
        


        public DefenseHoldNode(System.Action<Vector2> setMoveInput,
         System.Action<bool> defenceAction, GameObject owner )
        {
            _setMoveInput = setMoveInput;
            _defenceAction = defenceAction;
            _owner = owner;
        }

        protected override void OnEnter(Blackboard bb)
        {
            // 팔 늘리기 시작
            _defenceAction?.Invoke(true);
        }

        protected override NodeState OnUpdate(Blackboard bb)
        {
            _setMoveInput(Vector2.zero);
            _defenceAction?.Invoke(true);



            // 계속 실행 중
            return NodeState.Success;
        }

        protected override void OnExit(Blackboard bb)
        {
            // 팔 줄이기 시작
            _defenceAction?.Invoke(false);
            _setMoveInput(Vector2.zero);
        }
    }
}
