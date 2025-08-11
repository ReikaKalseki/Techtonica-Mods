using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;

using BepInEx;

using ReikaKalseki.DIANEXCAL;

using HarmonyLib;

using UnityEngine;

using EquinoxsModUtils;
using EquinoxsModUtils.Additions;
using EquinoxsModUtils.Additions.ContentAdders;
using EquinoxsModUtils.Additions.Patches;

namespace ReikaKalseki.DIANEXCAL {
	
	public class CustomItem : NewResourceDetails {
		
		public ResourceInfo item { get; private set; }
		
		public int itemID { get { return item.uniqueId; } }
		
		public Assembly ownerMod { get; private set; }
		
		public CustomItem(string name, string desc, string template, string sprite) : base() {
			this.name = name;
			description = desc;
			parentName = template;
			craftingMethod = CraftingMethod.Assembler;
			this.sprite = TextureManager.createSprite(TextureManager.getTexture(TTUtil.tryGetModDLL(true), "Textures/" + sprite));
			maxStackCount = 500;
			sortPriority = 998;
			
			ownerMod = TTUtil.tryGetModDLL();
		}
		
		public void register() {
			try {
				EMUAdditions.AddNewResource(this, true);
				DIMod.onDefinesLoadedFirstTime += () => {
					onPatched();
				};
				TTUtil.log("Registered item "+this, ownerMod);
			}
			catch (Exception ex) {
				TTUtil.log("Failed to register "+this, ownerMod);
				throw ex;
			}
		}
		
		public void onPatched() {
			item = EMU.Resources.GetResourceInfoByName(name);
			if (item == null)
				throw new Exception("Item " + this + " failed to find its registered counterpart");
			else
				TTUtil.log("Item " + name + " injected: " + item.toDebugString(), ownerMod);
			item.rawConveyorResourcePrefab = EMU.Resources.GetResourceInfoByName(parentName).rawConveyorResourcePrefab;
		}
		
		public override sealed string ToString() {
			return "Item '"+name+"'";
		}
		
	}
}
