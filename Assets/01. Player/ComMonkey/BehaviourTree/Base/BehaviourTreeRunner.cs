using System;
using System.Collections.Generic;

namespace Kyu_BT
{
    /// <summary>
    /// 행동 트리의 실행을 관리하는 클래스.
    /// 트리의 Tick, 트레이스(실행 경로 기록), 리셋, 해제 등을 담당.
    /// </summary>
    public class BehaviorTreeRunner : IDisposable
    {
        private readonly Node _root;
        private readonly Blackboard _bb;
        private readonly bool _trace;
        private readonly List<NodeExecutionRecord> _traceBuffer = new(64);
        private readonly List<Node> _runningChain = new(16);

        private bool _stopped = false; // Runner의 동작 정지 상태

        public bool IsStopped => _stopped; // 외부에서 정지 상태 확인용 프로퍼티

        public IReadOnlyList<NodeExecutionRecord> LastTrace => _traceBuffer;
        public IReadOnlyList<Node> LastRunningChain => _runningChain;
        public Node LastRunningLeaf { get; private set; }
        public NodeState LastRootState { get; private set; }

        /// <summary>
        /// Runner를 정지 상태로 전환
        /// </summary>
        public void Stop()
        {
            _stopped = true;
        }

        /// <summary>
        /// Runner를 다시 동작 상태로 전환
        /// </summary>
        public void Resume()
        {
            _stopped = false;
        }

        /// <summary>
        /// 생성자. 루트 노드와 블랙보드, 트레이스 활성화 여부를 받음.
        /// </summary>
        public BehaviorTreeRunner(Node root, Blackboard bb, bool enableTrace = true)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _bb = bb ?? throw new ArgumentNullException(nameof(bb));
            _trace = enableTrace;

            if (_trace)
                Node.NodeTicked += OnNodeTicked; // 트레이스 시 Tick 이벤트 구독
        }

        /// <summary>
        /// 트리 실행(Tick). 트레이스가 켜져 있으면 실행 경로 기록.
        /// </summary>
        public NodeState Tick()
        {
            if (_stopped)
                return LastRootState; // 정지 상태면 이전 상태 반환

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

        /// <summary>
        /// 노드가 Tick될 때마다 호출되어 실행 기록 및 Running 체인 관리.
        /// </summary>
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

        /// <summary>
        /// 현재 Running 체인을 문자열로 반환.
        /// </summary>
        public string GetRunningPathString()
        {
            if (_runningChain.Count == 0) return "(idle)";
            return string.Join(" -> ", _runningChain);
        }

        /// <summary>
        /// 트리와 트레이스 상태를 리셋.
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
        /// 트레이스 이벤트 구독 해제 등 리소스 해제.
        /// </summary>
        public void Dispose()
        {
            if (_trace)
                Node.NodeTicked -= OnNodeTicked;
        }
    }

    /// <summary>
    /// 노드 실행 기록 구조체. 노드, 상태, 타입명 저장.
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
