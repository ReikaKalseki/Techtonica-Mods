using System;
using System.Collections.Generic;
using System.Linq;
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
	
	public class CustomRecipe : NewRecipeDetails {
		
		public SchematicsRecipeData recipe { get; private set; }
		
		public Assembly ownerMod { get; private set; }
		
		public Action finalFixes;
		
		public CustomRecipe(string id, CraftingMethod cm = CraftingMethod.Assembler) {
			GUID = id;
			craftingMethod = cm;
			duration = 1;
			sortPriority = 10;
			
			ownerMod = TTUtil.tryGetModDLL();
		}
		
		public CustomRecipe setInputs(params object[] args) {
			TTUtil.setIngredients(this, args);
			return this;
		}
		
		public CustomRecipe setOutputs(params object[] args) {
			TTUtil.setProducts(this, args);
			return this;
		}
		
		public void register() {
			try {
				EMUAdditions.AddNewRecipe(this, true);
				DIMod.onRecipesLoaded += () => {
					recipe = lookupRecipe();
					if (recipe == null)
						throw new Exception("Recipe "+this+" failed to find registered counterpart");
					else
						TTUtil.log("Recipe "+GUID+" injected: "+recipe.toDebugString(), ownerMod);
					
					if (!GameDefines.instance.GetPossibleRecipes(craftingMethod, 9999, TTUtil.getMaxAllowedInputs(craftingMethod), false).Contains(recipe))
						throw new Exception("Recipe "+this+" was not available to its machine");
					
					if (finalFixes != null)
						finalFixes.Invoke();
				};
				TTUtil.log("Registered recipe "+this, ownerMod);
			}
			catch (Exception ex) {
				TTUtil.log("Failed to register "+this, ownerMod);
				throw ex;
			}
		}
		
		private SchematicsRecipeData lookupRecipe() {
			switch (craftingMethod) {
				case CraftingMethod.Assembler: 
					return TTUtil.getRecipesByOutput(outputs[0].name)[0];
				case CraftingMethod.Uncraftable: 
					return null;
				case CraftingMethod.Smelter: 
					return GameDefines.instance.GetValidSmelterRecipesForSingle(EMU.Resources.GetResourceIDByName(ingredients[0].name), 9999, 9999, false)[0];
				case CraftingMethod.Thresher: 
					return GameDefines.instance.GetThreshingRecipeForResource(EMU.Resources.GetResourceIDByName(ingredients[0].name));
				case CraftingMethod.BlastSmelter: 
					return GameDefines.instance.GetValidBlastSmelterRecipesForSingle(EMU.Resources.GetResourceIDByName(ingredients[0].name), 9999, 9999, false)[0];
				case CraftingMethod.Planter:
					return GameDefines.instance.GetPlanterRecipeBySeedInputId(EMU.Resources.GetResourceIDByName(ingredients[0].name));
				case CraftingMethod.Crusher:
					return GameDefines.instance.GetValidCrusherRecipes(ingredients.Select<RecipeResourceInfo, int>(i => EMU.Resources.GetResourceIDByName(i.name)).ToList(), 9999, 9999, false)[0];
			}
			return null;
		}
		
		public override sealed string ToString() {
			return "Recipe "+GUID;
		}
		
	}
}
