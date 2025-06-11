using Akasha;
using UnityEngine;

public class RxContextBehaviour : MonoBehaviour
{
    [SerializeField] private bool debugRx = false;

    protected BaseController? controllerContext;
    protected BasePresenter? presenterContext;

    public object? ReactiveRoot => (object?)controllerContext ?? presenterContext;

    public void InjectControllerContext(BaseController controller)
    {
        controllerContext = controller;
    }

    public void InjectPresenterContext(BasePresenter presenter)
    {
        presenterContext = presenter;
    }

    protected virtual void Awake()
    {
        if (debugRx)
            Debug.Log($"[RxContextBehaviour] {GetType().Name} 초기화: {RxFlow.Snapshot()}");
        OnInit();
    }

    protected virtual void OnInit() { }

    protected virtual void OnDispose() { }

    protected virtual void OnDestroy()
    {
        OnDispose();
        RxBinder.UnbindAll(this);

        if (debugRx)
            Debug.Log($"[RxContextBehaviour] {GetType().Name} 해제됨");
    }
}