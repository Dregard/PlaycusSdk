using System;

namespace Playcus
{
    /// <summary>
    /// Documentation https://docs.google.com/document/d/1A6Ce52Xh3iLBiVgYKnjTHNCm4g21k5jln3PdMnZE0To/edit#
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class InjectService : Attribute
    {

    }

    /// <summary>
    /// Documentation https://docs.google.com/document/d/1A6Ce52Xh3iLBiVgYKnjTHNCm4g21k5jln3PdMnZE0To/edit#
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ServiceBindAttribute : Attribute
    {
        public readonly Type bindedType;

        public ServiceBindAttribute(Type bindedType)
        {
            this.bindedType = bindedType;
        }
    }
}

