using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace HK.Modding.Analyzer
{
    static class Utils
    {
        public static LocalizableResourceString GetResourceString(string name)
        {
            return new(name, Resources.ResourceManager, typeof(Resources));
        }
        public static string GetNamespace(this ITypeSymbol type)
        {
            var sb = new StringBuilder();
            var np = type.ContainingNamespace;
            sb.Append(np.Name);
            while (np.ContainingNamespace != null)
            {
                np = np.ContainingNamespace;
                if (string.IsNullOrEmpty(np.Name)) break;
                sb.Insert(0, '.');
                sb.Insert(0, np.Name);
            }
            return sb.ToString();
        }
        public static string GetFullName(this ITypeSymbol type)
        {
            var np = type.ContainingType is not null ? type.ContainingType.GetFullName() : type.GetNamespace();
            if (string.IsNullOrEmpty(np)) return type.MetadataName;
            return np + "." + type.MetadataName;
        }
    }
}
