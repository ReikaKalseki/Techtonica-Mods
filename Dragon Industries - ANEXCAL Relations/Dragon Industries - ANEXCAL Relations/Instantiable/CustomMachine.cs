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
	
	public class CustomMachine<T, V> : Unlockable where T : struct, IMachineInstance<T, V> where V : MachineDefinition<T, V> {
		
		private readonly V definition;
		
		private CustomTech tech;
		
		private CustomRecipe recipe;
        
		public string name { get { return item.name; } }
        
		public CustomItem item { get; private set; }
		
		public Assembly ownerMod { get; private set; }
		
		public bool isUnlocked { get { return tech.isUnlocked; } }
		
		public Unlock unlock { get { return TTUtil.getUnlock(name); } }
		
		public CustomMachine(string name, string desc, string sprite, string unlock, string template) {
			item = new CustomItem(name, desc, template, sprite);
			item.craftTierRequired = 0;
			item.headerTitle = "Logistics"; //TODO
			item.subHeaderTitle = "Utility";
			item.maxStackCount = 50;
			item.sortPriority = 998;
			item.unlockName = unlock;
			
			definition = ScriptableObject.CreateInstance<V>();
			
			ownerMod = TTUtil.tryGetModDLL();
		}
		
		public CustomTech createUnlock(Unlock.TechCategory cat, ResearchCoreDefinition.CoreType type, int cores) {
			tech = new CustomTech(cat, name, item.description, type, cores);
			tech.sprite = item.sprite;
			return tech;
		}
		
		public CustomRecipe addRecipe(int amt = 1) {
			recipe = new CustomRecipe(name);
			recipe.duration = 1F;
			recipe.unlockName = EMU.Names.Unlocks.BasicLogistics;
			recipe.outputs = new List<RecipeResourceInfo>() {
				new RecipeResourceInfo() {
					name = name,
					quantity = amt
				}
			};
			return recipe;
		}
		
		public void register() {
			try {
				EMUAdditions.AddNewMachine(definition, item, true);
				
				if (recipe != null)
					recipe.register();
				else
					throw new Exception("Machine '"+this+"' has no recipe!");
				
				if (tech != null) {
					tech.setRecipes(recipe);
					tech.register();
				}
				else
					throw new Exception("Machine '"+this+"' has no tech!");
				
				EMU.Events.GameDefinesLoaded += () => {					
					item.onPatched(); //it will not be called since register() is not called since the item is passed in to AddNewMachine which does its own addItem
					item.item.rawSprite = item.sprite;
					item.item.unlock = unlock;
				};
				
				TTUtil.log("Registered machine " + this, ownerMod);
			}
			catch (Exception ex) {
				TTUtil.log("Failed to register "+this, ownerMod);
				throw ex;
			}
		}
		
		public override sealed string ToString() {
			return string.Format("CustomMachine<{0}, {1}> {2}", typeof(T).Name, typeof(V).Name, item.name);
		}
		
		public bool isThisMachine(IMachineInstance<T, V> inst) {
			return isThisMachine(inst.myDef);
		}
		
		public bool isThisMachine(MachineDefinition<T, V> def) {
			return def.displayName == name;
		}
		
	}
}
