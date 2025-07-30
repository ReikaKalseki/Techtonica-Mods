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
		
		private int recipeID = -1;
        
		public string name { get { return resource.name; } }
        
		public ResourceInfo item { get { return EMU.Resources.GetResourceInfoByName(name); } }
		
		public bool isUnlocked { get { return tech.isUnlocked; } }
		
		public Unlock unlock { get { return TTUtil.getUnlock(name); } }
		
		public SchematicsRecipeData registeredRecipe { get { return GameDefines.instance.GetSchematicsRecipeDataById(recipeID); } }
		
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
			resource.sprite = TextureManager.createSprite(TextureManager.getTexture(TTUtil.tryGetModDLL(true), "Textures/"+sprite));
			
			definition = ScriptableObject.CreateInstance<V>();
		}
		
		public CustomTech createUnlock(Unlock.TechCategory cat, ResearchCoreDefinition.CoreType type, int cores) {
			tech = new CustomTech(cat, type, cores);
			tech.displayName = name;
			tech.description = item.description;
			tech.sprite = item.sprite;
			return tech;
		}
		
		public NewRecipeDetails addRecipe(int amt = 1) {
			recipe = new NewRecipeDetails();
			recipe.GUID = name;
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
			try {
				EMUAdditions.AddNewMachine(definition, resource, true);
				
				if (recipe != null)
					EMUAdditions.AddNewRecipe(recipe, true);
				else
					TTUtil.log("Machine '"+this+"' has no recipe!");
				
				if (tech != null)
					tech.register();
				else
					TTUtil.log("Machine '"+this+"' has no tech!");
				
				EMU.Events.GameDefinesLoaded += () => {
					EMU.Resources.GetResourceInfoByName(name, true).unlock = unlock;
				};
				
				EMU.Events.TechTreeStateLoaded += () => {
					recipeID = TTUtil.getRecipeID(EMU.Resources.GetResourceIDByName(name));
					if (recipeID != -1)
						unlock.unlockedRecipes.Add(registeredRecipe);
				};
				
				TTUtil.log("Registered machine " + this, TTUtil.tryGetModDLL(true));
			}
			catch (Exception ex) {
				TTUtil.log("Failed to register "+this+": "+ex.ToString(), TTUtil.tryGetModDLL(true));
			}
		}
		
		public override sealed string ToString() {
			return resource.name;
		}
		
		public bool isThisMachine(IMachineInstance<T, V> inst) {
			return isThisMachine(inst.myDef);
		}
		
		public bool isThisMachine(MachineDefinition<T, V> def) {
			return def.displayName == name;
		}
		
	}
}
