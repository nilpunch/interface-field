using UnityEngine;
using Object = UnityEngine.Object;

namespace InterfaceField
{
	public class ObjectProxy
	{
		[SerializeField] private Object _object;

		public Object Object => _object;
	}
}
