using System.Collections;
using System.Collections.Generic;
using Akasha;
using UnityEngine;

public class LifePart : BasePart<LifeEntity, LifeModel>
{
    protected override void AtAwake()
    {
        Debug.Log("PartAtAwake");
    }
    protected override void AtInit()
    {
        Debug.Log("PartAtInit");
    }
    protected override void AtStart()
    {
        Debug.Log("PartAtStart");
    }
}
