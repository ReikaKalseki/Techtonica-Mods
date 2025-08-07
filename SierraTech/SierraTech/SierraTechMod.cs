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

	[BepInPlugin("ReikaKalseki.SierraTech", "SierraTech", "1.0.0")]
	public class SierraTechMod : BaseUnityPlugin {

		public static SierraTechMod instance;
        
		internal static CustomTech[] sandPumpBoostTechs;
		internal static CustomTech sandPumpCoreTech;
		internal static CustomTech machineScrappingTech;
		internal static CustomTech sesamiteCarbonBrickTech;
		internal static CustomTech greenCoreEfficiencyTech; //increases coreEfficiency for green from 1 to 1.25 like blue and purple
		internal static CustomTech allCoreEfficiencyTech2; //increases coreEfficiency for all to 150%
		internal static CustomTech goldCrushingTech;
		
		internal static SchematicsRecipeData carbonBrickRecipe;
		internal static SchematicsRecipeData goldCrushingRecipe;
		internal static SchematicsRecipeData goldPowderSmeltingRecipe;
		internal static NewResourceDetails goldPowder;
        
		public static readonly Assembly modDLL = Assembly.GetExecutingAssembly();
		
		private static readonly SandPumpTech.TechLevel[] techLevels = new SandPumpTech.TechLevel[]{
			new SandPumpTech.TechLevel(1, ResearchCoreDefinition.CoreType.Blue, 100, 1.5F, 0),
			new SandPumpTech.TechLevel(2, ResearchCoreDefinition.CoreType.Green, 50, 2F, 1),
			new SandPumpTech.TechLevel(3, ResearchCoreDefinition.CoreType.Green, 100, 3F, 3),
			new SandPumpTech.TechLevel(4, ResearchCoreDefinition.CoreType.Gold, 50, 4F, 5),
			new SandPumpTech.TechLevel(5, ResearchCoreDefinition.CoreType.Gold, 100, 6F, 7),
			//new SandPumpTech.TechLevel(6, ResearchCoreDefinition.CoreType.Gold, 250),
		};

		public SierraTechMod() : base() {
			instance = this;
			TTUtil.log("Constructed ST mod object");
		}

		public void Awake() {
			TTUtil.log("Begin Initializing SierraTech");
			try {
				Harmony harmony = new Harmony("SierraTech");
				Harmony.DEBUG = true;
				FileLog.logPath = Path.Combine(Path.GetDirectoryName(modDLL.Location), "harmony-log_" + Path.GetFileName(Assembly.GetExecutingAssembly().Location) + ".txt");
				FileLog.Log("Ran mod register, started harmony (harmony log)");
				TTUtil.log("Ran mod register, started harmony");
				try {
					harmony.PatchAll(modDLL);
				}
				catch (Exception ex) {
					FileLog.Log("Caught exception when running patchers!");
					FileLog.Log(ex.Message);
					FileLog.Log(ex.StackTrace);
					FileLog.Log(ex.ToString());
				}
                
				sandPumpBoostTechs = new CustomTech[techLevels.Length];
				for (int i = 0; i < sandPumpBoostTechs.Length; i++) {
					sandPumpBoostTechs[i] = new SandPumpTech(techLevels[i]);
					sandPumpBoostTechs[i].register();
				}
				sandPumpCoreTech = new CustomTech(Unlock.TechCategory.Science, ResearchCoreDefinition.CoreType.Gold, 250);
				sandPumpCoreTech.displayName = "Core Boost (Sand Pump)";
				sandPumpCoreTech.description = "Increases speed of all Sand Pumps by 10% per Core Cluster.";
				sandPumpCoreTech.finalFixes = () => {
					Unlock u = sandPumpCoreTech.unlock;
					u.sprite = TTUtil.getUnlock(EMU.Names.Resources.SandPump).sprite;
					u.requiredTier = TTUtil.getTierAtStation(TTUtil.ProductionTerminal.SIERRA, 6);
					u.treePosition = TTUtil.getUnlock(EMU.Names.Resources.MiningCharge).treePosition;
				};
				sandPumpCoreTech.register();
				
				sesamiteCarbonBrickTech = new CustomTech(Unlock.TechCategory.Synthesis, ResearchCoreDefinition.CoreType.Gold, 250);
				sesamiteCarbonBrickTech.displayName = "Sesamite Blasting";
				sesamiteCarbonBrickTech.description = "Enables blast smelting of carbon bricks from sesamite stems.";
				sesamiteCarbonBrickTech.requiredTier = TTUtil.getTierAtStation(TTUtil.ProductionTerminal.SIERRA, 7); //max
				sesamiteCarbonBrickTech.treePosition = 0;
				sesamiteCarbonBrickTech.finalFixes = () => {
		        	TTUtil.log("Adjusting "+sesamiteCarbonBrickTech.displayName);
		        	Unlock tu = sesamiteCarbonBrickTech.unlock;
		        	tu.treePosition = TTUtil.getUnlock(EMU.Names.Unlocks.SmelterMKII).treePosition;
		        	tu.sprite = EMU.Resources.GetResourceInfoByName(EMU.Names.Resources.CarbonPowderBrick).sprite;
				};
				sesamiteCarbonBrickTech.register();
				
				NewRecipeDetails rec = new NewRecipeDetails();
				rec.GUID = name;
				rec.craftingMethod = CraftingMethod.BlastSmelter;
				rec.craftTierRequired = 0;
				rec.sortPriority = 10;
				rec.unlockName = EMU.Names.Unlocks.SesamiteStemCompression; //gets overwritten
				rec.ingredients = new List<RecipeResourceInfo>() {
					new RecipeResourceInfo() {
						name = EMU.Names.Resources.SesamiteStems,
						quantity = 8
					}
				};
				rec.outputs = new List<RecipeResourceInfo>() {
					new RecipeResourceInfo() {
						name = EMU.Names.Resources.CarbonPowderBrick,
						quantity = 1
					}
				};
				EMUAdditions.AddNewRecipe(rec);
				
				machineScrappingTech = new CustomTech(Unlock.TechCategory.Synthesis, ResearchCoreDefinition.CoreType.Gold, 250);
				machineScrappingTech.displayName = "Machine Scrapping";
				machineScrappingTech.description = "Allows the crushing of machines into scrap ore.";
				machineScrappingTech.requiredTier = TTUtil.getTierAtStation(TTUtil.ProductionTerminal.SIERRA, 7); //max
				machineScrappingTech.treePosition = 0;
				machineScrappingTech.finalFixes = () => {
		        	TTUtil.log("Adjusting "+machineScrappingTech.displayName);
		        	Unlock tu = machineScrappingTech.unlock;
		        	tu.treePosition = TTUtil.getUnlock(EMU.Names.Unlocks.ThresherMKII).treePosition;
		        	tu.sprite = EMU.Resources.GetResourceInfoByName(EMU.Names.Resources.ScrapOre).sprite;
				};
				machineScrappingTech.register();
				
				goldPowder = new NewResourceDetails();
				goldPowder.name = "Gold Powder";
				goldPowder.description = "Pulverized Gold Ore.";
				goldPowder.craftingMethod = CraftingMethod.Crusher;
				goldPowder.craftTierRequired = 0;
				goldPowder.headerTitle = "Logistics"; //TODO
				goldPowder.subHeaderTitle = "Utility";
				goldPowder.maxStackCount = 500;
				goldPowder.sortPriority = 998;
				goldPowder.unlockName = EMU.Names.Unlocks.GoldOreExtraction;
				goldPowder.parentName = EMU.Names.Resources.CopperOrePowder;
				goldPowder.sprite = TextureManager.createSprite(TextureManager.getTexture(modDLL, "Textures/GoldPowder"));
				EMUAdditions.AddNewResource(goldPowder);
				
				goldCrushingTech = new CustomTech(Unlock.TechCategory.Synthesis, ResearchCoreDefinition.CoreType.Gold, 24);
				goldCrushingTech.displayName = "Gold Refinement";
				goldCrushingTech.description = "Getting more out of your gold ore.";
				goldCrushingTech.requiredTier = TTUtil.getTierAtStation(TTUtil.ProductionTerminal.SIERRA, 6);
				goldCrushingTech.treePosition = 0;
				goldCrushingTech.finalFixes = () => {
		        	TTUtil.log("Adjusting "+goldCrushingTech.displayName);
		        	Unlock tu = goldCrushingTech.unlock;
		        	tu.treePosition = TTUtil.getUnlock(EMU.Names.Unlocks.SesamitePowderProcessing).treePosition;
		        	tu.sprite = goldPowder.sprite;
					tu.dependency1 = TTUtil.getUnlock(EMU.Names.Unlocks.GoldIngot);
					tu.dependencies = new List<Unlock>{tu.dependency1};
				};
				goldCrushingTech.register();
				
				rec = new NewRecipeDetails();
				rec.GUID = name;
				rec.craftingMethod = CraftingMethod.Crusher;
				rec.craftTierRequired = 0;
				rec.sortPriority = 10;
				rec.duration = 30;
				rec.unlockName = EMU.Names.Unlocks.GoldOreExtraction; //gets overwritten
				rec.ingredients = new List<RecipeResourceInfo>() {
					new RecipeResourceInfo() {
						name = EMU.Names.Resources.GoldOre,
						quantity = 90 // 180/min
					},
					new RecipeResourceInfo() {
						name = EMU.Names.Resources.Clay,
						quantity = 12 // 24/min
					}
				};
				rec.outputs = new List<RecipeResourceInfo>() {
					new RecipeResourceInfo() {
						name = goldPowder.name,
						quantity = 60 // 120/min, 66.67% ratio
					}
				};
				EMUAdditions.AddNewRecipe(rec);
				
				rec = new NewRecipeDetails();
				rec.GUID = name;
				rec.craftingMethod = CraftingMethod.Smelter;
				rec.craftTierRequired = 0;
				rec.sortPriority = 10;
				rec.duration = 6*4; //conversion factor of 4 since MkIII is the comparison point
				rec.unlockName = EMU.Names.Unlocks.GoldOreExtraction; //gets overwritten
				rec.ingredients = new List<RecipeResourceInfo>() {
					new RecipeResourceInfo() {
						name = goldPowder.name,
						quantity = 2 // gets doubled to 4, becomes 40/min at mkIII (so 3 smelters per crusher)
					}
				};
				rec.outputs = new List<RecipeResourceInfo>() {
					new RecipeResourceInfo() {
						name = EMU.Names.Resources.GoldIngot,
						quantity = 3 // 30/min at mkIII, 75% ratio, net 50% ratio (compared to direct smelt 25%)
					}
				};
				EMUAdditions.AddNewRecipe(rec);
				
				greenCoreEfficiencyTech = new CustomTech(Unlock.TechCategory.Science, ResearchCoreDefinition.CoreType.Gold, 40); //takes three green to make each, so equivalent green cost of 120, breakeven at spent 480
				greenCoreEfficiencyTech.displayName = "Green Core Power Boost";
				greenCoreEfficiencyTech.description = "Boosts the power of green cores to 125%, matching blue and purple.";
				greenCoreEfficiencyTech.finalFixes = () => {
					Unlock u = greenCoreEfficiencyTech.unlock;
					Unlock cube = TTUtil.getUnlock(EMU.Names.Unlocks.SpectralCubeColorless);
					Unlock yellow = TTUtil.getUnlock(EMU.Names.Unlocks.ResearchCoreYellow);
					u.sprite = TTUtil.getUnlock(EMU.Names.Unlocks.ResearchCoreGreen).sprite;
					u.requiredTier = TTUtil.getTierAfter(yellow.requiredTier);
					u.treePosition = yellow.treePosition;
					cube.treePosition = TTUtil.getUnlock(EMU.Names.Unlocks.MassProductionMiningCharge).treePosition; //left col
					u.dependency1 = yellow;
					u.dependencies = new List<Unlock>{u.dependency1};
				};
				greenCoreEfficiencyTech.register();
				
				allCoreEfficiencyTech2 = new CustomTech(Unlock.TechCategory.Science, ResearchCoreDefinition.CoreType.Gold, 500); //== 500 gold/1500 green/4500 blue/13500 purple, breakeven at spent 1000 gold
				allCoreEfficiencyTech2.displayName = "Research Core Power Boost II";
				allCoreEfficiencyTech2.description = "Boosts the power of all research cores to 150%.";
				allCoreEfficiencyTech2.finalFixes = () => {
					Unlock u = allCoreEfficiencyTech2.unlock;
					u.sprite = TTUtil.getUnlock(EMU.Names.Unlocks.ResearchCoreYellow).sprite;
					u.requiredTier = TTUtil.getTierAtStation(TTUtil.ProductionTerminal.SIERRA, 7); //max
					u.dependency1 = greenCoreEfficiencyTech.unlock;
					u.treePosition = u.dependency1.treePosition;
					u.dependencies = new List<Unlock>{u.dependency1};
				};
				allCoreEfficiencyTech2.register();
				
				new MachineScrapRecipe(EMU.Names.Resources.AssemblerMKII).register();
				new MachineScrapRecipe(EMU.Names.Resources.ThresherMKII).register();
				new MachineScrapRecipe(EMU.Names.Resources.CrankGeneratorMKII).register();
				new MachineScrapRecipe(EMU.Names.Resources.ResearchCore520nmGreen).register();

				DIMod.onDefinesLoadedFirstTime += onDefinesLoaded;
				DIMod.onTechsLoadedFirstTime += onTechsLoaded;
				DIMod.onRecipesLoaded += onRecipesLoaded;
				DIMod.onTechActivatedEvent += onTechActivated;
				DIMod.onTechDeactivatedEvent += onTechDeactivated;
				EMU.Events.GameLoaded += updateCoreEfficiency;
			}
			catch (Exception e) {
				TTUtil.log("Failed to load SierraTech: " + e);
			}
			TTUtil.log("Finished Initializing SierraTech");
		}
        
		private static void onDefinesLoaded() {
        	TTUtil.setDrillUsableUntil(EMU.Names.Resources.ExcavatorBitMKIV, TTUtil.ElevatorLevels.ARCHIVE); //or SIERRA
        	TTUtil.setDrillUsableUntil(EMU.Names.Resources.ExcavatorBitMKV, TTUtil.ElevatorLevels.MECH);
        	ResourceInfo drill6 = EMU.Resources.GetResourceInfoByName(EMU.Names.Resources.ExcavatorBitMKVI);
        	drill6.digFuelPoints = drill6.digFuelPoints.multiplyBy(1.6); //from 1250 to 2000
        	EMU.Resources.GetResourceInfoByName(EMU.Names.Resources.ExcavatorBitMKVII).digFuelPoints *= 2; //from 2500 to 5000
		}
		
		private static void onTechsLoaded() {
        	TTUtil.setUnlockRecipes(sesamiteCarbonBrickTech.name, carbonBrickRecipe);
        	TTUtil.setUnlockRecipes(goldCrushingTech.name, goldCrushingRecipe, goldPowderSmeltingRecipe);
        	
        	TTUtil.getUnlock(EMU.Names.Unlocks.SesamiteCrystalSynthesis).adjustCoreCost(0.333); //from 300 to 100
        	TTUtil.getUnlock(EMU.Names.Unlocks.SesamitePowderExtraction).adjustCoreCost(2/3.5); //from 350 to 200
        	
        	Unlock mut = TTUtil.getUnlock(EMU.Names.Unlocks.SesamiteMutation);
        	Unlock ycore = TTUtil.getUnlock(EMU.Names.Unlocks.ResearchCoreYellow);
        	ycore.treePosition = mut.treePosition;
        	greenCoreEfficiencyTech.unlock.treePosition = mut.treePosition;
        	allCoreEfficiencyTech2.unlock.treePosition = mut.treePosition;
        	mut.treePosition = TTUtil.getUnlock(EMU.Names.Unlocks.MassProductionMiningCharge).treePosition;
		}
        
		private static void onRecipesLoaded() {
			SchematicsRecipeData crystalBuilding = GameDefines.instance.GetValidCrusherRecipes(new List<int>{
				EMU.Resources.GetResourceIDByName(EMU.Names.Resources.ShiverthornExtract),
				EMU.Resources.GetResourceIDByName(EMU.Names.Resources.SesamitePowder),
			}, 9999, 9999, false)[0];
			crystalBuilding.scalePower(2, 2); //from 150 max 18MJ to 300 max 72MJ
			SchematicsRecipeData crystalCrushing = GameDefines.instance.GetValidCrusherRecipes(new List<int>{
				EMU.Resources.GetResourceIDByName(EMU.Names.Resources.SesamiteCrystal),
			}, 9999, 9999, false)[0];
			crystalCrushing.scalePower(2400/950F, 2); //from 950 max 64k to 2.4MJ max 323MJ
			
			carbonBrickRecipe = GameDefines.instance.GetValidBlastSmelterRecipesForSingle(EMU.Resources.GetResourceIDByName(EMU.Names.Resources.SesamiteStems), 9999, 9999, false)[0];
			goldPowderSmeltingRecipe = GameDefines.instance.GetValidSmelterRecipesForSingle(EMU.Resources.GetResourceIDByName(goldPowder.name), 9999, 9999, false)[0];
			goldCrushingRecipe = GameDefines.instance.GetValidCrusherRecipes(new List<int>{EMU.Resources.GetResourceIDByName(EMU.Names.Resources.GoldOre), EMU.Resources.GetResourceIDByName(EMU.Names.Resources.Clay)}, 9999, 9999, false)[0];
		}
		
		public static void onTechActivated(int id) {
			updateCoreEfficiency();
		}
		
		public static void onTechDeactivated(int id) {
			updateCoreEfficiency();
		}
		
		private static void updateCoreEfficiency() {
			if (allCoreEfficiencyTech2.isUnlocked)
				TechTreeState.coreEffiencyMultipliers = new float[]{1.5F, 1.5F, 1.5F, 1.5F, 1, 1};
			else if (greenCoreEfficiencyTech.isUnlocked)
				TechTreeState.coreEffiencyMultipliers = new float[]{1.25F, 1.25F, 1.25F, 1, 1, 1};
			else
				TechTreeState.coreEffiencyMultipliers = new float[]{1.25F, 1, 1.25F, 1, 1, 1};
			TTUtil.log("Efficiency techs: "+allCoreEfficiencyTech2.isUnlocked+"/"+greenCoreEfficiencyTech.isUnlocked+" > "+TechTreeState.coreEffiencyMultipliers.toDebugString());
		}
		
		internal static SandPumpTech.TechLevel getHighestPumpLevelUnlocked() {
			for (int i = sandPumpBoostTechs.Length-1; i >= 0; i--) {
				if (sandPumpBoostTechs[i].isUnlocked)
					return techLevels[i];
			}
			return null;
		}
		
		public static float sandPumpFactor = 1;
		
		private static float getSandPumpRateFactor(float val) {
			SandPumpTech.TechLevel lvl = getHighestPumpLevelUnlocked();
			//TTUtil.log("Sand pump available tech level is "+lvl);
			if (lvl != null)
				val *= lvl.pumpMultiplier;
        	if (sandPumpCoreTech.isUnlocked && TechTreeState.instance.freeCores > 0) {
        		val *= 1+TTUtil.getCoreClusterCount()*0.1F;
			}
			return val*sandPumpFactor;
		}
		
		public static float getSandPumpRateDisplay(float val, SandPumpInspector insp, SandPumpInstance inst) {
			//inst.gridInfo.strata
			return getSandPumpRateFactor(val);
		}
		
		public static float getSandPumpRate(float val, SandVolume sand) {
			//sand.strata
			return getSandPumpRateFactor(val);
		}/*
        
		public static void tickSandVolume(SandVolume sand, float deltaTime) {
			bool flag = SandVolume.pendingDigsForStratas.ContainsKey(sand.strata);
			List<SandVolume.PendingDig> list = flag ? SandVolume.pendingDigsForStratas[sand.strata] : null;
			float num = (sand.strataSandFillRate > 0f) ? sand.strataSandFillRate : GameDefines.instance.SandVolumeFillRate;
			float num2 = 0f;
			sand.numOversandDigsThisFrame = 0;
			if (flag) {
				int i = 0;
				int count = list.Count;
				while (i < count) {
					if (list[i].underSand) {
						num2 += instance.SandPumpDigAmount;
					}
					else {
						num = Mathf.Max(num - instance.SandPumpPreventFillAmount * sand.strataSandDigRateMultiplier, 0f);
						sand.numOversandDigsThisFrame++;
					}
					i++;
				}
			}
			sand.addRateThisFrame = num;
			sand.digRateThisFrame = Mathf.Min(num2 * SandVolume.CHEAT_DigAmountModifier * sand.strataSandDigRateMultiplier, GameDefines.instance.SandPumpMaxRemovalRate * SandVolume.CHEAT_DigAmountModifier);
			sand.deltaRateThisFrame = (sand.addRateThisFrame - sand.digRateThisFrame) * deltaTime;
			if (sand.strata == VoxelManager.curStrataNum) {
				int num3 = 0;
				if ((sand.maxSandLevel - sand.state.linearSandLevel).Abs() > 1E-07f && sand.deltaRateThisFrame > 0f) {
					num3 = 2;
				}
				else
				if (sand.deltaRateThisFrame < 0f) {
					num3 = 1;
				}
				RuntimeManager.StudioSystem.setParameterByID(SandVolume.sandStatusAudioParamId, (float)num3, false);
			}
			sand.state.linearSandLevel += sand.deltaRateThisFrame;
			sand.state.linearSandLevel = Mathf.Clamp(sand.state.linearSandLevel, 0f, sand.maxSandLevel);
			sand.state.UpdateWeightedSandLevel(sand.maxSandLevel);
			if (flag) {
				list.Clear();
			}
		}*/

	}
}
