using System;
using System.Collections.Generic;

namespace Kyu_BT
{
    /// <summary>
    /// �ൿ Ʈ���� ������ �����ϴ� Ŭ����.
    /// Ʈ���� Tick, Ʈ���̽�(���� ��� ���), ����, ���� ���� ���.
    /// </summary>
    public class BehaviorTreeRunner : IDisposable
    {
        private readonly Node _root;
        private readonly Blackboard _bb;
        private readonly bool _trace;
        private readonly List<NodeExecutionRecord> _traceBuffer = new(64);
        private readonly List<Node> _runningChain = new(16);

        private bool _stopped = false; // Runner�� ���� ���� ����

        public bool IsStopped => _stopped; // �ܺο��� ���� ���� Ȯ�ο� ������Ƽ

        public IReadOnlyList<NodeExecutionRecord> LastTrace => _traceBuffer;
        public IReadOnlyList<Node> LastRunningChain => _runningChain;
        public Node LastRunningLeaf { get; private set; }
        public NodeState LastRootState { get; private set; }

        /// <summary>
        /// Runner�� ���� ���·� ��ȯ
        /// </summary>
        public void Stop()
        {
            _stopped = true;
        }

        /// <summary>
        /// Runner�� �ٽ� ���� ���·� ��ȯ
        /// </summary>
        public void Resume()
        {
            _stopped = false;
        }

        /// <summary>
        /// ������. ��Ʈ ���� ������, Ʈ���̽� Ȱ��ȭ ���θ� ����.
        /// </summary>
        public BehaviorTreeRunner(Node root, Blackboard bb, bool enableTrace = true)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _bb = bb ?? throw new ArgumentNullException(nameof(bb));
            _trace = enableTrace;

            if (_trace)
                Node.NodeTicked += OnNodeTicked; // Ʈ���̽� �� Tick �̺�Ʈ ����
        }

        /// <summary>
        /// Ʈ�� ����(Tick). Ʈ���̽��� ���� ������ ���� ��� ���.
        /// </summary>
        public NodeState Tick()
        {
            if (_stopped)
                return LastRootState; // ���� ���¸� ���� ���� ��ȯ

            if (_trace)
            {
                _traceBuffer.Clear();
                _runningChain.Clear();
                LastRunningLeaf = null;
            }

            LastRootState = _root.Tick(_bb);

            if (_trace && LastRunningLeaf == null && LastRootState == NodeState.Running)
            {
                // ��Ʈ ��ü�� Running ������ Running leaf ��ã�� ���(�̻� ���̽�) �� ��Ʈ �߰�
                LastRunningLeaf = _root;
            }

            return LastRootState;
        }

        /// <summary>
        /// ��尡 Tick�� ������ ȣ��Ǿ� ���� ��� �� Running ü�� ����.
        /// </summary>
        private void OnNodeTicked(Node node, NodeState state)
        {
            _traceBuffer.Add(new NodeExecutionRecord(node, state));

            if (state == NodeState.Running)
            {
                // Running ü�� ��� (������ Running leaf �� ���� ������ Running)
                _runningChain.Add(node);
                LastRunningLeaf = node;
            }
        }

        /// <summary>
        /// ���� Running ü���� ���ڿ��� ��ȯ.
        /// </summary>
        public string GetRunningPathString()
        {
            if (_runningChain.Count == 0) return "(idle)";
            return string.Join(" -> ", _runningChain);
        }

        /// <summary>
        /// Ʈ���� Ʈ���̽� ���¸� ����.
        /// </summary>
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

        /// <summary>
        /// Ʈ���̽� �̺�Ʈ ���� ���� �� ���ҽ� ����.
        /// </summary>
        public void Dispose()
        {
            if (_trace)
                Node.NodeTicked -= OnNodeTicked;
        }
    }

    /// <summary>
    /// ��� ���� ��� ����ü. ���, ����, Ÿ�Ը� ����.
    /// </summary>
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
