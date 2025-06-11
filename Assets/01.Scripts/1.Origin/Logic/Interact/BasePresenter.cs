using UnityEngine;

namespace Akasha
{
    public abstract class BasePresenter : RxContextBehaviour, IPresenter, IInteractLogicalSubscriber
    {
        protected override void OnInit()
        {
            foreach (var child in GetComponentsInChildren<RxContextBehaviour>())
            {
                child.InjectPresenterContext(this);
            }
            SetupBindings();
        }

        protected abstract void SetupBindings();

        protected override void OnDispose()
        {
            base.OnDispose();
            TeardownBindings();
        }

        protected virtual void TeardownBindings() { }
    }
}