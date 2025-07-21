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
		
		private readonly NewResourceDetails resource;
		private readonly V definition;
		
		private CustomTech tech;
		
		private NewRecipeDetails recipe;
        
		public string name { get { return resource.name; } }
        
		public string description { get { return resource.description; } }
        
		public Sprite sprite { get { return resource.sprite; } }
		
		public bool isUnlocked { get { return tech.isUnlocked; } }
		
		public CustomMachine(string name, string desc, string sprite, string unlock, string template) {
			resource = new NewResourceDetails();
			resource.name = name;
			resource.description = desc;
			resource.craftingMethod = CraftingMethod.Assembler;
			resource.craftTierRequired = 0;
			resource.headerTitle = "Logistics"; //TODO
			resource.subHeaderTitle = "Utility";
			resource.maxStackCount = 50;
			resource.sortPriority = 998;
			resource.unlockName = unlock;
			resource.parentName = template;
			resource.sprite = TextureManager.createSprite(TextureManager.getTexture(TTUtil.tryGetModDLL(true), sprite));
			
			definition = ScriptableObject.CreateInstance<V>();
		}
		
		public CustomTech createUnlock(Unlock.TechCategory cat, ResearchCoreDefinition.CoreType type, int cores) {
			tech = new CustomTech(cat, type, cores);
			tech.displayName = name;
			tech.description = description;
			tech.sprite = sprite;
			return tech;
		}
		
		public NewRecipeDetails addRecipe(string id, int amt = 1) {
			recipe = new NewRecipeDetails();
			recipe.GUID = id;
			recipe.craftingMethod = CraftingMethod.Assembler;
			recipe.craftTierRequired = 0;
			recipe.duration = 1F;
			recipe.sortPriority = 10;
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
			EMUAdditions.AddNewMachine(definition, resource);
			if (recipe != null)
				EMUAdditions.AddNewRecipe(recipe);
			if (tech != null)
				tech.register();
			
			EMU.Events.GameDefinesLoaded += () => {
				Unlock unlock = TTUtil.getUnlock(name);
				EMU.Resources.GetResourceInfoByName(name, true).unlock = unlock;
				
				unlock.unlockedRecipes.Add(recipe.ConvertToRecipe());
			};
			
			TTUtil.log("Registered machine " + this, TTUtil.tryGetModDLL(true));
		}
		
		public override sealed string ToString() {
			return resource.name;
		}
		
		public bool isThisMachine(IMachineInstance<T, V> inst) {
			return inst.myDef.displayName == name;
		}
		
	}
}
