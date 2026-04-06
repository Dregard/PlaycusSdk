using System;
using UnityEngine;

namespace Playcus
{
    /// <summary>
    /// Bind instance to ServiceLocator by target component class
    /// Documentation https://docs.google.com/document/d/1A6Ce52Xh3iLBiVgYKnjTHNCm4g21k5jln3PdMnZE0To/edit#
    /// </summary>
    public class ServiceClassBinder : MonoBehaviour
    {
        [HelpBox(@"Bind instance to ServiceLocator by target component class", HelpBoxMessageType.Info)]
        [SerializeField] private Component BindedComponent;
        [SerializeField] private bool bindOnlyIfNotBinded;

        private void Awake()
        {
            BindService();
        }

        public void BindService()
        {
            Type BindedType = BindedComponent.GetType();
            if (bindOnlyIfNotBinded)
            {
                if (!ServiceLocator.IsBind(BindedType))
                    ServiceLocator.Bind(BindedType, BindedComponent);
            }
            else
            {
                ServiceLocator.Bind(BindedType, BindedComponent);
            }
        }
    }
}