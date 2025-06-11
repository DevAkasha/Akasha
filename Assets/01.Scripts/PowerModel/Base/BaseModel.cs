using System.Collections.Generic;



public abstract class BaseModel : IModifiableTarget, IRxCaller, IRxOwner
{
    bool IRxCaller.IsLogicalCaller => true;
    bool IRxCaller.IsMultiRolesCaller => true;
    bool IRxCaller.IsFunctionalCaller => true;

    bool IRxOwner.IsRxVarOwner => true;
    bool IRxOwner.IsRxAllOwner => true;

    private readonly HashSet<RxBase> trackedRxVars = new();
    private readonly HashSet<IModifiable> modifiables = new();

    public void RegisterRx(RxBase rx) // Rx 필드를 모델에 등록
    {
        if (trackedRxVars.Add(rx))
        {
            if (rx is IModifiable mod)
                RegisterModifiable(mod);
        }
    }

    protected void RegisterModifiable(IModifiable modifiable)
    {
        if (modifiable != null)
            modifiables.Add(modifiable);
    }

    public virtual IEnumerable<IModifiable> GetModifiables() => modifiables; // 수정 가능한 필드 목록 반환
    
    public IEnumerable<RxBase> GetAllRxFields() => trackedRxVars;

    public void Unload()
    {
        foreach (var rx in trackedRxVars)
        {
            rx.ClearRelation();
        }
        trackedRxVars.Clear();
        modifiables.Clear();
    }
}
