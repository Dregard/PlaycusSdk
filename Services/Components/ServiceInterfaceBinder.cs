using System;
using UnityEngine;

namespace Playcus
{
    /// <summary>
    /// Bind instance to ServiceLocator first interface implemented by target component
    /// Documentation https://docs.google.com/document/d/1A6Ce52Xh3iLBiVgYKnjTHNCm4g21k5jln3PdMnZE0To/edit#
    /// </summary>
    public class ServiceInterfaceBinder : MonoBehaviour
    {
        [HelpBox(@"Bind instance to ServiceLocator first interface implemented by target component", HelpBoxMessageType.Info)]
        [SerializeField] private Component BindedComponent;

        [SerializeField] private bool bindOnlyIfNotBinded;

        private void Awake()
        {
            BindService();
        }

        public void BindService()
        {
            Type BindedInterface = BindedComponent.GetType().GetInterfaces()[0];
            if (bindOnlyIfNotBinded)
            {
                if (!ServiceLocator.IsBind(BindedInterface))
                    ServiceLocator.Bind(BindedInterface, BindedComponent);
            }
            else
            {
                ServiceLocator.Bind(BindedInterface, BindedComponent);
            }
        }
    }
}