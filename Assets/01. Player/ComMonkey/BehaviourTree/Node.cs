using System;
using UnityEngine;

namespace Kyu_BT
{
    public abstract class Node
    {
        protected NodeState _state = NodeState.Failure;

        public NodeState Tick(Blackboard bb)
        {
            if (_state != NodeState.Running)
                OnEnter(bb);

            NodeState newState;
            try
            {
                newState = OnTick(bb);
            }
            catch (Exception ex)
            {
                Debug.LogError($"OnTick exception: {ex}");
                newState = NodeState.Failure;
            }

            if (newState != NodeState.Running && _state == NodeState.Running)
                OnExit(bb);

            _state = newState;
            return _state;
        }

        protected virtual void OnEnter(Blackboard bb) { }
        protected abstract NodeState OnTick(Blackboard bb);
        protected virtual void OnExit(Blackboard bb) { }

        public virtual void Reset(Blackboard bb)
        {
            if (_state == NodeState.Running) OnExit(bb);
            _state = NodeState.Failure;
        }
    }
}
