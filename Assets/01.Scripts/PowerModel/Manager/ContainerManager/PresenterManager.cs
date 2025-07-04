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
            Debug.Log($"[PresenterManager] Registered BasePresenter: {presenter.GetType().Name}");
        }

        protected override void OnAggregateUnregistered(BasePresenter presenter)
        {
            Debug.Log($"[PresenterManager] Unregistered BasePresenter: {presenter.GetType().Name}");
        }

        public IEnumerable<BasePresenter> GetPresentersByType<T>() where T : BasePresenter
        {
            return GetAll().OfType<T>();
        }

        public T GetPresenter<T>() where T : BasePresenter
        {
            return GetAll().OfType<T>().FirstOrDefault();
        }

        public void ShowAll()
        {
            foreach (var presenter in GetAll())
            {
                presenter.Show();
            }
            Debug.Log("[PresenterManager] Showed all presenters");
        }

        public void HideAll()
        {
            foreach (var presenter in GetAll())
            {
                presenter.Hide();
            }
            Debug.Log("[PresenterManager] Hid all presenters");
        }

        public void ShowPresentersByType<T>() where T : BasePresenter
        {
            foreach (var presenter in GetPresentersByType<T>())
            {
                presenter.Show();
            }
            Debug.Log($"[PresenterManager] Showed all {typeof(T).Name} presenters");
        }

        public void HidePresentersByType<T>() where T : BasePresenter
        {
            foreach (var presenter in GetPresentersByType<T>())
            {
                presenter.Hide();
            }
            Debug.Log($"[PresenterManager] Hid all {typeof(T).Name} presenters");
        }

        public int GetTotalViewCount()
        {
            return GetAll().Sum(p => p.ViewCount);
        }
    }
}