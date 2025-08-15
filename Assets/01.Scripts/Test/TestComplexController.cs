using System.Collections;
using System.Collections.Generic;
using Akasha;
using UnityEngine;


public class TestComplexController : EMController<TestEntity, TestUnitModel>
{
    [Header("Complex Controller Settings")]
    public bool enableDebugLogs = true;

    protected override void AtAwake()
    {
        if (enableDebugLogs)
            Debug.Log($"[{GetAggregateId()}] AtAwake - Complex Controller");
    }

    protected override void AtModelReady()
    {
        if (enableDebugLogs)
            Debug.Log($"[{GetAggregateId()}] Model Ready - Unit: {Model.unitName.Value}");
    }

    protected override void AtInit()
    {
        if (enableDebugLogs)
            Debug.Log($"[{GetAggregateId()}] AtInit - Complex controller initialized");

        Entity.TestPartInteraction();
    }

    public void ModifyUnitHealth(float amount)
    {
        var currentHealth = Model.health.Value;
        Model.health.Set(currentHealth + (int)amount);

        if (enableDebugLogs)
            Debug.Log($"[{GetAggregateId()}] Modified health by {amount}. New health: {Model.health.Value}");
    }
}
