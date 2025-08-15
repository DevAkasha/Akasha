using System.Collections;
using System.Collections.Generic;
using Akasha;
using UnityEngine;

public class TestSimpleController : EMController<TestSimpleEntity, TestSimpleModel>
{
    public int testValue = 0;
    private TimerHandle timerHandle;

    protected override void AtAwake()
    {
        Debug.Log($"[{GetAggregateId()}] AtAwake - Simple Controller");
    }

    protected override void AtInit()
    {
        Debug.Log($"[{GetAggregateId()}] AtInit - Starting timer test");
        timerHandle = this.RepeatingCall(1.0f, OnTimerTick);
    }

    protected override void AtDeinit()
    {
        Debug.Log($"[{GetAggregateId()}] AtDeinit - Stopping timer");
        timerHandle?.Cancel();
    }

    private void OnTimerTick()
    {
        testValue++;
        Debug.Log($"[{GetAggregateId()}] Timer tick: {testValue}");
    }

    protected override void AtPoolInit()
    {
        Debug.Log($"[{GetAggregateId()}] AtPoolInit - Reactivated from pool");
        testValue = 0;
    }

    protected override void AtPoolDeinit()
    {
        Debug.Log($"[{GetAggregateId()}] AtPoolDeinit - Returning to pool");
        timerHandle?.Cancel();
    }
}