﻿using System;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace ReikaKalseki.DIANEXCAL {

	public static class ObjectUtil {
		
		public static bool debugMode;
		
		public static void stripAllExcept(GameObject go, params Type[] except) {
			HashSet<Type> li = except.ToHashSet();
			foreach (Component c in go.GetComponentsInChildren<Component>()) {
				if (c is Transform || li.Contains(c.GetType()))
					continue;
				UnityEngine.Object.DestroyImmediate(c);
			}
		}
		
		public static void removeComponent(GameObject go, Type tt, bool immediate = true) {
			foreach (Component c in go.GetComponentsInChildren(tt)) {
				if (c is MonoBehaviour)
					((MonoBehaviour)c).enabled = false;
				if (immediate)
					UnityEngine.Object.DestroyImmediate(c);
				else
					UnityEngine.Object.Destroy(c);
			}
		}
		
		public static void removeComponent<C>(GameObject go, bool immediate = true) where C : Component {
			applyToComponents<C>(go, immediate ? 2 : 1, true, true);
		}
		
		public static void setActive<C>(GameObject go, bool active) where C : Component {
			applyToComponents<C>(go, 0, true, active);
		}
		
		private static void applyToComponents<C>(GameObject go, int destroy, bool setA, bool setTo) where C : Component {
			foreach (Component c in go.GetComponentsInChildren<C>(true)) {
				if (debugMode)
					TTUtil.log("Affecting component "+c+" in "+go+" @ "+go.transform.position+": D="+destroy+"/"+setTo+"("+setA+")", TTUtil.diDLL);
				if (c is MonoBehaviour && setA)
					((MonoBehaviour)c).enabled = setTo;
				if (destroy == 2)
					UnityEngine.Object.DestroyImmediate(c);
				else if (destroy == 1)
					UnityEngine.Object.Destroy(c);
			}
		}
		
		public static void dumpObjectData(GameObject go, bool includeChildren = true) {
			dumpObjectData(go, 0, includeChildren);
		}
		
		private static void dumpObjectData(GameObject go, int indent, bool includeChildren = true) {
			if (!go) {
				TTUtil.log("null object");
				return;
			}
			TTUtil.log("object "+go, TTUtil.diDLL, indent);
			TTUtil.log("components: "+string.Join(", ", (object[])go.GetComponents<Component>()), TTUtil.diDLL, indent);
			TTUtil.log("transform: "+go.transform, TTUtil.diDLL, indent);
			if (go.transform != null) {
				TTUtil.log("position: "+go.transform.position, TTUtil.diDLL, indent);
				TTUtil.log("transform object: "+go.transform.gameObject, TTUtil.diDLL, indent);
				for (int i = 0; i < go.transform.childCount; i++) {
					GameObject ch = go.transform.GetChild(i).gameObject;
					TTUtil.log("child object #"+i+": "+(includeChildren ? "" : ch.name), TTUtil.diDLL, indent);
					if (includeChildren)
						dumpObjectData(ch, indent+3);
				}
			}
		}
		
		public static void dumpObjectData(Component go) {
			dumpObjectData(go, 0);
		}
		
		private static void dumpObjectData(Component go, int indent) {
			if (!go) {
				TTUtil.log("null component");
				return;
			}
			TTUtil.log("component "+go, TTUtil.diDLL, indent);
			dumpObjectData(go.gameObject);
		}
		
		public static void dumpObjectData(Mesh m) {
			TTUtil.log("Mesh "+m+":");
			if (m == null) {
				TTUtil.log("Mesh is null");
				return;
			}
			TTUtil.log("Mesh has "+m.subMeshCount+" submeshes");
			TTUtil.log("Mesh has "+m.vertexCount+" vertices:");
			if (m.isReadable) {
				Vector3[] verts = m.vertices;
				for (int i = 0; i < verts.Length; i++) {
					TTUtil.log("Vertex "+i+": "+verts[i].ToString("F5"));
				}
			}
			else {
				TTUtil.log("[Not readable]");
			}
		}
		
		public static int removeChildObject(GameObject go, string name, bool immediate = true) {
			GameObject find = getChildObject(go, name);
			int found = 0;
			while (go && find) {
				find.SetActive(false);
				if (immediate)
					UnityEngine.Object.DestroyImmediate(find);
				else
					UnityEngine.Object.Destroy(find);
				find = getChildObject(go, name);
				found++;
				if (found > 500) {
					TTUtil.log("REMOVING CHILD OBJECT STUCK IN INFINITE LOOP INSIDE "+go.name+"!");
					return found;
				}
			}
			return found;
		}
		
		public static List<GameObject> getChildObjects(GameObject go) {
			List<GameObject> ret = new List<GameObject>();
			foreach (Transform t in go.transform) {
				ret.Add(t.gameObject);
			}
			return ret;
		}
		
		public static List<GameObject> getChildObjects(GameObject go, string name, bool recursive = false) {
			bool startWild = name[0] == '*';
			bool endWild = name[name.Length-1] == '*';
			string seek = name;
			if (startWild)
				seek = seek.Substring(1);
			if (endWild)
				seek = seek.Substring(0, seek.Length-1);
			//SNUtil.writeToChat(seek+" > "+startWild+"&"+endWild);
			List<GameObject> ret = new List<GameObject>();
			foreach (Transform t in go.transform) {
				string n = t.gameObject.name;
				n = n.Replace("(Placeholder)", "");
				n = n.Replace("(Clone)", "");
				bool match = false;
				if (startWild && endWild) {
					match = n.Contains(seek);
				}
				else if (startWild) {
					match = n.EndsWith(seek, StringComparison.InvariantCulture);
				}
				else if (endWild) {
					match = n.StartsWith(seek, StringComparison.InvariantCulture);
				}
				else {
					match = n == seek;
				}
				//SNUtil.writeToChat(seek+"&&"+n+" > "+match);
				if (match) {
					ret.Add(t.gameObject);
				}
				if (recursive && (startWild || endWild)) {
					ret.AddRange(getChildObjects(t.gameObject, name, true));
				}
			}
			return ret;
		}
		
		public static GameObject getChildObject(GameObject go, string name) {
			if (!go)
				return null;
			if (name == "*")
				return go.transform.childCount > 0 ? go.transform.GetChild(0).gameObject : null;
			bool startWild = name[0] == '*';
			bool endWild = name[name.Length-1] == '*';
			if (startWild || endWild) {
				if (debugMode)
					TTUtil.log("Looking for child wildcard match "+name+" > "+startWild+", "+endWild, TTUtil.diDLL);
				return findFirstChildMatching(go, name, startWild, endWild);
			}
			else {
			 	Transform t = go.transform.Find(name);
			 	if (t != null)
			 		return t.gameObject;
			 	t = go.transform.Find(name+"(Clone)");
			 	if (t != null)
			 		return t.gameObject;
			 	t = go.transform.Find(name+"(Placeholder)");
			 	return t != null ? t.gameObject : null;
			}
		}
		
		public static GameObject findFirstChildMatching(GameObject go, string s0, bool startWild, bool endWild) {
			string s = s0;
			if (startWild)
				s = s.Substring(1);
			if (endWild)
				s = s.Substring(0, s.Length-1);
			foreach (Transform t in go.transform) {
				string name = t.gameObject.name;
				bool match = false;
				if (startWild && endWild) {
					match = name.Contains(s);
				}
				else if (startWild) {
					match = name.EndsWith(s, StringComparison.InvariantCulture);
				}
				else if (endWild) {
					match = name.StartsWith(s, StringComparison.InvariantCulture);
				}
				if (match) {
					return t.gameObject;
				}
				else {
					if (debugMode)
						TTUtil.log("Found no match for "+s0+" against "+t.gameObject.name, TTUtil.diDLL);
					GameObject inner = findFirstChildMatching(t.gameObject, s0, startWild, endWild);
					if (inner)
						return inner;
				}
			}
			return null;
		}
		
		public static bool objectCollidesPosition(GameObject go, Vector3 pos) {
			if (go.transform != null) {
				Collider c = go.GetComponentInParent<Collider>();
				if (c != null && c.enabled) {
					return (c.ClosestPoint(pos) - pos).sqrMagnitude < Mathf.Epsilon * Mathf.Epsilon;
				}
				Renderer r = go.GetComponentInChildren<Renderer>();
				if (r != null && r.enabled) {
					return r.bounds.Contains(pos);
				}
			}
			return false;
		}
		
		public static void offsetColliders(GameObject go, Vector3 move) {
			foreach (Collider c in go.GetComponentsInChildren<Collider>()) {
				if (c is SphereCollider) {
					((SphereCollider)c).center = ((SphereCollider)c).center+move;
				}
				else if (c is BoxCollider) {
					((BoxCollider)c).center = ((BoxCollider)c).center+move;
				}
				else if (c is CapsuleCollider) {
					((CapsuleCollider)c).center = ((CapsuleCollider)c).center+move;
				}
				else if (c is MeshCollider) {
					//TODO move to subobject
				}
			}
		}
		
		public static void visualizeColliders(GameObject go) {
			foreach (Collider c in go.GetComponentsInChildren<Collider>()) {
				Vector3 sc = Vector3.one;
				Vector3 off = Vector3.zero;
				PrimitiveType? pm = null;
				if (c is SphereCollider) {
					pm = PrimitiveType.Sphere;
					SphereCollider sp = (SphereCollider)c;
					sc = Vector3.one*sp.radius;
					off = sp.center;
				}
				else if (c is BoxCollider) {
					pm = PrimitiveType.Cube;
					BoxCollider b = (BoxCollider)c;
					sc = b.size/2;
					off = b.center;
				}
				else if (c is CapsuleCollider) {
					pm = PrimitiveType.Capsule;
					CapsuleCollider cc = (CapsuleCollider)c;
					sc = new Vector3(cc.radius, cc.height, cc.radius);
					off = cc.center;
				}
				if (pm != null && pm.HasValue) {
					GameObject vis = GameObject.CreatePrimitive(pm.Value);
					vis.transform.position = off;
					vis.transform.parent = c.transform;
					vis.transform.localScale = sc;
					vis.SetActive(true);
				}
			}
		}
		
		public static Light addLight(GameObject go) {
			GameObject child = new GameObject();
			child.transform.parent = go.transform;
			child.transform.localPosition = Vector3.zero;
			child.name = "Light Entity";
			return child.AddComponent<Light>();
		}
		
		public static T copyComponent<T>(GameObject from, GameObject to) where T : Component {
			T tgt = to.ensureComponent<T>();
			tgt.copyObject(from.GetComponent<T>());
			return tgt;
		}
		
		public static void ignoreCollisions(GameObject from, params GameObject[] with) {
			foreach (GameObject go in with) {
				foreach (Collider c in go.GetComponentsInChildren<Collider>(true)) {
					foreach (Collider c0 in from.GetComponentsInChildren<Collider>(true)) {
						Physics.IgnoreCollision(c0, c);
					}
				}
			}
		}
		
		public static void fullyEnable(GameObject go) {
			go.SetActive(true);
			foreach (Behaviour mb in go.GetComponentsInChildren<Behaviour>(true)) {
				mb.enabled = true;
			}
			foreach (Transform t in go.transform) {
				if (t)
					fullyEnable(t.gameObject);
			}
		}
		
		public static bool isLookingAt(Transform looker, Vector3 pos, float maxAng) {
			return Vector3.Angle(looker.forward, pos-looker.transform.position) <= maxAng;
		}
		
		public static void reparentTo(GameObject go, GameObject child) {
			Vector3 pos = child.transform.position;
			Quaternion rot = child.transform.rotation;
			child.transform.SetParent(go.transform);
			child.transform.position = pos;
			child.transform.rotation = rot;
		}
		
		public static void cleanUpOriginObjects(Component c) {
			if (c.transform.position.sqrMagnitude < 0.01)
				UnityEngine.Object.Destroy(c.gameObject);
		}
		
	}
}
