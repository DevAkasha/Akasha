using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseEntity : RxContextBehaviour
{
    protected override void OnInit()
    {
        base.OnInit();
        SetupModels();
        SetupParts();
    }

    protected abstract void SetupModels();

    protected virtual void SetupParts() { }
}