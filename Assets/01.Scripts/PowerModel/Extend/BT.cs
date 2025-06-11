using System;
using System.Collections.Generic;

// 노드 실행 상태를 나타내는 열거형
public enum NodeStatus
{
    Success,  // 노드가 성공적으로 완료됨
    Failure,  // 노드가 실패함
    Running   // 노드가 아직 실행 중임
}

// 모든 BT 노드의 기본 인터페이스
public interface IBehaviorNode
{
    NodeStatus Execute();
}

// 행동 트리를 관리하는 클래스
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

// 기본 노드 클래스 - 모든 노드의 기본 구현
public abstract class BehaviorNode : IBehaviorNode
{
    // 노드 실행 로직
    public abstract NodeStatus Execute();
}

// 컴포지트 노드 - 여러 자식 노드를 가지는 노드의 기본 클래스
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

// 데코레이터 노드 - 단일 자식 노드를 수정하는 노드
public abstract class DecoratorNode : BehaviorNode
{
    protected IBehaviorNode child;

    public DecoratorNode(IBehaviorNode child)
    {
        this.child = child;
    }
}

// 액션 노드 - 실제 동작을 수행하는 리프 노드
public abstract class ActionNode : BehaviorNode
{
    // 구체적인 액션 구현은 파생 클래스에서 정의
}

// 조건 노드 - 조건을 검사하는 리프 노드
public abstract class ConditionNode : BehaviorNode
{
    // 구체적인 조건 검사는 파생 클래스에서 정의
}

// 실행 결과에 따라 자식 노드 중 하나라도 성공하면 성공 반환
public class Selector : CompositeNode
{
    private int currentChildIndex = 0;

    public Selector(params IBehaviorNode[] nodes) : base(nodes) { }

    public override NodeStatus Execute()
    {
        // 이전에 실행 중이던 노드부터 시작
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
                currentChildIndex = 0;  // 성공 시 인덱스 초기화
                return NodeStatus.Success;
            }
            // 실패한 경우 다음 자식으로 진행
        }

        // 모든 자식이 실패하면 실패 반환
        currentChildIndex = 0;
        return NodeStatus.Failure;
    }
}

// 모든 자식이 성공해야 성공을 반환
public class Sequence : CompositeNode
{
    private int currentChildIndex = 0;

    public Sequence(params IBehaviorNode[] nodes) : base(nodes) { }

    public override NodeStatus Execute()
    {
        // 이전에 실행 중이던 노드부터 시작
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
                currentChildIndex = 0;  // 실패 시 인덱스 초기화
                return NodeStatus.Failure;
            }
            // 성공한 경우 다음 자식으로 진행
        }

        // 모든 자식이 성공하면 성공 반환
        currentChildIndex = 0;
        return NodeStatus.Success;
    }
}

// 자식 노드의 결과를 반전
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

// 조건 노드를 간단히 생성하는 유틸리티 클래스
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

// 액션 노드를 간단히 생성하는 유틸리티 클래스
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

// FSM 상태를 변경하는 액션 노드
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

// 플래그를 확인하는 조건 노드
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

// 플래그를 설정하는 액션 노드
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