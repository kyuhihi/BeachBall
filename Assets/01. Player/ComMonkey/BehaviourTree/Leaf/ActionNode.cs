using System;

namespace Kyu_BT
{
    public class ActionNode : Node
    {
        private readonly Func<Blackboard, NodeState> _action;
        private readonly Action<Blackboard> _onEnter;
        private readonly Action<Blackboard> _onExit;

        public ActionNode(Func<Blackboard, NodeState> action,
                          Action<Blackboard> onEnter = null,
                          Action<Blackboard> onExit = null)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _onEnter = onEnter;
            _onExit = onExit;
        }

        protected override void OnEnter(Blackboard bb) => _onEnter?.Invoke(bb);
        protected override NodeState OnTick(Blackboard bb) => _action(bb);
        protected override void OnExit(Blackboard bb) => _onExit?.Invoke(bb);
    }
}
