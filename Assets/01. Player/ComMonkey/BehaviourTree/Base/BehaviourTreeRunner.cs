using System;
using System.Collections.Generic;

namespace Kyu_BT
{
    public class BehaviorTreeRunner : IDisposable
    {
        private readonly Node _root;
        private readonly Blackboard _bb;
        private readonly bool _trace;
        private readonly List<NodeExecutionRecord> _traceBuffer = new(64);
        private readonly List<Node> _runningChain = new(16);

        public IReadOnlyList<NodeExecutionRecord> LastTrace => _traceBuffer;
        public IReadOnlyList<Node> LastRunningChain => _runningChain;
        public Node LastRunningLeaf { get; private set; }
        public NodeState LastRootState { get; private set; }

        public BehaviorTreeRunner(Node root, Blackboard bb, bool enableTrace = true)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _bb = bb ?? throw new ArgumentNullException(nameof(bb));
            _trace = enableTrace;

            if (_trace)
                Node.NodeTicked += OnNodeTicked;
        }

        public NodeState Tick()
        {
            if (_trace)
            {
                _traceBuffer.Clear();
                _runningChain.Clear();
                LastRunningLeaf = null;
            }

            LastRootState = _root.Tick(_bb);

            if (_trace && LastRunningLeaf == null && LastRootState == NodeState.Running)
            {
                // 루트 자체가 Running 이지만 Running leaf 못찾은 경우(이상 케이스) → 루트 추가
                LastRunningLeaf = _root;
            }

            return LastRootState;
        }

        private void OnNodeTicked(Node node, NodeState state)
        {
            _traceBuffer.Add(new NodeExecutionRecord(node, state));

            if (state == NodeState.Running)
            {
                // Running 체인 기록 (마지막 Running leaf 는 가장 마지막 Running)
                _runningChain.Add(node);
                LastRunningLeaf = node;
            }
        }

        public string GetRunningPathString()
        {
            if (_runningChain.Count == 0) return "(idle)";
            return string.Join(" -> ", _runningChain);
        }

        public void Reset()
        {
            _root.Reset(_bb);
            if (_trace)
            {
                _traceBuffer.Clear();
                _runningChain.Clear();
                LastRunningLeaf = null;
            }
        }

        public void Dispose()
        {
            if (_trace)
                Node.NodeTicked -= OnNodeTicked;
        }
    }

    public readonly struct NodeExecutionRecord
    {
        public readonly Node Node;
        public readonly NodeState State;
        public readonly string TypeName;

        public NodeExecutionRecord(Node node, NodeState state)
        {
            Node = node;
            State = state;
            TypeName = node.GetType().Name;
        }

        public override string ToString() => $"{TypeName}:{State}";
    }
}
