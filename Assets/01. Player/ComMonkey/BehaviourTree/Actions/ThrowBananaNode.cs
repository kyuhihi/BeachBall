using UnityEngine;
namespace Kyu_BT
{
    public class ThrowBananaNode : BTActionNode
    {
        readonly string m_keyOtherPlayer;
        readonly MonkeyPlayerMovement m_movement;
        readonly Transform _owner;

        readonly System.Action<Vector2> _setMoveInput;
        readonly bool _cameraRelative;

        public ThrowBananaNode(System.Action<Vector2> setMoveInput,
                 string OtherPlayerKey, MonkeyPlayerMovement movement, Transform owner)
        {
            _setMoveInput = setMoveInput;
            m_keyOtherPlayer = OtherPlayerKey;
            m_movement = movement;
            _owner = owner;
        }


        protected override NodeState OnUpdate(Blackboard bb)
        {
            MoveToOtherPlayer(bb);
            m_movement.ThrowBanana(bb.Get<Transform>(m_keyOtherPlayer));

            return NodeState.Failure;
        }

        private void MoveToOtherPlayer(Blackboard bb)
        {
            var t = bb.Get<Transform>(m_keyOtherPlayer);

            Vector3 to = t.position - _owner.position;
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
        }
    }
}