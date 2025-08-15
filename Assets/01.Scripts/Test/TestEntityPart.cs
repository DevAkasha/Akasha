using System.Collections;
using System.Collections.Generic;
using Akasha;
using UnityEngine;

public class TestEntityPart : BasePart<TestEntity, TestUnitModel>
{
    [Header("Part Settings")]
    public string partName = "TestPart";

    protected override void AtAwake()
    {
        Debug.Log($"[Part:{partName}] AtAwake");
    }

    protected override void AtModelReady()
    {
        Debug.Log($"[Part:{partName}] Model Ready - Health: {Model.health.Value}");
    }

    protected override void AtInit()
    {
        Debug.Log($"[Part:{partName}] AtInit - Part initialized");
    }

    public void DoSomething()
    {
        Debug.Log($"[Part:{partName}] DoSomething called - Current health: {Model.health.Value}");
    }
}