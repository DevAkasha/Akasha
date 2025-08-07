using System.Collections;
using System.Collections.Generic;
using Akasha;
using UnityEngine;

public class TestEntity : BaseEntity<TestModel>
{
    protected override void AtAwake()
    {
        Debug.Log("TestEntity AtAwake");
    }

    protected override void AtInit()
    {
        Debug.Log("TestEntity AtInit");
    }
    protected override void AtStart()
    {
        Debug.Log("TestEntity AtStart");
    }
    protected override void AtLateStart()
    {
        Debug.Log("TestEntity AtLateStart");
    }

    protected override void AtModelReady()
    {
        Debug.Log("TestEntity AtModelReady");
    }
    protected override void AtSave()
    {
        Debug.Log("TestEntity AtSave");
    }
    protected override void AtLoad()
    {
        Debug.Log("TestEntity AtLoad");
    }

    protected override void AtEnable()
    {
        Debug.Log("TestEntity AtEnable");
    }
    protected override void AtDisable()
    {
        Debug.Log("TestEntity AtDisable");
    }
    protected override void AtPoolInit()
    {
        Debug.Log("TestEntity AtPoolInit");
    }
    protected override void AtPoolDeinit()
    {
        Debug.Log("TestEntity AtPoolDeinit");
    }

    protected override void AtDeinit()
    {
        Debug.Log("TestEntity AtDeinit");
    }
    protected override void AtDestroy()
    {
        Debug.Log("TestEntity AtDestroy");
    }

    protected override TestModel SetupModel()
    {
        Debug.Log("TestEntity SetupModel");
        return new TestModel();
    }
}
