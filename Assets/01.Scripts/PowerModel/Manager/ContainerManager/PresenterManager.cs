using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Akasha
{
    public class PresenterManager : ContainerManager<BasePresenter>
    {
        public override int InitializationPriority => 70;

        protected override void OnAggregateRegistered(BasePresenter presenter)
        {
            Debug.Log($"[PresenterManager] Registered Presenter: {presenter.GetType().Name}");
        }

        protected override void OnAggregateUnregistered(BasePresenter presenter)
        {
            Debug.Log($"[PresenterManager] Unregistered Presenter: {presenter.GetType().Name}");
        }

        public IEnumerable<BasePresenter> GetPresentersByType<TPresenter>() where TPresenter : BasePresenter
        {
            return GetAll().OfType<TPresenter>();
        }

        public TPresenter GetPresenter<TPresenter>() where TPresenter : BasePresenter
        {
            return GetAll().OfType<TPresenter>().FirstOrDefault();
        }

        public TPresenter SpawnPresenter<TPresenter>() where TPresenter : BasePresenter
        {
            return Spawn<TPresenter>();
        }

        public TPresenter SpawnPresenter<TPresenter>(Vector3 position, Quaternion rotation) where TPresenter : BasePresenter
        {
            return Spawn<TPresenter>(position, rotation);
        }

        public TPresenter SpawnPresenter<TPresenter>(Transform parent) where TPresenter : BasePresenter
        {
            return Spawn<TPresenter>(parent);
        }

        public TPresenter SpawnPresenterFromPrefab<TPresenter>(TPresenter prefab) where TPresenter : BasePresenter
        {
            return SpawnFromPrefab(prefab);
        }

        public TPresenter SpawnPresenterOrCreate<TPresenter>(TPresenter prefab) where TPresenter : BasePresenter
        {
            return SpawnOrCreate(prefab);
        }

        public bool ReturnPresenter(BasePresenter presenter)
        {
            return ReturnToPool(presenter);
        }

        public void ReturnAllPresenters()
        {
            ReturnAllToPool();
        }

        public void ReturnAllPresentersOfType<TPresenter>() where TPresenter : BasePresenter
        {
            ReturnAllOfType<TPresenter>();
        }

        public void PrewarmPresenterPool<TPresenter>(TPresenter prefab, int count) where TPresenter : BasePresenter
        {
            PrewarmPool(prefab, count);
        }

        public void ClearPresenterPool<TPresenter>() where TPresenter : BasePresenter
        {
            ClearPool<TPresenter>();
        }

        public void ShowAll()
        {
            foreach (var presenter in GetActive())
            {
                presenter.Show();
            }
            Debug.Log("[PresenterManager] Showed all active presenters");
        }

        public void HideAll()
        {
            foreach (var presenter in GetActive())
            {
                presenter.Hide();
            }
            Debug.Log("[PresenterManager] Hid all active presenters");
        }

        public void ShowPresentersByType<TPresenter>() where TPresenter : BasePresenter
        {
            foreach (var presenter in GetPresentersByType<TPresenter>())
            {
                if (!presenter.IsInPool)
                {
                    presenter.Show();
                }
            }
            Debug.Log($"[PresenterManager] Showed all active {typeof(TPresenter).Name} presenters");
        }

        public void HidePresentersByType<TPresenter>() where TPresenter : BasePresenter
        {
            foreach (var presenter in GetPresentersByType<TPresenter>())
            {
                if (!presenter.IsInPool)
                {
                    presenter.Hide();
                }
            }
            Debug.Log($"[PresenterManager] Hid all active {typeof(TPresenter).Name} presenters");
        }

        public int GetPresenterPoolCount<TPresenter>() where TPresenter : BasePresenter
        {
            return GetPoolCount<TPresenter>();
        }

        public int GetActivePresenterCount<TPresenter>() where TPresenter : BasePresenter
        {
            return GetActiveCount<TPresenter>();
        }

        public bool HasPresenterPool<TPresenter>() where TPresenter : BasePresenter
        {
            return HasPool<TPresenter>();
        }

        public int GetTotalViewCount()
        {
            return GetActive().Sum(p => p.ViewCount);
        }

        public int GetActivePresenterCount()
        {
            return GetActive().Count();
        }
    }
}