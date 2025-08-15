using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Akasha
{
    public class ControllerManager : ContainerManager<BaseController>
    {
        public override int InitializationPriority => 80;

        protected override void OnAggregateRegistered(BaseController controller)
        {
            Debug.Log($"[ControllerManager] Registered Controller: {controller.GetType().Name}");
        }

        protected override void OnAggregateUnregistered(BaseController controller)
        {
            Debug.Log($"[ControllerManager] Unregistered Controller: {controller.GetType().Name}");
        }

        public IEnumerable<BaseController> GetControllersByType<TController>() where TController : BaseController
        {
            return GetAll().OfType<TController>();
        }

        public TController GetController<TController>() where TController : BaseController
        {
            return GetAll().OfType<TController>().FirstOrDefault();
        }

        public TController SpawnController<TController>() where TController : BaseController
        {
            return Spawn<TController>();
        }

        public TController SpawnController<TController>(Vector3 position, Quaternion rotation) where TController : BaseController
        {
            return Spawn<TController>(position, rotation);
        }

        public TController SpawnController<TController>(Transform parent) where TController : BaseController
        {
            return Spawn<TController>(parent);
        }

        public TController SpawnControllerFromPrefab<TController>(TController prefab) where TController : BaseController
        {
            return SpawnFromPrefab(prefab);
        }

        public TController SpawnControllerOrCreate<TController>(TController prefab) where TController : BaseController
        {
            return SpawnOrCreate(prefab);
        }

        public bool ReturnController(BaseController controller)
        {
            return ReturnToPool(controller);
        }

        public void ReturnAllControllers()
        {
            ReturnAllToPool();
        }

        public void ReturnAllControllersOfType<TController>() where TController : BaseController
        {
            ReturnAllOfType<TController>();
        }

        public void PrewarmControllerPool<TController>(TController prefab, int count) where TController : BaseController
        {
            PrewarmPool(prefab, count);
        }

        public void ClearControllerPool<TController>() where TController : BaseController
        {
            ClearPool<TController>();
        }

        public int GetControllerPoolCount<TController>() where TController : BaseController
        {
            return GetPoolCount<TController>();
        }

        public int GetActiveControllerCount<TController>() where TController : BaseController
        {
            return GetActiveCount<TController>();
        }

        public bool HasControllerPool<TController>() where TController : BaseController
        {
            return HasPool<TController>();
        }

        public IEnumerable<Controller> GetControllers()
        {
            return GetAll().OfType<Controller>();
        }

        public IEnumerable<MController> GetMControllers()
        {
            return GetAll().OfType<MController>();
        }

        public IEnumerable<EMController> GetEMControllers()
        {
            return GetAll().OfType<EMController>();
        }

        public TController GetControllerOfType<TController>() where TController : Controller
        {
            return GetAll().OfType<TController>().FirstOrDefault();
        }

        public TMController GetMControllerOfType<TMController>() where TMController : MController
        {
            return GetAll().OfType<TMController>().FirstOrDefault();
        }

        public TEMController GetEMControllerOfType<TEMController>() where TEMController : EMController
        {
            return GetAll().OfType<TEMController>().FirstOrDefault();
        }

        public void ReturnAllControllersByType()
        {
            ReturnAllOfType<Controller>();
        }

        public void ReturnAllMControllersByType()
        {
            ReturnAllOfType<MController>();
        }

        public void ReturnAllEMControllersByType()
        {
            ReturnAllOfType<EMController>();
        }

        public Dictionary<System.Type, int> GetControllerTypeStatistics()
        {
            var stats = new Dictionary<System.Type, int>();

            foreach (var controller in GetAll())
            {
                var baseType = controller switch
                {
                    EMController => typeof(EMController),
                    MController => typeof(MController),
                    Controller => typeof(Controller),
                    _ => typeof(BaseController)
                };
                stats[baseType] = stats.GetValueOrDefault(baseType) + 1;
            }

            return stats;
        }
    }
}