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
            // �� �ø��� ����
            _defenceAction?.Invoke(true);
        }

        protected override NodeState OnUpdate(Blackboard bb)
        {
            _setMoveInput(Vector2.zero);
            _defenceAction?.Invoke(true);



            // ��� ���� ��
            return NodeState.Success;
        }

        protected override void OnExit(Blackboard bb)
        {
            // �� ���̱� ����
            _defenceAction?.Invoke(false);
            _setMoveInput(Vector2.zero);
        }
    }
}
