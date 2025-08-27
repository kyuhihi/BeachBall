using System.Collections.Generic;

namespace Kyu_BT
{
    public class Selector : Node
    {
        readonly List<Node> _children = new();

        public Selector(params Node[] children)
        {
            if (children != null) _children.AddRange(children);
        }

        protected override NodeState OnTick(Blackboard bb)
        {
            for (int i = 0; i < _children.Count; i++)
            {
                var c = _children[i];
                var s = c.Tick(bb);
                if (s == NodeState.Success) return NodeState.Success;
                if (s == NodeState.Running) return NodeState.Running;
                // Failure 이면 다음 자식
            }
            return NodeState.Failure;
        }

        public override void Reset(Blackboard bb)
        {
            base.Reset(bb);
            for (int i = 0; i < _children.Count; i++)
                _children[i].Reset(bb);
        }
    }
}
