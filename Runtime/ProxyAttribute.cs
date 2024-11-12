using System;
using UnityEngine;

namespace InterfaceField
{
	public class ProxyAttribute : PropertyAttribute
	{
		public Type ProxyType { get; }

		public ProxyAttribute(Type proxyType)
		{
			if (proxyType == null)
			{
				throw new Exception("Provided proxy type is null!");
			}

			if (!typeof(ObjectProxy).IsAssignableFrom(proxyType))
			{
				throw new Exception($"Provided proxy type {proxyType.GetFullGenericName()} must inherit {typeof(ObjectProxy).Name}!");
			}

			ProxyType = proxyType;
		}
	}
}
