using System.Collections;
using System.Collections.Generic;
using Akasha;
using UnityEngine;

public class LifeEntity : BaseEntity<LifeModel>
{
    protected override void SetupModel()
    {
        Debug.Log("EntitySetupModel");
        Model = new LifeModel();
    }
    protected override void AtAwake()
    {
        Debug.Log("EntityAtAwake");
    }
    protected override void AtInit()
    {
        Debug.Log("EntityAtInit");
    }
    protected override void AtStart()
    {
        Debug.Log("EntityAtStart");
    }
}
