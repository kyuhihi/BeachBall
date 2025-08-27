using System;

namespace Kyu_BT
{
    public class ConditionNode : Node
    {
        private readonly Func<Blackboard, bool> _predicate;
        private readonly bool _invert;

        public ConditionNode(Func<Blackboard, bool> predicate, bool invert = false)
        {
            _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
            _invert = invert;
        }

        protected override NodeState OnTick(Blackboard bb)
        {
            try
            {
                bool res = _predicate(bb);
                if (_invert) res = !res;
                return res ? NodeState.Success : NodeState.Failure;
            }
            catch
            {
                return NodeState.Failure;
            }
        }
    }
}
