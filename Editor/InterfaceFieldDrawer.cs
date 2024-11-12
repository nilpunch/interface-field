using System;
using System.Collections.Generic;
using System.Reflection;
using Plugins.InterfaceObjectField.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using Object = UnityEngine.Object;

[CustomPropertyDrawer(typeof(ProxyAttribute))]
public class InterfaceFieldDrawer : PropertyDrawer
{
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		var interfaceType = fieldInfo.FieldType;

		if (property.boxedValue == null)
		{
			var proxy = fieldInfo.GetCustomAttribute<ProxyAttribute>().ProxyType;
			property.boxedValue = Activator.CreateInstance(proxy);
			property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
		}

		var objectField = property.FindPropertyRelative("Object");

		label = EditorGUI.BeginProperty(position, label, objectField);
		ObjectFieldInternal(position, objectField, interfaceType, label, EditorStyles.objectField, null);
		EditorGUI.EndProperty();
	}

	private static void ObjectFieldInternal(
		Rect position,
		SerializedProperty property,
		Type objType,
		GUIContent label,
		GUIStyle style,
		ObjectFieldValidator validator = null)
	{
		int controlId = GUIUtility.GetControlID(s_PPtrHash, FocusType.Keyboard, position);
		position = EditorGUI.PrefixLabel(position, controlId, label);
		bool allowSceneObjects = false;
		if (property != null)
		{
			Object targetObject = property.serializedObject.targetObject;
			if (targetObject != null && !EditorUtility.IsPersistent(targetObject))
				allowSceneObjects = true;
		}
		DoObjectField(position, position, controlId, null, null, objType, null, property, validator, allowSceneObjects, style, objectFieldButton);
	}

	private static Object DoObjectField(
		Rect position,
		Rect dropRect,
		int id,
		Object obj,
		Object objBeingEdited,
		Type objType,
		Type additionalType,
		SerializedProperty property,
		ObjectFieldValidator validator,
		bool allowSceneObjects,
		GUIStyle style,
		GUIStyle buttonStyle,
		Action<Object> onObjectSelectorClosed = null,
		Action<Object> onObjectSelectedUpdated = null)
	{
		if (validator == null)
			validator = new ObjectFieldValidator(ValidateObjectFieldAssignment);
		if (property != null)
			obj = property.objectReferenceValue;
		Event current = Event.current;
		EventType eventType = current.type;
		if (!GUI.enabled && GUIClip_enabled && Event.current.rawType == EventType.MouseDown)
			eventType = Event.current.rawType;
		bool flag = EditorGUIUtility.HasObjectThumbnail(objType);
		ObjectFieldVisualType visualType = ObjectFieldVisualType.IconAndText;
		if (flag && position.height <= 18.0 && position.width <= 32.0)
			visualType = ObjectFieldVisualType.MiniPreview;
		else if (flag && position.height > 18.0)
			visualType = ObjectFieldVisualType.LargePreview;
		Vector2 iconSize = EditorGUIUtility.GetIconSize();
		switch (visualType)
		{
			case ObjectFieldVisualType.IconAndText:
				EditorGUIUtility.SetIconSize(new Vector2(12f, 12f));
				break;
			case ObjectFieldVisualType.LargePreview:
				EditorGUIUtility.SetIconSize(new Vector2(64f, 64f));
				break;
		}
		switch (eventType)
		{
			case EventType.MouseDown:
				if (position.Contains(Event.current.mousePosition) && Event.current.button == 0)
				{
					Rect buttonRect = GetButtonRect(visualType, position);
					EditorGUIUtility.editingTextField = false;
					if (buttonRect.Contains(Event.current.mousePosition))
					{
						if (GUI.enabled)
						{
							GUIUtility.keyboardControl = id;
							ObjectSelectorShow(property.objectReferenceValue, objType, property.serializedObject.targetObject, allowSceneObjects, onObjectSelectorClosed: onObjectSelectorClosed,
								onObjectSelectedUpdated: onObjectSelectedUpdated);
							ObjectSelectorSetSelectorId(id);
							current.Use();
							GUIUtility.ExitGUI();
						}
					}
					else
					{
						Object @object = property != null ? property.objectReferenceValue : obj;
						Component component = @object as Component;
						if ((bool)(Object)component)
							@object = component.gameObject;
						if (EditorGUI.showMixedValue)
							@object = null;
						if (Event.current.clickCount == 1)
						{
							GUIUtility.keyboardControl = id;
							PingObjectOrShowPreviewOnClick(@object, position);
							Material targetMaterial = @object as Material;
							if (targetMaterial != null)
								PingObjectInSceneViewOnClick(targetMaterial);
							current.Use();
						}
						else if (Event.current.clickCount == 2 && (bool)@object)
						{
							AssetDatabase.OpenAsset(@object);
							current.Use();
							GUIUtility.ExitGUI();
						}
					}
					break;
				}
				break;
			case EventType.Repaint:
				GUIContent content = !EditorGUI.showMixedValue ? ObjectContent(obj, objType, property, validator) : s_MixedValueContent;
				switch (visualType)
				{
					case ObjectFieldVisualType.IconAndText:
						BeginHandleMixedValueContentColor();
						style.Draw(position, content, id, DragAndDrop.activeControlID == id, position.Contains(Event.current.mousePosition));
						Rect position1 = buttonStyle.margin.Remove(GetButtonRect(visualType, position));
						buttonStyle.Draw(position1, GUIContent.none, id, DragAndDrop.activeControlID == id, position1.Contains(Event.current.mousePosition));
						EndHandleMixedValueContentColor();
						break;
					case ObjectFieldVisualType.LargePreview:
						DrawObjectFieldLargeThumb(position, id, obj, content);
						break;
					case ObjectFieldVisualType.MiniPreview:
						DrawObjectFieldMiniThumb(position, id, obj, content);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
				break;
			case EventType.DragUpdated:
			case EventType.DragPerform:
				string errorString;
				if (eventType == EventType.DragPerform && !ValidDroppedObject(DragAndDrop.objectReferences, objType, out errorString))
				{
					Object objectReference = DragAndDrop.objectReferences[0];
					EditorUtility.DisplayDialog("Can't assign script", errorString, "OK");
					break;
				}
				if (dropRect.Contains(Event.current.mousePosition) && GUI.enabled)
				{
					Object[] objectReferences = DragAndDrop.objectReferences;
					Object target = validator(objectReferences, objType, property, ObjectFieldValidatorOptions.None);
					if (target != null && !allowSceneObjects && !EditorUtility.IsPersistent(target))
						target = null;
					if (target != null)
					{
						if (DragAndDrop.visualMode == DragAndDropVisualMode.None)
							DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
						if (eventType == EventType.DragPerform)
						{
							if (property != null)
								property.objectReferenceValue = target;
							else
								obj = target;
							GUI.changed = true;
							DragAndDrop.AcceptDrag();
							DragAndDrop.activeControlID = 0;
						}
						else
							DragAndDrop.activeControlID = id;
						Event.current.Use();
					}
					break;
				}
				break;
			case EventType.ValidateCommand:
				if ((current.commandName == "Delete" || current.commandName == "SoftDelete") && GUIUtility.keyboardControl == id)
				{
					current.Use();
					break;
				}
				break;
			case EventType.ExecuteCommand:
				string commandName = current.commandName;
				if (commandName == "ObjectSelectorUpdated" && ObjectSelectorGetSelectorId() == id && GUIUtility.keyboardControl == id && (property == null || !IsScript(property)))
					return AssignSelectedObject(property, validator, objType, current);
				if (commandName == "ObjectSelectorClosed" && ObjectSelectorGetSelectorId() == id && GUIUtility.keyboardControl == id && property != null && IsScript(property))
				{
					if (((Object)ObjectSelector_Get()).GetInstanceID() != 0)
						return AssignSelectedObject(property, validator, objType, current);
					current.Use();
					break;
				}
				if ((current.commandName == "Delete" || current.commandName == "SoftDelete") && GUIUtility.keyboardControl == id)
				{
					if (property != null)
						property.objectReferenceValue = null;
					else
						obj = null;
					GUI.changed = true;
					current.Use();
					break;
				}
				break;
			case EventType.DragExited:
				if (GUI.enabled)
				{
					HandleUtility.Repaint();
					break;
				}
				break;
		}
		EditorGUIUtility.SetIconSize(iconSize);
		return obj;
	}

	private static readonly GUIStyle objectFieldButton =
		(GUIStyle)typeof(EditorStyles).GetProperty("objectFieldButton", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);

	private static readonly int s_PPtrHash = nameof(s_PPtrHash).GetHashCode();

	internal delegate Object ObjectFieldValidator(
		Object[] references,
		Type objType,
		SerializedProperty property,
		ObjectFieldValidatorOptions options);

	[Flags]
	internal enum ObjectFieldValidatorOptions
	{
		None = 0,
		ExactObjectTypeValidation = 1,
	}

	internal static Object ValidateObjectFieldAssignment(
		Object[] references,
		Type objType,
		SerializedProperty property,
		ObjectFieldValidatorOptions options)
	{
		if (references.Length != 0)
		{
			bool flag1 = DragAndDrop.objectReferences.Length != 0;
			bool flag2 = references[0] != null && references[0] is Texture2D;
			if (objType == typeof(Sprite) & flag2 & flag1)
				return null;
			if (property != null)
			{
				if (references[0] != null && ValidateObjectReferenceValue(property, references[0], options))
				{
					if (EditorSceneManager.preventCrossSceneReferences && CheckForCrossSceneReferencing(references[0], property.serializedObject.targetObject))
						return null;
					if (!(objType != null))
						return references[0];
					if (references[0] is GameObject)
						references = ((GameObject)references[0]).GetComponents(typeof(Component));
					foreach (Object reference in references)
					{
						if (reference != null && objType.IsAssignableFrom(reference.GetType()))
							return reference;
					}
				}
				string str = property.type;
				if (property.type == "vector")
					str = property.arrayElementType;
				if (((str == "PPtr<Sprite>" ? 1 : (str == "PPtr<$Sprite>" ? 1 : 0)) & (flag2 ? 1 : 0) & (flag1 ? 1 : 0)) != 0)
					return null;
			}
			else
			{
				if (references[0] != null && references[0] is GameObject && typeof(Component).IsAssignableFrom(objType))
					references = ((GameObject)references[0]).GetComponents(typeof(Component));
				foreach (Object reference in references)
				{
					if (reference != null && objType.IsAssignableFrom(reference.GetType()))
						return reference;
				}
			}
		}
		return null;
	}

	private static bool ValidateObjectReferenceValue(
		SerializedProperty property,
		Object obj,
		ObjectFieldValidatorOptions options)
	{
		return (options & ObjectFieldValidatorOptions.ExactObjectTypeValidation) == ObjectFieldValidatorOptions.ExactObjectTypeValidation
			? ValidateObjectReferenceValueExactForProperty(property, obj)
			: ValidateObjectReferenceValueForProperty(property, obj);
	}

	internal static Func<Object, Object, bool> CheckForCrossSceneReferencing = (Func<Object, Object, bool>)Delegate.CreateDelegate(typeof(Func<Object, Object, bool>),
		typeof(EditorGUI).GetMethod("CheckForCrossSceneReferencing", BindingFlags.Static | BindingFlags.NonPublic)!);

	internal static readonly Func<SerializedProperty, Object, bool> ValidateObjectReferenceValueForProperty = (Func<SerializedProperty, Object, bool>)Delegate.CreateDelegate(
		typeof(Func<SerializedProperty, Object, bool>),
		typeof(SerializedProperty).GetMethod("ValidateObjectReferenceValue", BindingFlags.Instance | BindingFlags.NonPublic)!);

	internal static readonly Func<SerializedProperty, Object, bool> ValidateObjectReferenceValueExactForProperty = (Func<SerializedProperty, Object, bool>)Delegate.CreateDelegate(
		typeof(Func<SerializedProperty, Object, bool>),
		typeof(SerializedProperty).GetMethod("ValidateObjectReferenceValueExact", BindingFlags.Instance | BindingFlags.NonPublic)!);

	internal static bool GUIClip_enabled = true;

	internal enum ObjectFieldVisualType
	{
		IconAndText,
		LargePreview,
		MiniPreview,
	}

	private static Rect GetButtonRect(ObjectFieldVisualType visualType, Rect position)
	{
		switch (visualType)
		{
			case ObjectFieldVisualType.IconAndText:
				return new Rect(position.xMax - 19f, position.y, 19f, position.height);
			case ObjectFieldVisualType.LargePreview:
				return new Rect(position.xMax - 36f, position.yMax - 14f, 36f, 14f);
			case ObjectFieldVisualType.MiniPreview:
				return new Rect(position.xMax - 14f, position.y, 14f, position.height);
			default:
				throw new ArgumentOutOfRangeException();
		}
	}

	private static Type GetInternalType(string typeName, string assemblyName = "UnityEditor.CoreModule")
	{
		string internalClassName = typeName;

		Assembly assembly = Assembly.Load(assemblyName);

		return assembly.GetType(internalClassName);
	}

	private static readonly Type ObjectSelector_Type = GetInternalType("UnityEditor.ObjectSelector");

	private static readonly Func<object> ObjectSelector_Get =
		(Func<object>)Delegate.CreateDelegate(typeof(Func<object>), ObjectSelector_Type.GetProperty("get", BindingFlags.Static | BindingFlags.Public).GetMethod!);

	private static readonly MethodInfo ObjectSelector_Show_Method =
		ObjectSelector_Type.GetMethod("Show", BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[]
		{
			typeof(Object),
			typeof(Type),
			typeof(Object),
			typeof(bool),
			typeof(List<int>),
			typeof(Action<Object>),
			typeof(Action<Object>),
			typeof(bool)
		}, null)!;

	private static void ObjectSelectorShow(
		UnityEngine.Object obj,
		System.Type requiredType,
		UnityEngine.Object objectBeingEdited,
		bool allowSceneObjects,
		List<int> allowedInstanceIDs = null,
		Action<UnityEngine.Object> onObjectSelectorClosed = null,
		Action<UnityEngine.Object> onObjectSelectedUpdated = null,
		bool showNoneItem = true)
	{
		ObjectSelector_Show_Method.Invoke(ObjectSelector_Get(), new object[]
		{
			obj,
			requiredType,
			objectBeingEdited,
			allowSceneObjects,
			allowedInstanceIDs,
			onObjectSelectorClosed,
			onObjectSelectedUpdated,
			showNoneItem
		});
	}

	private static readonly FieldInfo ObjectSelector_selectorId = ObjectSelector_Type.GetField("objectSelectorID", BindingFlags.Instance | BindingFlags.NonPublic);
	private static readonly Action<object, int> ObjectSelector_selectorId_Set = (selector, id) => { ObjectSelector_selectorId.SetValue(selector, id); };
	private static readonly Func<object, int> ObjectSelector_selectorId_Get = (selector) => (int)ObjectSelector_selectorId.GetValue(selector);

	private static void ObjectSelectorSetSelectorId(int id)
	{
		ObjectSelector_selectorId_Set.Invoke(ObjectSelector_Get.Invoke(), id);
	}

	private static int ObjectSelectorGetSelectorId()
	{
		return ObjectSelector_selectorId_Get.Invoke(ObjectSelector_Get.Invoke());
	}

	internal static Action<Object, Rect> PingObjectOrShowPreviewOnClick = (Action<Object, Rect>)Delegate.CreateDelegate(typeof(Action<Object, Rect>),
		typeof(EditorGUI).GetMethod("PingObjectOrShowPreviewOnClick", BindingFlags.Static | BindingFlags.NonPublic)!);

	internal static Action<Material> PingObjectInSceneViewOnClick =
		(Action<Material>)Delegate.CreateDelegate(typeof(Action<Material>), typeof(EditorGUI).GetMethod("PingObjectInSceneViewOnClick", BindingFlags.Static | BindingFlags.NonPublic)!);

	internal static Func<string, GUIContent> TempContent = (Func<string, GUIContent>)Delegate.CreateDelegate(typeof(Func<string, GUIContent>), typeof(EditorGUIUtility).GetMethod("TempContent",
		BindingFlags.Static | BindingFlags.NonPublic, null, new Type[]
		{
			typeof(string)
		}, null)!);

	internal static GUIContent ObjectContent(
		Object obj,
		Type type,
		SerializedProperty property,
		ObjectFieldValidator validator = null)
	{
		if (validator == null)
			validator = new ObjectFieldValidator(ValidateObjectFieldAssignment);
		GUIContent guiContent = (obj != null)
			? ObjectContentv2(obj, type, property.objectReferenceInstanceIDValue)
			: TempContent($"None ({type.Name})");
		if (property != null && obj != null)
		{
			Object[] references = new Object[1]
			{
				obj
			};
			if (EditorSceneManager.preventCrossSceneReferences && CheckForCrossSceneReferencing(obj, property.serializedObject.targetObject))
			{
				if (!EditorApplication.isPlaying)
					guiContent = s_SceneMismatch;
				else
					guiContent.text += string.Format(" ({0})", GetGameObjectFromObject(obj).scene.name);
			}
			else if (validator(references, type, property, ObjectFieldValidatorOptions.ExactObjectTypeValidation) == null)
				guiContent = s_TypeMismatch;
		}
		return guiContent;
	}

	private static GUIContent s_TypeMismatch = EditorGUIUtility.TrTextContent("Type mismatch");
	private static GUIContent s_SceneMismatch = EditorGUIUtility.TrTextContent("Scene mismatch (cross scene references not supported)");
	private static readonly GUIContent s_MixedValueContent = EditorGUIUtility.TrTextContent("—", "Mixed Values");

	internal static GameObject GetGameObjectFromObject(Object obj)
	{
		GameObject objectFromObject = obj as GameObject;
		if (objectFromObject == null && obj is Component)
			objectFromObject = ((Component)obj).gameObject;
		return objectFromObject;
	}

	internal static Action BeginHandleMixedValueContentColor =
		(Action)Delegate.CreateDelegate(typeof(Action), typeof(EditorGUI).GetMethod("BeginHandleMixedValueContentColor", BindingFlags.Static | BindingFlags.NonPublic)!);

	internal static Action EndHandleMixedValueContentColor =
		(Action)Delegate.CreateDelegate(typeof(Action), typeof(EditorGUI).GetMethod("BeginHandleMixedValueContentColor", BindingFlags.Static | BindingFlags.NonPublic)!);

	internal static Action<Rect, int, Object, GUIContent> DrawObjectFieldLargeThumb = (Action<Rect, int, Object, GUIContent>)Delegate.CreateDelegate(typeof(Action<Rect, int, Object, GUIContent>),
		typeof(EditorGUI).GetMethod("DrawObjectFieldLargeThumb", BindingFlags.Static | BindingFlags.NonPublic)!);

	internal static Action<Rect, int, Object, GUIContent> DrawObjectFieldMiniThumb = (Action<Rect, int, Object, GUIContent>)Delegate.CreateDelegate(typeof(Action<Rect, int, Object, GUIContent>),
		typeof(EditorGUI).GetMethod("DrawObjectFieldLargeThumb", BindingFlags.Static | BindingFlags.NonPublic)!);

	private static bool ValidDroppedObject(
		Object[] references,
		Type objType,
		out string errorString)
	{
		errorString = "";
		if (references == null || references.Length == 0)
			return true;
		Object reference = references[0];
		Object @object = EditorUtility.InstanceIDToObject(reference.GetInstanceID());
		if (!(@object is MonoBehaviour) && !(@object is ScriptableObject) || HasValidScript(@object))
			return true;
		errorString = string.Format("Type cannot be found: {0}. Containing file and class name must match.", reference.GetType());
		return false;
	}

	private static bool HasValidScript(Object obj)
	{
		MonoScript monoScript = FromScriptedObject(obj);
		return !(monoScript == null) && !(monoScript.GetClass() == null);
	}

	internal static Func<Object, MonoScript> FromScriptedObject =
		(Func<Object, MonoScript>)Delegate.CreateDelegate(typeof(Func<Object, MonoScript>), typeof(MonoScript).GetMethod("FromScriptedObject", BindingFlags.Static | BindingFlags.NonPublic)!);

	internal static Func<SerializedProperty, string> ObjectReferenceStringValue = (Func<SerializedProperty, string>)Delegate.CreateDelegate(typeof(Func<SerializedProperty, string>),
		typeof(SerializedProperty).GetProperty("objectReferenceStringValue", BindingFlags.Instance | BindingFlags.NonPublic).GetMethod!);

	private static UnityEngine.Object AssignSelectedObject(
		SerializedProperty property,
		ObjectFieldValidator validator,
		System.Type objectType,
		UnityEngine.Event evt)
	{
		UnityEngine.Object[] references = new UnityEngine.Object[1]
		{
			ObjectSelectorGetCurrentObject()
		};
		UnityEngine.Object @object = validator(references, objectType, property, ObjectFieldValidatorOptions.None);
		if (property != null)
			property.objectReferenceValue = @object;
		GUI.changed = true;
		evt.Use();
		return @object;
	}

	internal static Action<SerializedProperty, object> SetManagedReferenceValueInternal =
		(Action<SerializedProperty, object>)Delegate.CreateDelegate(typeof(Action<SerializedProperty, object>),
			typeof(SerializedProperty).GetMethod("SetManagedReferenceValueInternal", BindingFlags.Instance | BindingFlags.NonPublic)!);

	internal static Func<Object> ObjectSelectorGetCurrentObject =
		(Func<Object>)Delegate.CreateDelegate(typeof(Func<Object>), ObjectSelector_Type.GetMethod("GetCurrentObject", BindingFlags.Static | BindingFlags.Public)!);

	internal static Func<Object, Type, int, GUIContent> ObjectContentv2 = (Func<Object, Type, int, GUIContent>)Delegate.CreateDelegate(typeof(Func<Object, Type, int, GUIContent>),
		typeof(EditorGUIUtility).GetMethod("ObjectContent", BindingFlags.Static | BindingFlags.NonPublic, null, new Type[]
		{
			typeof(Object), typeof(Type), typeof(int)
		}, null)!);

	public static bool IsScript(SerializedProperty property)
	{
		return property.type == "PPtr<MonoScript>";
	}
}
