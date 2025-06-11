using System;
using System.Collections.Generic;
using System.Text;

public sealed class RxModLong : RxModBase<long>
{
    private readonly Dictionary<ModifierKey, long> additives = new();
    private readonly Dictionary<ModifierKey, long> additiveMultipliers = new();
    private readonly Dictionary<ModifierKey, long> multipliers = new();
    private readonly Dictionary<ModifierKey, long> postMultiplicativeAdditives = new();
    private readonly HashSet<ModifierKey> signModifiers = new();

    private long sum, addMul, mul, postAdd;
    private bool isNegative;

    public RxModLong(long origin = 0L, string fieldName = null, IRxOwner owner = null)
    {
        if(!owner.IsRxAllOwner)
            throw new InvalidOperationException($"An invalid owner({owner}) has accessed.");
        this.origin = origin;
        cachedValue = origin;

        if (!string.IsNullOrEmpty(fieldName))
            FieldName = fieldName;

        owner.RegisterRx(this);

        Recalculate();
    }

    // 실제 계산 구현
    protected override void CalculateValue()
    {
        sum = origin;
        foreach (var v in additives.Values) sum += v;

        addMul = 1L;
        foreach (var v in additiveMultipliers.Values) addMul += v;

        mul = 1L;
        foreach (var v in multipliers.Values) mul *= v;

        postAdd = 0L;
        foreach (var v in postMultiplicativeAdditives.Values) postAdd += v;

        isNegative = signModifiers.Count % 2 == 1;

        long result = (sum * addMul * mul) + postAdd;
        cachedValue = isNegative ? -result : result;
    }

    protected override string BuildDebugFormula()
    {
        var sb = new StringBuilder();
        if (isNegative)
            sb.Append("-1 * ");

        sb.Append('(').Append(sum).Append(')');
        sb.Append(" * ").Append(addMul);
        sb.Append(" * ").Append(mul);
        sb.Append(" + ").Append(postAdd);

        return sb.ToString();
    }

    public override void ClearAll()
    {
        additives.Clear();
        additiveMultipliers.Clear();
        multipliers.Clear();
        postMultiplicativeAdditives.Clear();
        signModifiers.Clear();
        Recalculate(); // 즉시 재계산
    }

    public override void SetModifier(ModifierType type, ModifierKey key, long value)
    {
        switch (type)
        {
            case ModifierType.OriginAdd: additives[key] = value; break;
            case ModifierType.AddMultiplier: additiveMultipliers[key] = value; break;
            case ModifierType.Multiplier: multipliers[key] = value; break;
            case ModifierType.FinalAdd: postMultiplicativeAdditives[key] = value; break;
            default: throw new InvalidOperationException("Use AddModifier for SignFlip.");
        }
        Recalculate(); // 즉시 재계산
    }

    public override void AddModifier(ModifierType type, ModifierKey key)
    {
        if (type != ModifierType.SignFlip)
            throw new InvalidOperationException("Only SignFlip can be added without a value.");
        signModifiers.Add(key);
        Recalculate(); // 즉시 재계산
    }

    public override void RemoveModifier(ModifierType type, ModifierKey key)
    {
        bool removed = type switch
        {
            ModifierType.OriginAdd => additives.Remove(key),
            ModifierType.AddMultiplier => additiveMultipliers.Remove(key),
            ModifierType.Multiplier => multipliers.Remove(key),
            ModifierType.FinalAdd => postMultiplicativeAdditives.Remove(key),
            ModifierType.SignFlip => signModifiers.Remove(key),
            _ => false
        };
        if (removed) Recalculate(); // 변경 있을 때만 재계산
    }

    protected override bool AreValuesEqual(long a, long b)
    {
        return a == b;
    }
}