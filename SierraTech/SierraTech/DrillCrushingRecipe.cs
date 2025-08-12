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

	public class DrillCrushingRecipe : CustomRecipe {
		
		private static int scrapID;
		private static int carbonFrameID;
		private static int sensorBlockID;
		private static int actuatorID;
		private static int drill6ID;
		private static int drill7ID;
		private static float scrapPerCarbonFrameDipping;
		private static float scrapPerActuator;
		private static float scrapPerSensorBlock;
		private static float scrapPerDrill6;
		private static float scrapPerDrill7;
		private static int scrapPerUnpacking;
		
		private static SchematicsRecipeData drill7Recipe;
		
		static DrillCrushingRecipe() {
			DIMod.onDefinesLoadedFirstTime += () => {
				scrapID = EMU.Resources.GetResourceIDByName(EMU.Names.Resources.ScrapOre);
				carbonFrameID = EMU.Resources.GetResourceIDByName(EMU.Names.Resources.CarbonFrame);
				sensorBlockID = EMU.Resources.GetResourceIDByName(EMU.Names.Resources.SensorBlock);
				actuatorID = EMU.Resources.GetResourceIDByName(EMU.Names.Resources.Actuator);
				drill6ID = EMU.Resources.GetResourceIDByName(EMU.Names.Resources.ExcavatorBitMKVI);
				drill7ID = EMU.Resources.GetResourceIDByName(EMU.Names.Resources.ExcavatorBitMKVII);
			};
			DIMod.onRecipesLoaded += () => {
				SchematicsRecipeData scrapDip = GameDefines.instance.GetValidCrusherRecipes(new List<int>{
					scrapID,
					EMU.Resources.GetResourceIDByName(EMU.Names.Resources.ShiverthornExtractGel),
				}, 9999, 9999, false)[0];
				SchematicsRecipeData scrapUnpack = GameDefines.instance.GetValidCrusherRecipes(new List<int>{
					EMU.Resources.GetResourceIDByName(EMU.Names.Resources.CondensedScrapOre),
				}, 9999, 1, false)[0];
				scrapPerUnpacking = scrapUnpack.getYield(scrapID);
				
				SchematicsRecipeData sensorBlock = TTUtil.getRecipesByOutput(sensorBlockID)[0];
				SchematicsRecipeData actuator = TTUtil.getRecipesByOutput(actuatorID)[0];
				SchematicsRecipeData drill6 = TTUtil.getRecipesByOutput(drill6ID)[0];
				drill7Recipe = TTUtil.getRecipesByOutput(drill7ID)[0];
				
				scrapPerCarbonFrameDipping = scrapDip.getCost(scrapID)/(float)scrapDip.getYield(carbonFrameID);
				scrapPerSensorBlock = (scrapPerCarbonFrameDipping*sensorBlock.getCost(carbonFrameID)+MachineScrapRecipe.getScrapPerRelay()*sensorBlock.getCost(EMU.Names.Resources.RelayCircuit))/(float)(sensorBlock.getYield(sensorBlockID)*4); //x4 since assembler
				scrapPerActuator = scrapPerCarbonFrameDipping*actuator.getCost(carbonFrameID)/(float)(actuator.getYield(actuatorID)*4); //x4 since assembler
				scrapPerDrill6 = (scrapPerCarbonFrameDipping*drill6.getCost(carbonFrameID)+scrapPerActuator*drill6.getCost(actuatorID))/(float)(drill6.getYield(drill6ID)*4); //x4 since assembler
				scrapPerDrill7 = (scrapPerSensorBlock*drill7Recipe.getCost(sensorBlockID)+scrapPerDrill6*drill7Recipe.getCost(drill6ID))/(float)(drill7Recipe.getYield(drill7ID)*4); //x4 since assembler
				
				TTUtil.log("Computed the following scrap per item ratios:");
				TTUtil.log(scrapUnpack.toDebugString()+": "+scrapPerUnpacking+"x");
				TTUtil.log(scrapDip.toDebugString()+": "+scrapPerCarbonFrameDipping.ToString("0.00"));
				TTUtil.log(sensorBlock.toDebugString()+": "+scrapPerSensorBlock.ToString("0.00"));
				TTUtil.log(actuator.toDebugString()+": "+scrapPerActuator.ToString("0.00"));
				TTUtil.log(drill6.toDebugString()+": "+scrapPerDrill6.ToString("0.00"));
				TTUtil.log(drill7Recipe.toDebugString()+": "+scrapPerDrill7.ToString("0.00"));
			};
		}

		internal DrillCrushingRecipe() : base("drill7_to_scrap", CraftingMethod.Crusher) {
			ingredients = new List<RecipeResourceInfo>() {
				new RecipeResourceInfo() {
					name = EMU.Names.Resources.ExcavatorBitMKVII,
					quantity = 1
				},
				new RecipeResourceInfo() {
					name = EMU.Names.Resources.SesamiteCrystal,
					quantity = 2
				}
			};
			outputs = new List<RecipeResourceInfo>() {
				new RecipeResourceInfo() {
					name = EMU.Names.Resources.CondensedScrapOre,
					quantity = 1 //will be replaced
				}
			};
			
			finalFixes = () => {
				int raw = MathUtil.roundUpToX((scrapPerDrill7*1.2F*SierraTechMod.config.getFloat(STConfig.ConfigEntries.SCRAPCYCLERATIO)).CeilToInt(), 250);
				recipe.outputQuantities[0] = raw/scrapPerUnpacking;
				int surplus = raw-scrapPerDrill7.FloorToInt();
				TTUtil.log("Computed dense scrap yield of "+recipe.outputQuantities[0]+" for "+recipe.toDebugString()+" (raw scrap amount "+raw+")");
				TTUtil.log("Maximum achievable surplus scrap per cycle = "+surplus);
				if (recipe.outputQuantities[0] > EMU.Resources.GetResourceInfoByName(EMU.Names.Resources.CondensedScrapOre).maxStackCount)
					TTUtil.log("Over max stack size!");
				
				SchematicsRecipeData crystalCrushing = GameDefines.instance.GetValidCrusherRecipes(new List<int>{
					EMU.Resources.GetResourceIDByName(EMU.Names.Resources.SesamiteCrystal),
				}, 9999, 9999, false)[0];
				recipe.chargeGeneratedWhenProcessed = crystalCrushing.chargeGeneratedWhenProcessed.multiplyBy(0.67);
				recipe.maxChargeGenerated = crystalCrushing.maxChargeGenerated.multiplyBy(0.67);
				recipe.chargeGenerationMultiplier = crystalCrushing.chargeGenerationMultiplier;
			};
			DIMod.onTechsLoadedFirstTime += () => {
				Unlock u = TTUtil.getUnlock(EMU.Names.Unlocks.ExcavatorBitMKVII);
				List<SchematicsRecipeData> li = new List<SchematicsRecipeData>(u.unlockedRecipes);
				li.Add(recipe);
				TTUtil.setUnlockRecipes(EMU.Names.Unlocks.ExcavatorBitMKVII, li.ToArray());
			};
		}
	}
}
