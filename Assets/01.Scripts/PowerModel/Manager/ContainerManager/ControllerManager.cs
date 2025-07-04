using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ControllerManager : ContainerManager<BaseController>
{
    public override int InitializationPriority => 80;

    protected override void OnAggregateRegistered(BaseController controller)
    {
        Debug.Log($"[ControllerManager] Registered BaseController: {controller.GetType().Name}");
    }

    protected override void OnAggregateUnregistered(BaseController controller)
    {
        Debug.Log($"[ControllerManager] Unregistered BaseController: {controller.GetType().Name}");
    }

    public IEnumerable<BaseController> GetControllersByType<T>() where T : BaseController
    {
        return GetAll().OfType<T>();
    }

    public T GetController<T>() where T : BaseController
    {
        return GetAll().OfType<T>().FirstOrDefault();
    }
}

