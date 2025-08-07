using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Akasha
{

    public class ControllerManager : ContainerManager<EMController>
    {
        public override int InitializationPriority => 80;

        protected override void OnAggregateRegistered(EMController controller)
        {
            Debug.Log($"[ControllerManager] Registered BaseController: {controller.GetType().Name}");
        }

        protected override void OnAggregateUnregistered(EMController controller)
        {
            Debug.Log($"[ControllerManager] Unregistered BaseController: {controller.GetType().Name}");
        }

        public IEnumerable<EMController> GetControllersByType<T>() where T : EMController
        {
            return GetAll().OfType<T>();
        }

        public T GetController<T>() where T : EMController
        {
            return GetAll().OfType<T>().FirstOrDefault();
        }
    }
}

