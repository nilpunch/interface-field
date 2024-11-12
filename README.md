# Interface Field for UnityEngine.Object

![image](https://github.com/user-attachments/assets/c591803c-8430-4ff3-a533-d92c3de01809)

A Unity package for serializing UnityEngine.Object using an interface field.  
This leverages decompiled UnityEditor code, so stability may vary.

Refer to the [samples](https://github.com/nilpunch/interface-field/tree/master/Samples) for a quick start.

## Installation

Make sure you have standalone [Git](https://git-scm.com/downloads) installed first. Reboot after installation.  
In Unity, open "Window" -> "Package Manager".  
Click the "+" sign at the top left corner -> "Add package from git URL..."  
Paste this: `https://github.com/nilpunch/interface-field.git`  
See minimum required Unity version in the `package.json` file.

## Overview

Currently supports only drag-and-drop. Object selection via a window is WIP. 

What is allowed:
- Renaming the interface field in your consumer
- Renaming implementations of your interface

What is **not** allowed:
- Renaming proxy classes
- Renaming the interface (this may work in the latest Unity versions)

### How to use

1. Write proxy class for the interface (can be defined anywhere):
```cs
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
```

2. Use `SerializeReference` with `Proxy` attribute for your interface consumer:
```cs
public class WeaponConsumer : MonoBehaviour
{
	[SerializeReference, Proxy(typeof(IWeapon.Proxy))]
	private IWeapon _weapon;

	[ContextMenu("Test")]
	public void Test()
	{
		_weapon.Attack();
	}
}
```

Interface implementors must simply implement the interface and thats it.
