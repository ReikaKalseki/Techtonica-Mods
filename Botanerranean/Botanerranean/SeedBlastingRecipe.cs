using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Linq;

using BepInEx;

using ReikaKalseki.DIANEXCAL;

using HarmonyLib;

using UnityEngine;

using EquinoxsModUtils;
using EquinoxsModUtils.Additions;
using EquinoxsModUtils.Additions.ContentAdders;
using EquinoxsModUtils.Additions.Patches;

using PropStreaming;

namespace ReikaKalseki.Botanerranean {

	public class SeedBlastingRecipe : CustomRecipe {
		
		private static readonly float BLASTING_EFFICIENCY_FACTOR = 3.5F; //so ~35:1 instead of 125:1
		
		private static readonly Dictionary<string, SeedBlastingRecipe> recipeItems = new Dictionary<string, SeedBlastingRecipe>();
		
		private static int carbonPowderID;
		private static int carbonPowderBrickID;
		private static float powderPerBrick;
		
		public readonly CustomRecipe basicRecipe;
		
		public string sourceItem { get { return basicRecipe.ingredients[0].name; } }
		
		static SeedBlastingRecipe() {
			DIMod.onDefinesLoadedFirstTime += () => {
				carbonPowderID = EMU.Resources.GetResourceIDByName(EMU.Names.Resources.CarbonPowder);
				carbonPowderBrickID = EMU.Resources.GetResourceIDByName(EMU.Names.Resources.CarbonPowderBrick);
			};
			DIMod.onRecipesLoaded += () => {
				SchematicsRecipeData brickFromPowder = TTUtil.getRecipe(isPowderToBrick);
				powderPerBrick = brickFromPowder.getCost(carbonPowderID)/(4F*brickFromPowder.getYield(carbonPowderBrickID)); //include 4x so is 500:1 to match thresh
				
				TTUtil.log("Computed powder per brick ratio of "+powderPerBrick.ToString("0.00"));
			};
		}
		
		private static bool isPowderToBrick(SchematicsRecipeData rec) {
			return rec.ingTypes.Length == 1 && rec.ingTypes[0].uniqueId == carbonPowderID && rec.outputTypes.Length == 1 && rec.outputTypes[0].uniqueId == carbonPowderBrickID;
		}

		internal SeedBlastingRecipe(CustomRecipe smeltingReference) : base(smeltingReference.GUID+"_to_brick", CraftingMethod.BlastSmelter) {
			basicRecipe = smeltingReference;
			recipeItems[basicRecipe.GUID] = this;
			
			ingredients = new List<RecipeResourceInfo>() {
				new RecipeResourceInfo() {
					name = sourceItem,
					quantity = 1 //will be replaced
				}
			};
			outputs = new List<RecipeResourceInfo>() {
				new RecipeResourceInfo() {
					name = EMU.Names.Resources.CarbonPowderBrick,
					quantity = 1
				}
			};
			
			finalFixes = () => {
				recipe.ingQuantities[0] = computeCost(basicRecipe);
				recipe.runtimeIngQuantities[0] = recipe.ingQuantities[0];
				TTUtil.log("Computed seed cost of "+recipe.ingQuantities[0]+" for "+recipe.toDebugString());
				if (recipe.ingQuantities[0] > EMU.Resources.GetResourceInfoByName(sourceItem).maxStackCount)
					TTUtil.log("Over max stack size!");
			};
		}
		
		private static int computeCost(CustomRecipe from) {
			float seedsPerPowder = from.ingredients[0].quantity/(float)from.outputs[0].quantity;
			return MathUtil.roundUpToX((seedsPerPowder*powderPerBrick/BLASTING_EFFICIENCY_FACTOR).CeilToInt(), 5);
		}
	}
}
