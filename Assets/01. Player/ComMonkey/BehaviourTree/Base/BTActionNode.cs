using UnityEngine;

namespace Kyu_BT
{
    // 공통 액션 베이스: 파생 클래스는 OnUpdate만 구현
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
                Debug.LogError($"[BT] {GetType().Name} 예외: {ex.Message}\n{ex.StackTrace}");
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
