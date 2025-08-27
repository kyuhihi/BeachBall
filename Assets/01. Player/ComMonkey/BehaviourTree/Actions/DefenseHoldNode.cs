using UnityEngine;

namespace Kyu_BT
{
    public class DefenseHoldNode : BTActionNode
    {
        readonly System.Action<Vector2> _setMoveInput;



        public DefenseHoldNode(System.Action<Vector2> setMoveInput
        )
        {
            _setMoveInput = setMoveInput;
        }

        protected override void OnEnter(Blackboard bb)
        {

        }

        protected override NodeState OnUpdate(Blackboard bb)
        {
            _setMoveInput(Vector2.zero);

            return NodeState.Running;
        }

        protected override void OnExit(Blackboard bb)
        {
            _setMoveInput(Vector2.zero);
        }
    }
}
