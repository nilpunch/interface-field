using UnityEngine;

namespace InterfaceField.Samples
{
	public class WeaponB : MonoBehaviour, IWeapon
	{
		public void Attack()
		{
			Debug.Log("BBBBBBBBB");
		}
	}
}