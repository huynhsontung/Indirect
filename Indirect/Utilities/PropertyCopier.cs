using System;
using System.Diagnostics.Contracts;

namespace Indirect.Utilities
{
    // Source: https://www.pluralsight.com/guides/property-copying-between-two-objects-using-reflection
    public static class PropertyCopier<TParent, TChild> where TParent : class where TChild : class
    {
        public static void Copy(TParent parent, TChild child)
        {
            Contract.Requires(parent != null, $"{nameof(PropertyCopier<TParent, TChild>)}: parent has to be not null");
            Contract.Requires(child != null, $"{nameof(PropertyCopier<TParent, TChild>)}: child has to be not null");
            var parentProperties = parent.GetType().GetProperties();
            var childProperties = child.GetType().GetProperties();

            foreach (var parentProperty in parentProperties)
            {
                foreach (var childProperty in childProperties)
                {
                    if (parentProperty.Name == childProperty.Name && parentProperty.PropertyType == childProperty.PropertyType && childProperty.CanWrite)
                    {
                        childProperty.SetValue(child, parentProperty.GetValue(parent));
                        break;
                    }
                }
            }
        }
    }
}
