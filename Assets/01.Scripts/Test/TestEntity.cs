using System.Collections;
using System.Collections.Generic;
using Akasha;
using UnityEngine;

public class TestEntity : BaseEntity<TestUnitModel>
{
    protected override void AtAwake()
    {
        Debug.Log($"[Entity] AtAwake");
    }

    protected override TestUnitModel SetupModel()
    {
        return new TestUnitModel($"Entity_{GetInstanceID()}");
    }

    protected override void AtModelReady()
    {
        Debug.Log($"[Entity] Model Ready - Unit: {Model.unitName.Value}");
    }

    protected override void AtInit()
    {
        Debug.Log($"[Entity] AtInit - Entity initialized");
    }

    public void TestPartInteraction()
    {
        var testPart = GetPart<TestEntityPart>();
        testPart?.DoSomething();
    }
}
