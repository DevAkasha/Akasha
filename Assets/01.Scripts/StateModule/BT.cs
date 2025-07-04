using System;
using System.Collections.Generic;

namespace Akasha.State
{
    public enum NodeStatus
    {
        Success,
        Failure,
        Running
    }

    public interface IBehaviorNode
    {
        NodeStatus Execute();
    }

    public class BehaviorTree
    {
        private readonly IBehaviorNode rootNode;
        private NodeStatus lastStatus = NodeStatus.Failure;

        public BehaviorTree(IBehaviorNode rootNode)
        {
            this.rootNode = rootNode;
        }

        public NodeStatus Update()
        {
            if (rootNode == null)
                return NodeStatus.Failure;

            lastStatus = rootNode.Execute();
            return lastStatus;
        }

        public NodeStatus LastStatus => lastStatus;
    }

    public abstract class BehaviorNode : IBehaviorNode
    {
        public abstract NodeStatus Execute();
    }

    public abstract class CompositeNode : BehaviorNode
    {
        protected List<IBehaviorNode> children = new List<IBehaviorNode>();

        public CompositeNode(params IBehaviorNode[] nodes)
        {
            children.AddRange(nodes);
        }

        public CompositeNode AddChild(IBehaviorNode node)
        {
            children.Add(node);
            return this;
        }
    }

    public abstract class DecoratorNode : BehaviorNode
    {
        protected IBehaviorNode child;

        public DecoratorNode(IBehaviorNode child)
        {
            this.child = child;
        }
    }

    public abstract class ActionNode : BehaviorNode
    {
    }

    public abstract class ConditionNode : BehaviorNode
    {
    }

    public class Selector : CompositeNode
    {
        private int currentChildIndex = 0;

        public Selector(params IBehaviorNode[] nodes) : base(nodes) { }

        public override NodeStatus Execute()
        {
            for (int i = currentChildIndex; i < children.Count; i++)
            {
                currentChildIndex = i;
                NodeStatus status = children[i].Execute();

                if (status == NodeStatus.Running)
                {
                    return NodeStatus.Running;
                }
                else if (status == NodeStatus.Success)
                {
                    currentChildIndex = 0;
                    return NodeStatus.Success;
                }
            }

            currentChildIndex = 0;
            return NodeStatus.Failure;
        }
    }

    public class Sequence : CompositeNode
    {
        private int currentChildIndex = 0;

        public Sequence(params IBehaviorNode[] nodes) : base(nodes) { }

        public override NodeStatus Execute()
        {
            for (int i = currentChildIndex; i < children.Count; i++)
            {
                currentChildIndex = i;
                NodeStatus status = children[i].Execute();

                if (status == NodeStatus.Running)
                {
                    return NodeStatus.Running;
                }
                else if (status == NodeStatus.Failure)
                {
                    currentChildIndex = 0;
                    return NodeStatus.Failure;
                }
            }

            currentChildIndex = 0;
            return NodeStatus.Success;
        }
    }

    public class Inverter : DecoratorNode
    {
        public Inverter(IBehaviorNode child) : base(child) { }

        public override NodeStatus Execute()
        {
            NodeStatus status = child.Execute();

            if (status == NodeStatus.Running)
                return NodeStatus.Running;

            if (status == NodeStatus.Success)
                return NodeStatus.Failure;

            return NodeStatus.Success;
        }
    }

    public class Condition : ConditionNode
    {
        private readonly Func<bool> condition;

        public Condition(Func<bool> condition)
        {
            this.condition = condition;
        }

        public override NodeStatus Execute()
        {
            return condition() ? NodeStatus.Success : NodeStatus.Failure;
        }
    }

    public class BehaviorAction : ActionNode
    {
        private readonly Func<NodeStatus> action;

        public BehaviorAction(Func<NodeStatus> action)
        {
            this.action = action;
        }

        public BehaviorAction(System.Action simpleAction)
            : this(() => { simpleAction(); return NodeStatus.Success; })
        {
        }

        public override NodeStatus Execute()
        {
            return action();
        }
    }

    public class FSMStateCondition<TState> : ConditionNode where TState : Enum
    {
        private readonly FSM<TState> fsm;
        private readonly TState targetState;

        public FSMStateCondition(FSM<TState> fsm, TState targetState)
        {
            this.fsm = fsm;
            this.targetState = targetState;
        }

        public override NodeStatus Execute()
        {
            return EqualityComparer<TState>.Default.Equals(fsm.Value, targetState)
                ? NodeStatus.Success
                : NodeStatus.Failure;
        }
    }

    public class SetFSMStateAction<TState> : ActionNode where TState : Enum
    {
        private readonly FSM<TState> fsm;
        private readonly TState targetState;

        public SetFSMStateAction(FSM<TState> fsm, TState targetState)
        {
            this.fsm = fsm;
            this.targetState = targetState;
        }

        public override NodeStatus Execute()
        {
            fsm.Request(targetState);
            return NodeStatus.Success;
        }
    }

    public class FlagCondition<TFlag> : ConditionNode where TFlag : Enum
    {
        private readonly RxStateFlagSet<TFlag> flagSet;
        private readonly TFlag flag;
        private readonly bool expectedValue;

        public FlagCondition(RxStateFlagSet<TFlag> flagSet, TFlag flag, bool expectedValue = true)
        {
            this.flagSet = flagSet;
            this.flag = flag;
            this.expectedValue = expectedValue;
        }

        public override NodeStatus Execute()
        {
            return flagSet.GetValue(flag) == expectedValue ? NodeStatus.Success : NodeStatus.Failure;
        }
    }

    public class SetFlagAction<TFlag> : ActionNode where TFlag : Enum
    {
        private readonly RxStateFlagSet<TFlag> flagSet;
        private readonly TFlag flag;
        private readonly bool value;

        public SetFlagAction(RxStateFlagSet<TFlag> flagSet, TFlag flag, bool value)
        {
            this.flagSet = flagSet;
            this.flag = flag;
            this.value = value;
        }

        public override NodeStatus Execute()
        {
            flagSet.SetValue(flag, value);
            return NodeStatus.Success;
        }
    }
}