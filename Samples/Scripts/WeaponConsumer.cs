using System;
using InterfaceField;
using InterfaceField.Samples;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InterfaceField.Samples
{
	public class WeaponConsumer : MonoBehaviour
	{
		[SerializeReference, Proxy(typeof(IWeapon.Proxy))]
		private IWeapon _weapon;

		private void Update()
		{
			_weapon.Attack();
		}

		[ContextMenu("Test")]
		public void Test()
		{
			_weapon.Attack();
		}
	}
}