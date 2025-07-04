using System;
using System.Collections.Generic;
using UnityEngine;

namespace Akasha
{
    public abstract class BaseViewModel : IBindable<BaseModel>
    {
        protected BaseView owner;
        protected BaseModel boundModel;
        private readonly List<Action> cleanupActions = new();

        public abstract Type ModelType { get; }

        public void SetOwner(BaseView view)
        {
            owner = view;
        }
        public virtual void Bind(BaseModel model)
        {
            boundModel = model;
            OnModelBound(model);
        }
        protected abstract void OnModelBound(BaseModel model);

        public virtual void Cleanup()
        {
            boundModel = null;
        }
    }

    public abstract class BaseViewModel<T> : BaseViewModel, IBindable<T> where T : BaseModel
    {
        protected T model;

        public override void Bind(BaseModel baseModel)
        {
            if (baseModel is T typedModel)
            {
                model = typedModel;
                base.Bind(baseModel);
                OnTypedModelBound(typedModel);
            }
        }
        public virtual void Bind(T typedModel)
        {
            model = typedModel;
            base.Bind(typedModel);
            OnTypedModelBound(typedModel);
        }

        protected override void OnModelBound(BaseModel model) { }


        protected abstract void OnTypedModelBound(T model);

    }
}