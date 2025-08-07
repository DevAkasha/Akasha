using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Akasha;

public class TestPart : BasePart<TestEntity,TestModel>
{
    protected override void AtAwake()
    {
        Debug.Log("TestPart AtAwake");
    }

    protected override void AtInit()
    {
        Debug.Log("TestPart AtInit");
    }
    protected override void AtStart()
    {
        Debug.Log("TestPart AtStart");
    }
    protected override void AtLateStart()
    {
        Debug.Log("TestPart AtLateStart");
    }

    protected override void AtModelReady()
    {
        Debug.Log("TestPart AtModelReady");
    }
    protected override void AtSave()
    {
        Debug.Log("TestPart AtSave");
    }
    protected override void AtLoad()
    {
        Debug.Log("TestPart AtLoad");
    }

    protected override void AtEnable()
    {
        Debug.Log("TestPart AtEnable");
    }
    protected override void AtDisable()
    {
        Debug.Log("TestPart AtDisable");
    }
    protected override void AtPoolInit()
    {
        Debug.Log("TestPart AtPoolInit");
    }
    protected override void AtPoolDeinit()
    {
        Debug.Log("TestPart AtPoolDeinit");
    }

    protected override void AtDeinit()
    {
        Debug.Log("TestPart AtDeinit");
    }
    protected override void AtDestroy()
    {
        Debug.Log("TestPart AtDestroy");
    }

}
