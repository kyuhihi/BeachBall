using UnityEngine;

namespace Kyu_BT
{
    // ���� �׼� ���̽�: �Ļ� Ŭ������ OnUpdate�� ����
    public abstract class BTActionNode : Node
    {
        protected override void OnEnter(Blackboard bb) { }

        protected override NodeState OnTick(Blackboard bb)
        {
#if UNITY_EDITOR
            try
            {
                return OnUpdate(bb);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[BT] {GetType().Name} ����: {ex.Message}\n{ex.StackTrace}");
                return NodeState.Failure;
            }
#else
            return OnUpdate(bb);
#endif
        }

        protected abstract NodeState OnUpdate(Blackboard bb);
        protected override void OnExit(Blackboard bb) { }
    }
}
