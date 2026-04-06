using System;


public static class ReflectionExtensions
{
    public static bool ImplementsInterface(this Type type, Type checkType)
    {
        Type[] intf = type.GetInterfaces();
        for (int i = 0; i < intf.Length; i++)
        {
            if (intf[i] == checkType)
            {
                return true;
            }
        }

        return false;
    }

    /*

        public static bool IsAnonymousType(this Type type)
        {
            Debug.Assert(type != null, "Type should not be null");

            // HACK: The only way to detect anonymous types right now.
            return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false)
                   && type.IsGenericType && type.Name.Contains("AnonymousType")
                   && (type.Name.StartsWith("<>", StringComparison.OrdinalIgnoreCase) ||
                       type.Name.StartsWith("VB$", StringComparison.OrdinalIgnoreCase))
                   && (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
        }*/
}
