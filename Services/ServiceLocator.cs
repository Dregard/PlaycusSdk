using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Playcus
{
    /// <summary>
    /// Contain binds class or interface to instances
    /// Better alternative to Singletones
    /// Documentation https://docs.google.com/document/d/1A6Ce52Xh3iLBiVgYKnjTHNCm4g21k5jln3PdMnZE0To/edit#
    /// </summary>
    public static class ServiceLocator
    {
        private static Dictionary<object, object> _services = new Dictionary<object, object>();


        /// <summary>
        /// Bind instance with class or interface
        /// </summary>
        public static void Bind<T>(object instance)
        {
            Bind(typeof(T), instance);
        }


        /// <summary>
        /// Bind instance with class or interface
        /// </summary>
        public static void Bind(Type bindedType, object instance)
        {
            if (!_services.ContainsKey(bindedType))
            {
                Debug.Log("ServiceLocator: Bind new " + bindedType.ToString());
                _services.Add(bindedType, instance);
            }
            else
            {
                Debug.Log("ServiceLocator: Bind override " + bindedType.ToString());
                _services[bindedType] = instance;
            }
        }



        /// <summary>
        /// Unbind instance with class or interface
        /// </summary>
        public static void Unbind<T>()
        {
            Unbind(typeof(T));
        }

        /// <summary>
        /// Unbind instance with class or interface
        /// </summary>
        public static void Unbind(Type bindedType)
        {
            Debug.Log("ServiceLocator: Unbind " + bindedType.ToString());
            _services.Remove(bindedType);
        }

        /// <summary>
        /// Return true if target type has binded instance
        /// </summary>
        public static bool IsBind<T>()
        {
            return IsBind(typeof(T));
        }

        /// <summary>
        /// Return true if target type has binded instance
        /// </summary>
        public static bool IsBind(Type bindedType)
        {
            return _services.ContainsKey(bindedType);
        }

        /// <summary>
        /// Get instance by class or interface
        /// </summary>
        public static T Get<T>(bool withoutWarnings = false)
        {
            try
            {
                return (T)_services[typeof(T)];
            }
            catch (KeyNotFoundException)
            {
                if (!withoutWarnings)
                    Debug.LogWarning("ServiceLocator: The requested service is not registered " + typeof(T).ToString());
                return default(T);
            }
        }

        /// <summary>
        /// Get instance by class or interface
        /// </summary>
        public static object Get(Type bindedType, bool withoutWarnings = false)
        {
            try
            {
                return _services[bindedType];
            }
            catch (KeyNotFoundException)
            {
                if (!withoutWarnings)
                    Debug.LogWarning("ServiceLocator: The requested service is not registered " + bindedType.ToString());
                return null;
            }
        }


        /// <summary>
        /// Find [ServiceBind] attribute in all components on targetObject and try to bind they
        /// </summary>
        public static void BindServicesFromObject(GameObject targetObject)
        {
            Component[] allComponents = targetObject.GetComponents<Component>();

            foreach (Component component in allComponents)
            {
                BindServicesFromComponent(component);
            }
        }

        /// <summary>
        /// Find [ServiceBind] attribute in all components on targetObject and try to bind they
        /// </summary>
        public static void BindServicesFromComponent(Component component)
        {
            Type monoType = component.GetType();
            ServiceBindAttribute attribute = Attribute.GetCustomAttribute(monoType, typeof(ServiceBindAttribute)) as ServiceBindAttribute;
            if (attribute != null)
            {
                Bind(attribute.bindedType, component);
            }

        }

        /// <summary>
        /// Find all fields in all components on targetObject with [ServiceResolve] attribute and try to resolve they
        /// </summary>
        internal static void ResolveServicesInObject(GameObject targetObject, bool withoutWarnings = false)
        {
            Component[] allComponents = targetObject.GetComponents<Component>();

            foreach (Component component in allComponents)
            {
                ResolveServicesInComponent(component, withoutWarnings);
            }
        }

        /// <summary>
        /// Find all fields in  component with [ServiceResolve] attribute and try to resolve they
        /// </summary>
        internal static void ResolveServicesInComponent(Component component, bool withoutWarnings = false)
        {
            Type monoType = component.GetType();

            // Retreive the fields from the mono instance
            FieldInfo[] objectFields = monoType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            // search all fields and find the attribute [Position]
            for (int i = 0; i < objectFields.Length; i++)
            {
                InjectService attribute = Attribute.GetCustomAttribute(objectFields[i], typeof(InjectService)) as InjectService;
                if (attribute != null)
                {
                    objectFields[i].SetValue(component, Get(objectFields[i].FieldType,withoutWarnings));
                }
            }

        }
    }
}
