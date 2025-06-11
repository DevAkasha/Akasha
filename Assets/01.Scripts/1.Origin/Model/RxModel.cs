using System;
using Akasha;

namespace Akasha
{
    public abstract class RxModel : IRxModelOwner
    {
        public object? ReactiveOwner { get; private set; }

        public void SetReactiveOwner(object owner)
        {
            ReactiveOwner = owner;
        }
    }
}