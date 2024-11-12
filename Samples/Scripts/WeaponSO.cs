using UnityEngine;

namespace InterfaceField.Samples
{
	public class WeaponSO : ScriptableObject, IWeapon
	{
		public void Attack()
		{
			Debug.Log("I Am SO");
		}
	}
}