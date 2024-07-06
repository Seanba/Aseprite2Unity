using System;
using System.Collections.Generic;
using System.Linq;

namespace Aseprite2Unity.Editor
{
    public static class EnumExtensions
    {
        public static T GetAttribute<T>(this Enum value) where T : Attribute
        {
            var type = value.GetType();
            var memberInfo = type.GetMember(value.ToString());
            var attributes = memberInfo[0].GetCustomAttributes(typeof(T), false);
            return (attributes.Length > 0) ? (T)attributes[0] : null;
        }

        // Filters out obsolete enum values
        public static string[] GetUpToDateEnumNames<T>()
        {
            List<string> niceNames = new List<string>();

            foreach (Enum value in Enum.GetValues(typeof(T)))
            {
                if (value.GetAttribute<ObsoleteAttribute>() == null)
                {
                    niceNames.Add(value.ToString());
                }
            }

            return niceNames.Distinct().ToArray();
        }
    }
}
