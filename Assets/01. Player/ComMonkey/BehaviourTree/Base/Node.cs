using System;
using UnityEngine;

namespace Kyu_BT
{
    public abstract class Node
    {
        // ---- 추가: 노드 Tick 추적 이벤트 (Runner 가 구독) ----
        public static event Action<Node, NodeState> NodeTicked;

        // 선택: 디버그용 이름 (설정 안하면 타입명)
        public string DebugName;

        protected bool _entered;
        protected NodeState _lastState = NodeState.Failure;

        public NodeState LastState => _lastState;
        public bool     IsRunning => _lastState == NodeState.Running;
        public bool     HasEntered => _entered;

        public NodeState Tick(Blackboard bb)
        {
            if (!_entered)
            {
                _entered = true;
                OnEnter(bb);
            }

            _lastState = OnTick(bb);

            if (_lastState != NodeState.Running)
            {
                OnExit(bb);
                _entered = false;
            }

            // ---- 추가: Tick 결과 방송 ----
            NodeTicked?.Invoke(this, _lastState);
            return _lastState;
        }

        public virtual void Reset(Blackboard bb)
        {
            if (_entered)
            {
                OnExit(bb);
                _entered = false;
            }
            _lastState = NodeState.Failure;
            OnReset(bb);
        }

        public override string ToString() => string.IsNullOrEmpty(DebugName) ? GetType().Name : DebugName;

        protected virtual void OnEnter(Blackboard bb) { }
        protected virtual void OnExit(Blackboard bb) { }
        protected virtual void OnReset(Blackboard bb) { }
        protected abstract NodeState OnTick(Blackboard bb);
    }
}
