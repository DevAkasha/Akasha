using UnityEngine;

namespace Akasha
{
    public abstract class Goldbug : RxContextBehaviour, IRxUnsafe
    {
        protected override void OnInit()
        {
            RegisterModel();      // RxVar, RxExpr 생성
            RegisterFlags();      // RxFlag 생성
            SetupReactive();      // ReactiveField 구독
            SetupViewBinding();   // UI 바인딩
            SetupEntityLogic();   // 모델 기반 로직 처리
            OnGoldbugInitialized();
        }
        protected virtual void RegisterModel() { }

        protected virtual void RegisterFlags() { }

        protected virtual void SetupReactive() { }

        protected virtual void SetupViewBinding() { }

        protected virtual void SetupEntityLogic() { }

        protected virtual void OnGoldbugInitialized() { }

        protected override void OnDispose()
        {
            base.OnDispose();
            OnTeardown();
        }

        protected virtual void OnTeardown() { }
    }
}