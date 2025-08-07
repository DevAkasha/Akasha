using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Akasha
{
    public abstract class BaseEntity : MonoBehaviour, IModelOwner, IRxCaller
    {
        public bool IsLogicalCaller => true;
        public bool IsMultiRolesCaller => false;
        public bool IsFunctionalCaller => true;

        public abstract BaseModel GetBaseModel();
    }

    public abstract class BaseEntity<M> : BaseEntity, IModelOwner<M> where M : BaseModel
    {
        private readonly Dictionary<Type, BasePart> partsByType = new();
        private readonly List<BasePart> allParts = new();
        
        protected bool isInit = false;
        private EMController controller;

        public M Model { get; set; }

        public override BaseModel GetBaseModel() => Model;
        public M GetModel() => Model;

        private void Awake()
        {
            enabled = false;
            controller = GetComponentInParent<EMController>();
            controller.RegistEntity(this);
            AtAwake();
        }

        public void SetEnable()
        {
            enabled = true;
            Model = SetupModel();
            InitializeParts();
            AtModelReady();
            controller.CallModelReady();
        }

        public void CallInit()
        {
            foreach (var part in allParts)
            {
                part.CallInit();
            }
            AtInit();
        }

        private void InitializeParts()
        {
            var parts = GetComponentsInChildren<BasePart>();
            foreach (BasePart part in parts)
            {
                allParts.Add(part);
                partsByType[part.GetType()] = part;

                part.enabled = true;
                part.RegistEntity(this);
            }
        }

        public void CallStart()
        {
            foreach (var part in allParts)
            {
                part.CallStart();
            }
            AtStart();
        }

        public void CallLateStart()
        {
            foreach (var part in allParts)
            {
                part.CallLateStart();
            }
            AtLateStart();
        }

        public void CallPoolInit()
        {
            foreach (var part in allParts)
            {
                part.CallPoolInit();
            }
            AtPoolInit();
        }

        public void CallEnable()
        {
            foreach (var part in allParts)
            {
                part.CallEnable();
            }
            AtEnable();
        }

        public void CallPoolDeinit()
        {
            foreach (var part in allParts)
            {
                part.CallPoolDeinit();
            }
            AtPoolDeinit();
        }

        public void CallDisable()
        {
            foreach (var part in allParts)
            {
                part.CallDisable();
            }
            AtDisable();
        }

        public void CallSave()
        {
            foreach (var part in allParts)
            {
                part.CallSave();
            }
            AtSave();
        }

        public void CallLoad()
        {
            foreach (var part in allParts)
            {
                part.CallLoad();
            }
            AtLoad();
        }

        public void CallDeinit()
        {
            foreach (var part in allParts)
            {
                part.CallDeinit();
            }
            AtDeinit();
        }

        public void CallDestroy()
        {
            foreach (var part in allParts)
            {
                part.CallDestroy();
            }
            AtDestroy();
        }

        public T GetPart<T>() where T : BasePart
        {
            partsByType.TryGetValue(typeof(T), out var part);
            return part as T;
        }

        public IEnumerable<T> GetParts<T>() where T : BasePart
        {
            return allParts.OfType<T>();
        }

        public void NotifyAllParts(string methodName, params object[] parameters)
        {
            foreach (var part in allParts)
            {
                var method = part.GetType().GetMethod(methodName);
                method?.Invoke(part, parameters);
            }
        }

        protected virtual void AtAwake() { }
        protected virtual void AtEnable() { }
        protected virtual void AtInit() { }
        protected virtual void AtStart() { }
        protected virtual void AtLateStart() { }

        protected virtual void AtPoolInit() { }
        protected virtual void AtPoolDeinit() { }

        protected virtual void AtModelReady() { }
        protected virtual void AtLoad() { }
        protected virtual void AtSave() { }

        protected virtual void AtDeinit() { }
        protected virtual void AtDisable() { }
        protected virtual void AtDestroy() { }

        protected abstract M SetupModel();
    }
}