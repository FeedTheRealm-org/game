using System;
using UnityEngine;
using VContainer;

namespace FTR.Core.Common.Scopes
{
    [CreateAssetMenu(menuName = "Scriptable Objects/Object Resolver Container")]
    public class ObjectResolverContainer : ScriptableObject
    {
        public event Action OnResolverSet;

        private IObjectResolver resolver = null;

        public IObjectResolver Resolver => resolver;

        public void SetResolver(IObjectResolver resolver)
        {
            this.resolver = resolver;
            OnResolverSet?.Invoke();
        }
    }
}
