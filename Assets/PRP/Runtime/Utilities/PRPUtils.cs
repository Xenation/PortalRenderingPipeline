using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PRP {
	public static class PRPUtils {

		/// <summary>
		/// Instantiate a game object to be used as a dummy render only element
		/// </summary>
		/// <param name="toInst">The game object to copy</param>
		/// <param name="parent">The parent of the instantiated game object</param>
		/// <param name="componentsToCopy">The component types to copy from the original game object</param>
		/// <returns>The instantiated game object</returns>
		public static GameObject InstantiateDummy(GameObject toInst, Transform parent, params Type[] componentsToCopy) {
			MeshFilter oriFilter = toInst.GetComponent<MeshFilter>();
			MeshRenderer oriRenderer = toInst.GetComponent<MeshRenderer>();

			GameObject instantiated = new GameObject(toInst.name);
			instantiated.transform.position = toInst.transform.position;
			instantiated.transform.rotation = toInst.transform.rotation;
			instantiated.transform.localScale = toInst.transform.lossyScale;
			instantiated.transform.SetParent(parent, true);

			MeshFilter filter = instantiated.AddComponent<MeshFilter>();
			filter.sharedMesh = oriFilter.sharedMesh;
			MeshRenderer renderer = instantiated.AddComponent<MeshRenderer>();
			renderer.materials = oriRenderer.materials;

			instantiated.CopyIncludedComponentsFrom(toInst, componentsToCopy);

			return instantiated;
		}

		public static GameObject InstantiateNoChildren(GameObject toInst, Transform parent, params Type[] excludedComponents) {
			GameObject instantiated = new GameObject(toInst.name);
			instantiated.CopyComponentsFrom(toInst, excludedComponents);
			instantiated.transform.SetParent(parent, true);
			return instantiated;
		}

		public static void CopyComponentsFrom(this GameObject go, GameObject copyFrom, params Type[] exclude) {
			List<Type> excluded = new List<Type>(exclude);
			List<Component> toCopy = new List<Component>();
			copyFrom.GetComponents(toCopy);
			foreach (Component component in toCopy) {
				if (excluded.Contains(component.GetType())) continue;
				Component instComp = go.GetComponent(component.GetType());
				if (instComp == null) {
					instComp = go.AddComponent(component.GetType());
				}
				instComp.CopyFrom(component);
			}
		}

		public static void CopyIncludedComponentsFrom(this GameObject go, GameObject copyFrom, params Type[] included) {
			foreach (Type copyType in included) {
				Component toCopy = copyFrom.GetComponent(copyType);
				if (toCopy == null) continue;
				go.AddComponent(copyType).CopyFrom(toCopy);
			}
		}
		
		public static void CopyFrom(this Component comp, Component other) {
			Type type = comp.GetType();
			if (type != other.GetType()) return; // type mis-match
			BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
			PropertyInfo[] pinfos = type.GetProperties(flags);
			foreach (var pinfo in pinfos) {
				if (pinfo.CanWrite && pinfo.GetCustomAttribute<ObsoleteAttribute>() == null) {
					try {
						pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
					} catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
				}
			}
			FieldInfo[] finfos = type.GetFields(flags);
			foreach (var finfo in finfos) {
				finfo.SetValue(comp, finfo.GetValue(other));
			}
		}

	}
}
