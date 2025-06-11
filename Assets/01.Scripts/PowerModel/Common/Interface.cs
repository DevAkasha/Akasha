using System;
using System.Collections;
using System.Collections.Generic;

public interface IModifiableTarget
{
    IEnumerable<IModifiable> GetModifiables(); // 수정 가능한 필드 목록 반환
}

public interface IRxReadable<T>
{
    T Value { get; }
    void AddListener(Action<T> listener); // 값 변경을 구독할 수 있음
    void RemoveListener(Action<T> listener); // 구독 해제
}
public interface IRxField
{
    string FieldName { get; }
}

public interface IRxField<T> : IRxField, IRxReadable<T> { }

public interface IConditionCheckable
{
    bool Satisfies(Func<object, bool> predicate);
}

public interface IRxCaller
{
    bool IsLogicalCaller { get; }
    bool IsMultiRolesCaller { get; }
    bool IsFunctionalCaller { get; }
}

public interface IRxOwner
{
    bool IsRxVarOwner { get; }
    bool IsRxAllOwner { get; }

    void RegisterRx(RxBase rx);

    public void Unload();
}

public interface IModelOwner  // GetComponent용 엔티티 인터페이스
{
    BaseModel GetBaseModel();
}

public interface IModelOwner<M> : IModelOwner where M : BaseModel // 모델을 소유하는 엔티티 인터페이스
{
    public M Model { get; set; }

    public M GetModel(); //현재 모델 반환
}

