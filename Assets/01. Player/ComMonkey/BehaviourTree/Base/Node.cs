using System;
using UnityEngine;

namespace Kyu_BT
{
    public abstract class Node
    {
        // ---- �߰�: ��� Tick ���� �̺�Ʈ (Runner �� ����) ----
        public static event Action<Node, NodeState> NodeTicked;

        // ����: ����׿� �̸� (���� ���ϸ� Ÿ�Ը�)
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

            // ---- �߰�: Tick ��� ��� ----
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
