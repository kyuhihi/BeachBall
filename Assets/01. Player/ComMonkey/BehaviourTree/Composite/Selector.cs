using System.Collections.Generic;

namespace Kyu_BT
{
    public class Selector : Node
    {
        private readonly List<Node> _children;
        private int _currentIndex = 0;

        public Selector(params Node[] children)
        {
            _children = new List<Node>(children ?? new Node[0]);
        }

        protected override void OnEnter(Blackboard bb) => _currentIndex = 0;

        protected override NodeState OnTick(Blackboard bb)
        {
            for (int i = _currentIndex; i < _children.Count; i++)
            {
                var s = _children[i].Tick(bb);
                if (s == NodeState.Failure) continue;
                if (s == NodeState.Running)
                {
                    _currentIndex = i;
                    return NodeState.Running;
                }
                return NodeState.Success;
            }
            return NodeState.Failure;
        }

        protected override void OnExit(Blackboard bb)
        {
            foreach (var c in _children) c.Reset(bb);
            _currentIndex = 0;
        }

        public override void Reset(Blackboard bb)
        {
            base.Reset(bb);
            _currentIndex = 0;
            foreach (var c in _children) c.Reset(bb);
        }
    }
}
