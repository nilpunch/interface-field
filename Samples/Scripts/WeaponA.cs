using UnityEngine;

namespace InterfaceField.Samples
{
	public class WeaponA : MonoBehaviour, IWeapon
	{
		public void Attack()
		{
			Debug.Log("AAAAAAAAAA");
		}
	}
}