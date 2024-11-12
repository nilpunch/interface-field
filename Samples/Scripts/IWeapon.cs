namespace InterfaceField.Samples
{
	public interface IWeapon
	{
		void Attack();

		public class Proxy : ObjectProxy, IWeapon
		{
			public void Attack()
			{
				((IWeapon)Object).Attack();
			}
		}
	}
}
