using System;
using UnityEngine;

namespace Plugins.InterfaceObjectField.Runtime
{
	public class ProxyAttribute : PropertyAttribute
	{
		public Type ProxyType { get; }

		public ProxyAttribute(Type proxyType)
		{
			ProxyType = proxyType;
		}
	}
}
