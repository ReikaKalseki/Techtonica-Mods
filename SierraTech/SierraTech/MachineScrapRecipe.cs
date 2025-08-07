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

namespace ReikaKalseki.SierraTech {

	public class MachineScrapRecipe {
		
		private static readonly Dictionary<string, MachineScrapRecipe> recipeItems = new Dictionary<string, MachineScrapRecipe>();
		private static readonly List<SchematicsRecipeData> recipes = new List<SchematicsRecipeData>();		
		
		private static int scrapID;
		private static int goldOreID;
		private static int goldID;
		private static int relayID;
		private static float scrapPerIngotShiverthorn;
		private static float scrapPerIngotSesamite;
		private static float goldOrePerIngot;
		private static float scrapPerRelayLeastEfficient;
		private static float scrapPerRelayMostEfficient;
		private static float weightedScrapPerRelay;
		
		public readonly string sourceItem;
		
		public SchematicsRecipeData recipe { get; private set; }
		
		static MachineScrapRecipe() {
			DIMod.onDefinesLoadedFirstTime += () => {
				scrapID = EMU.Resources.GetResourceIDByName(EMU.Names.Resources.ScrapOre);
				goldOreID = EMU.Resources.GetResourceIDByName(EMU.Names.Resources.GoldOre);
				goldID = EMU.Resources.GetResourceIDByName(EMU.Names.Resources.GoldIngot);
				relayID = EMU.Resources.GetResourceIDByName(EMU.Names.Resources.RelayCircuit);
			};
			DIMod.onRecipesLoaded += () => {
				SchematicsRecipeData goldOreProduction = GameDefines.instance.GetValidCrusherRecipes(new List<int>{
					scrapID,
					EMU.Resources.GetResourceIDByName(EMU.Names.Resources.ShiverthornExtract),
				}, 9999, 9999, false)[0];
				SchematicsRecipeData goldOreFromSesamite = GameDefines.instance.GetValidCrusherRecipes(new List<int>{
					scrapID,
					EMU.Resources.GetResourceIDByName(EMU.Names.Resources.SesamitePowder),
				}, 9999, 9999, false)[0];
				SchematicsRecipeData goldOreSmelting = GameDefines.instance.GetValidSmelterRecipes(new List<int>{
					goldOreID,
				}, 9999, 9999, false)[0];
				
				goldOrePerIngot = goldOreSmelting.getCost(goldOreID)/goldOreSmelting.getYield(goldID);
				
				//50 scrap per ingot by default
				scrapPerIngotShiverthorn = (goldOreProduction.getCost(scrapID)-goldOreProduction.getYield(scrapID))/(float)goldOreProduction.getYield(goldOreID)*goldOrePerIngot;
				//8 scrap per ingot by default
				scrapPerIngotSesamite = (goldOreFromSesamite.getCost(scrapID)-goldOreFromSesamite.getYield(scrapID))/(float)goldOreFromSesamite.getYield(goldOreID)*goldOrePerIngot;
				
				TTUtil.log("Computed scrap per ingot ratios of ["+scrapPerIngotShiverthorn.ToString("0.00")+", "+scrapPerIngotSesamite.ToString("0.00")+" via "+goldOreProduction.toDebugString()+" and "+goldOreFromSesamite.toDebugString()+" through "+goldOreSmelting.toDebugString());
				
				List<SchematicsRecipeData> li = TTUtil.getRecipesByOutput(relayID);
				SchematicsRecipeData relaysFromGoldAndScrap = null, relaysFromGoldInsteadOfCoolant = null;
				foreach (SchematicsRecipeData rec in li) {
					if (isGoldAndScrapRecipe(rec))
						relaysFromGoldAndScrap = rec;
					else if (isGoldButNotScrapRecipe(rec))
						relaysFromGoldInsteadOfCoolant = rec;
				}
				
				//include x4 since assembler II
				scrapPerRelayLeastEfficient = (relaysFromGoldAndScrap.getCost(goldID)*scrapPerIngotShiverthorn+relaysFromGoldAndScrap.getCost(scrapID))/((float)relaysFromGoldAndScrap.getYield(relayID)*4);
				scrapPerRelayMostEfficient = scrapPerIngotSesamite*relaysFromGoldInsteadOfCoolant.getCost(goldID)/((float)relaysFromGoldAndScrap.getYield(relayID)*4);
				
				weightedScrapPerRelay = Mathf.Lerp(scrapPerRelayLeastEfficient, scrapPerRelayMostEfficient, 0.8F); //deliberately overvalue scrap relative to most efficient so can get positive return in a loop
				
				TTUtil.log("Relay recipes are "+relaysFromGoldAndScrap.toDebugString()+" and "+relaysFromGoldInsteadOfCoolant.toDebugString());
				TTUtil.log("Computed scrap per relay ratio of ["+scrapPerRelayLeastEfficient.ToString("0.00")+", "+scrapPerRelayMostEfficient.ToString("0.00")+"]="+weightedScrapPerRelay.ToString("0.00"));
			};
			EMU.Events.TechTreeStateLoaded += () => {
        		TTUtil.setUnlockRecipes(SierraTechMod.machineScrappingTech.name, recipes.ToArray());
			};
		}
		
		 //the one with slabs and coolant is 25 gold = 200 scrap if using the good gold ore recipe, otherwise costs the same amount of scrap, since 250 scrap is 5 gold in shiverthorn one
		private static bool isGoldAndScrapRecipe(SchematicsRecipeData rec) {
			return rec.ingTypes.Length == 2 && (rec.ingTypes[0].uniqueId == scrapID || rec.ingTypes[1].uniqueId == goldID) && (rec.ingTypes[0].uniqueId == scrapID || rec.ingTypes[1].uniqueId == goldID);
		}
		
		private static bool isGoldButNotScrapRecipe(SchematicsRecipeData rec) {
		 	return rec.ingTypes.Length == 3 && rec.getCost(goldID) > 0 && rec.getCost(EMU.Names.Resources.ProcessorArray) > 0 && rec.getCost(EMU.Names.Resources.AtlantumSlab) > 0;
		}

		internal MachineScrapRecipe(string item) {
			sourceItem = item;
			recipeItems[item] = this;
		}
		
		public void register() {
			NewRecipeDetails rec = new NewRecipeDetails();
			rec.GUID = sourceItem+"_to_scrap";
			rec.craftingMethod = CraftingMethod.Crusher;
			rec.craftTierRequired = 0;
			rec.sortPriority = 10;
			rec.unlockName = EMU.Names.Unlocks.ScrapProcessingBasic; //gets overwritten
			rec.ingredients = new List<RecipeResourceInfo>() {
				new RecipeResourceInfo() {
					name = sourceItem,
					quantity = 1
				},
				new RecipeResourceInfo() {
					name = EMU.Names.Resources.SesamiteIngot,
					quantity = 1
				}
			};
			rec.outputs = new List<RecipeResourceInfo>() {
				new RecipeResourceInfo() {
					name = EMU.Names.Resources.ScrapOre,
					quantity = 1 //will be replaced
				}
			};
			EMUAdditions.AddNewRecipe(rec);
			DIMod.onRecipesLoaded += () => {
				recipe = GameDefines.instance.GetValidCrusherRecipes(new List<int>{EMU.Resources.GetResourceIDByName(sourceItem)}, 9999, 9999, false)[0];
				int surplus;
				float f = sourceItem == EMU.Names.Resources.ResearchCore520nmGreen ? 2.5F : 1; //more involved = more reward
				recipe.outputQuantities[0] = computeYield(sourceItem, out surplus, f);
				TTUtil.log("Computed scrap yield of "+recipe.outputQuantities[0]+" for "+recipe.toDebugString());
				TTUtil.log("Maximum achievable surplus scrap per cycle = "+surplus+" ("+surplus*2+" if using gold crushing)");
				if (recipe.outputQuantities[0] > EMU.Resources.GetResourceInfoByName(EMU.Names.Resources.ScrapOre).maxStackCount)
					TTUtil.log("Over max stack size!");
			};
		}
		
		private static int computeYield(string from, out int surplus, float efficacy = 1) {
		 	SchematicsRecipeData rec = TTUtil.getRecipesByOutput(from)[0];
		 	float relaysPer = rec.getCost(relayID)/((float)rec.outputQuantities[0]*4); //include x4 since assembler II
			int ret = MathUtil.roundUpToX((weightedScrapPerRelay*relaysPer*efficacy).CeilToInt(), 50);
			surplus = ret-(scrapPerRelayMostEfficient*relaysPer).FloorToInt();
			return ret;
		}
	}
}
