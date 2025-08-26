using System;

namespace Kyu_BT
{
    public class BehaviorTreeRunner
    {
        private readonly Node _root;
        private readonly Blackboard _bb;

        public BehaviorTreeRunner(Node root, Blackboard blackboard)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _bb = blackboard ?? throw new ArgumentNullException(nameof(blackboard));
        }

        public NodeState Tick() => _root.Tick(_bb);

        public void Reset() => _root.Reset(_bb);
    }
}
