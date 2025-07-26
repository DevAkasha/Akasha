using System.Collections;
using System.Collections.Generic;
using Akasha;
using UnityEngine;

public class LifeController : EMController<LifeEntity,LifeModel>
{
    protected override void AtAwake()
    {
        Debug.Log("ControllerAtAwake");
    }
    protected override void AtInit()
    {
        Debug.Log("ControllerAtInit");
    }
    protected override void AtStart()
    {
        Debug.Log("ControllerAtStart");
    }
}
