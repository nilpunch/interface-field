using System;
using System.Linq;

namespace InterfaceField
{
	public static class Utils
	{
		public static string GetFullGenericName(this Type type)
		{
			if (type.IsGenericType)
			{
				var genericArguments = string.Join(',', type.GetGenericArguments().Select(GetFullGenericName));
				var typeItself = type.FullName[..type.FullName.IndexOf('`', StringComparison.Ordinal)];
				return $"{typeItself}<{genericArguments}>";
			}
			return type.FullName;
		}
	}
}
