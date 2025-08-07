using System.Collections;
using System.Collections.Generic;
using Akasha;
using UnityEngine;

public class TestController : EMController<TestEntity, TestModel>
{
    protected override void AtAwake()
    {
        isPoolObject = true;
        Debug.Log("TestController AtAwake");
    }

    protected override void AtInit()
    {
        Debug.Log("TestController AtInit");
    }
    protected override void AtStart()
    {
        Debug.Log("TestController AtStart");
    }
    protected override void AtLateStart()
    {
        Debug.Log("TestController AtLateStart");
        gameObject.SetActive(false);
        gameObject.SetActive(true);
    }

    protected override void AtModelReady()
    {
        Debug.Log("TestController AtModelReady");
    }
    protected override void AtSave()
    {
        Debug.Log("TestController AtSave");
    }
    protected override void AtLoad()
    {
        Debug.Log("TestController AtLoad");
    }

    protected override void AtEnable()
    {
        Debug.Log("TestController AtEnable");
    }
    protected override void AtDisable()
    {
        Debug.Log("TestController AtDisable");
    }
    protected override void AtPoolInit()
    {
        Debug.Log("TestController AtPoolInit");
    }
    protected override void AtPoolDeinit()
    {
        Debug.Log("TestController AtPoolDeinit");
    }
    protected override void AtDeinit()
    {
        Debug.Log("TestController AtDeinit");
    }
    protected override void AtDestroy()
    {
        Debug.Log("TestController AtDestroy");
    }
}
