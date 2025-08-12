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

    [BepInPlugin("ReikaKalseki.Botanerranean", "Botanerranean", "1.0.0")]
    public class BotanerraneanMod : BaseUnityPlugin {

        public static BotanerraneanMod instance;
        
        private static CoreBoostTech planterCoreTech;
        private static CoreBoostTech seedYieldCoreTech;
		internal static CustomTech[] seedYieldBoostTechs;
        private static CustomTech t3PlanterTech;
        private static CustomTech seedPlantmatterTech;
        private static CustomTech seedCarbonTech;
        private static CustomMachine<PlanterInstance, PlanterDefinition> t3Planter;
        private static ResourceInfo t2Planter;
        private static CustomRecipe kindlevineSeedBlasting;
        private static CustomRecipe shiverthornSeedBlasting;
        private static CustomRecipe kindlevineSeedSmelting;
        private static CustomRecipe shiverthornSeedSmelting;
		
        private static SeedYieldTech.TechLevel[] techLevels;
        
        public static SchematicsRecipeData planter2Recipe;
        public static SchematicsRecipeData planter3Recipe;
        
        public static readonly Assembly modDLL = Assembly.GetExecutingAssembly();
    
    	public static readonly Config<BTConfig.ConfigEntries> config = new Config<BTConfig.ConfigEntries>(modDLL);
        
        private static readonly Dictionary<string, NewRecipeDetails> seedRecipes = new Dictionary<string, NewRecipeDetails>();

        public BotanerraneanMod() : base() {
            instance = this;
            TTUtil.log("Constructed BT mod object");
        }

        public void Awake() {
            TTUtil.log("Begin Initializing Botanerranean");
            try {
            	config.load();
            	
				techLevels = new SeedYieldTech.TechLevel[] {
            		new SeedYieldTech.TechLevel(1, ResearchCoreDefinition.CoreType.Purple, 150, config.getInt(BTConfig.ConfigEntries.SEED1EFFECT), TTUtil.getTierAtStation(TTUtil.ProductionTerminal.VICTOR, 1)),
					new SeedYieldTech.TechLevel(2, ResearchCoreDefinition.CoreType.Blue, 80, config.getInt(BTConfig.ConfigEntries.SEED2EFFECT), TTUtil.getTierAtStation(TTUtil.ProductionTerminal.VICTOR, 5)),
					new SeedYieldTech.TechLevel(3, ResearchCoreDefinition.CoreType.Blue, 160, config.getInt(BTConfig.ConfigEntries.SEED3EFFECT), TTUtil.getTierAtStation(TTUtil.ProductionTerminal.XRAY, 1), TTUtil.TechColumns.RIGHT),
				};
            	
                Harmony harmony = new Harmony("Botanerranean");
                Harmony.DEBUG = true;
                FileLog.logPath = Path.Combine(Path.GetDirectoryName(modDLL.Location), "harmony-log_"+Path.GetFileName(Assembly.GetExecutingAssembly().Location)+".txt");
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
				
				t3Planter = new CustomMachine<PlanterInstance, PlanterDefinition>("Planter MKIII", "Grows plants at quadruple the speed of the MK1 planter [this desc should be replaced].", "PlanterT3", EMU.Names.Unlocks.PlanterMKII, EMU.Names.Resources.PlanterMKII);
				t3Planter.addRecipe().ingredients = new List<RecipeResourceInfo>{new RecipeResourceInfo{name=EMU.Names.Resources.PlanterMKII, quantity = 1}}; //will populate it later
                
				t3PlanterTech = t3Planter.createUnlock(Unlock.TechCategory.Synthesis, ResearchCoreDefinition.CoreType.Gold, 100);
				
				t3Planter.register();
                
				planterCoreTech = new CoreBoostTech(config.getInt(BTConfig.ConfigEntries.PLANTERCOREEFFECT), ResearchCoreDefinition.CoreType.Blue, 100);
				planterCoreTech.setText("Planter", "speed of all Planters");
				planterCoreTech.setPosition(TTUtil.getTierAtStation(TTUtil.ProductionTerminal.SIERRA, 6), TTUtil.TechColumns.CENTERRIGHT);
				planterCoreTech.setSprite(EMU.Names.Unlocks.Planter);
				planterCoreTech.register();
				
				seedYieldBoostTechs = new CustomTech[techLevels.Length];
				for (int i = 0; i < seedYieldBoostTechs.Length; i++) {
					seedYieldBoostTechs[i] = new SeedYieldTech(techLevels[i]);
					seedYieldBoostTechs[i].register();
				}
				
				seedYieldCoreTech = new CoreBoostTech(config.getInt(BTConfig.ConfigEntries.SEEDCOREEFFECT), ResearchCoreDefinition.CoreType.Green, 100, EMU.Names.Unlocks.CoreBoostThreshing);
				seedYieldCoreTech.setText("Threshing Seed Yield", "seed yield from threshing");
				seedYieldCoreTech.setPosition(TTUtil.getTierAtStation(TTUtil.ProductionTerminal.SIERRA, 6), TTUtil.TechColumns.RIGHT);
				seedYieldCoreTech.setSprite(EMU.Names.Resources.KindlevineSeed);
				seedYieldCoreTech.register();
				
				kindlevineSeedSmelting = makeSeedToPlantmatterAndCarbon(EMU.Names.Resources.KindlevineSeed, 1.5F);
				shiverthornSeedSmelting = makeSeedToPlantmatterAndCarbon(EMU.Names.Resources.ShiverthornSeed, 0.5F);
				//sesamiteSeedSmelting = makeSeedToPlantmatterAndCarbon(EMU.Names.Resources.SesamiteSeed, 5); //costs 2 of each seed normally to make 4 sesamite, but recompute to be sure
				kindlevineSeedBlasting = new SeedBlastingRecipe(kindlevineSeedSmelting);
				shiverthornSeedBlasting = new SeedBlastingRecipe(shiverthornSeedSmelting);
				//sesamiteSeedBlasting = new SeedBlastingRecipe(sesamiteSeedSmelting);
				kindlevineSeedBlasting.register();
				shiverthornSeedBlasting.register();
				//sesamiteSeedBlasting.register();
				
				seedPlantmatterTech = new CustomTech(Unlock.TechCategory.Synthesis, "Seed Recycling I", "Reusing surplus seeds.", ResearchCoreDefinition.CoreType.Purple, 50);
				seedPlantmatterTech.setPosition(TTUtil.getTierAtStation(TTUtil.ProductionTerminal.VICTOR, 4), TTUtil.TechColumns.CENTERLEFT);
				seedPlantmatterTech.setSprite("SeedPlantmatter", false);
				seedPlantmatterTech.register();
				
				seedCarbonTech = new CustomTech(Unlock.TechCategory.Synthesis, "Seed Recycling II", "A more lucrative use for surplus seeds.", ResearchCoreDefinition.CoreType.Blue, 20);
				seedCarbonTech.setSprite("SeedToCarbon", false);
				seedCarbonTech.finalFixes = () => {
		        	TTUtil.log("Adjusting "+seedCarbonTech.displayName);
		        	Unlock carb = TTUtil.getUnlock(EMU.Names.Unlocks.CarbonPowder);
		        	Unlock tu = seedCarbonTech.unlock;
		        	tu.treePosition = carb.treePosition;
		        	tu.requiredTier = TTUtil.getTierAfter(carb.requiredTier);
		        	tu.dependency1 = carb;
		        	tu.dependency2 = seedPlantmatterTech.unlock;
		        	tu.dependencies = new List<Unlock>{tu.dependency1, tu.dependency2};
				};
        		seedCarbonTech.setRecipes(kindlevineSeedSmelting, shiverthornSeedSmelting, kindlevineSeedBlasting, shiverthornSeedBlasting);
				seedCarbonTech.register();			

				DIMod.onDefinesLoadedFirstTime += onDefinesLoaded;
				DIMod.onTechsLoadedFirstTime += onTechsLoaded;
				
				DIMod.onRecipesLoaded += onRecipesLoaded;
			}
			catch (Exception e) {
                TTUtil.log("Failed to load Botanerranean: "+e);
            }
            TTUtil.log("Finished Initializing Botanerranean");
        }
        
		private static void onDefinesLoaded() {
			TTUtil.log("Copying PlanterMKII data to MKIII");			
			Unlock pu = TTUtil.getUnlock(EMU.Names.Resources.PlanterMKII);		
			Unlock t3 = TTUtil.getUnlock("Planter MKIII");
			t3.requiredTier = pu.requiredTier;
			t3.treePosition = pu.treePosition;
			t3.coresNeeded.Clear();
			t3.coresNeeded.Add(new Unlock.RequiredCores{type=ResearchCoreDefinition.CoreType.Green, number=pu.coresNeeded[0].number});
			t3.dependencies = new List<Unlock>(pu.dependencies);
			t3.dependencies.Add(pu);
			t3.dependency1 = pu.dependency1;
			t3.dependency2 = pu.dependency2;
			t3.numScansNeeded = 0;//pu.numScansNeeded;
			
			TTUtil.log("Moving PlanterMKII tech");				
			Unlock thresh = TTUtil.getUnlock(EMU.Names.Resources.ThresherMKII);
			pu.requiredTier = TTUtil.getTierAtStation(TTUtil.ProductionTerminal.VICTOR, 2);//TTUtil.getTierAfter(thresh.requiredTier);
			pu.treePosition = TTUtil.getUnlock(EMU.Names.Resources.AssemblerMKII).treePosition;
			pu.coresNeeded.Clear();
			pu.coresNeeded.Add(new Unlock.RequiredCores{type = ResearchCoreDefinition.CoreType.Blue, number = 50});
			pu.dependencies = new List<Unlock>{thresh};
			pu.dependency1 = thresh.dependency1;
			pu.dependency2 = thresh.dependency2;
			
			t2Planter = EMU.Resources.GetResourceInfoByName(EMU.Names.Resources.PlanterMKII);
			t3Planter.item.description = t2Planter.description.Replace("rapid", "extremely fast");
			t3.setDescription(t3Planter.item.description);
		}
        
        private static void onRecipesLoaded() {
        	planter2Recipe = TTUtil.getRecipesByOutput(EMU.Names.Resources.PlanterMKII)[0];
        	planter3Recipe = TTUtil.getRecipesByOutput(t3Planter.name)[0];
        	
        	planter3Recipe.ingTypes = new ResourceInfo[planter2Recipe.ingTypes.Length];
        	for (int i = 0; i < planter3Recipe.ingTypes.Length; i++) {
        		planter3Recipe.ingTypes[i] = planter2Recipe.ingTypes[i];
        		if (planter3Recipe.ingTypes[i].uniqueId == EMU.Resources.GetResourceIDByName(EMU.Names.Resources.Planter))
        			planter3Recipe.ingTypes[i] = EMU.Resources.GetResourceInfoByName(EMU.Names.Resources.PlanterMKII);
        	}
        	planter3Recipe.ingQuantities = new int[planter2Recipe.ingQuantities.Length];
        	planter3Recipe.duration = planter2Recipe.duration*4;
        	Array.Copy(planter2Recipe.ingQuantities, planter3Recipe.ingQuantities, planter3Recipe.ingQuantities.Length);
        	TTUtil.compileRecipe(planter3Recipe);
        	
        	TTUtil.setIngredients(planter2Recipe, EMU.Names.Resources.Planter, 2, EMU.Names.Resources.SteelFrame, 1, EMU.Names.Resources.AdvancedCircuit, 15);
        }
        
        private static void onTechsLoaded() {
        	TTUtil.setUnlockRecipes(EMU.Names.Resources.PlanterMKII, planter2Recipe);
        	TTUtil.setUnlockRecipes(t3Planter.name, planter3Recipe);
        	
        	seedPlantmatterTech.setRecipes(TTUtil.getRecipes(isSeedRecycling).ToArray());
        }
        
        private static bool isSeedRecycling(SchematicsRecipeData rec) {
        	if (rec == null)
        		return false;
        	if (rec.ingTypes == null || rec.ingTypes[0] == null || rec.outputTypes == null || rec.outputTypes[0] == null) {
        		TTUtil.log("Invalid recipe "+rec.toDebugString());
        		return false;
        	}
        	return rec.ingTypes.Length == 1 && TTUtil.isSeed(rec.ingTypes[0]) && rec.outputTypes.Length == 1 && rec.outputTypes[0].uniqueId == EMU.Resources.GetResourceIDByName(EMU.Names.Resources.Plantmatter);
        }
        
        private static CustomRecipe makeSeedToPlantmatterAndCarbon(string name, float relativeValue = 1) {        	
			CustomRecipe recipe = new CustomRecipe(name+"_To_Plantmatter");
			recipe.duration = 2F;
			recipe.unlockName = ""; //will be replaced
			recipe.ingredients = new List<RecipeResourceInfo>() {
				new RecipeResourceInfo() {
					name = name,
					quantity = 1,
				}
			};
			recipe.outputs = new List<RecipeResourceInfo>() {
				new RecipeResourceInfo() {
					name = EMU.Names.Resources.Plantmatter,
					quantity = 6.multiplyBy(relativeValue*config.getFloat(BTConfig.ConfigEntries.SEEDPLANTMATTERRATIO1)),
				}
			};			
			recipe.register();
			    	
			CustomRecipe smelt = new CustomRecipe(name+"_To_Carbon", CraftingMethod.Smelter);
			smelt.duration = 15F;
			smelt.unlockName = ""; //will be replaced
			smelt.ingredients = new List<RecipeResourceInfo>() {
				new RecipeResourceInfo() {
					name = name,
					quantity = 10, //2.5/min
				}
			};
			smelt.outputs = new List<RecipeResourceInfo>() {
				new RecipeResourceInfo() {
					name = EMU.Names.Resources.CarbonPowder,
					quantity = (10*relativeValue*config.getFloat(BTConfig.ConfigEntries.SEEDPLANTMATTERRATIO2)).FloorToInt(),
				}
			};			
			smelt.register();
			return smelt;
        }
        
        public static bool tickPlantSlot(ref PlanterInstance.PlantSlot slot, float increment, ref PlanterInstance owner) { //no need to handle T2, as it is already doubled
			return slot.UpdateGrowth(increment*getPlanterSpeed(ref owner));
		}
        
        public static float getPlanterGrowthTimeForUI(float val, ref PlanterInstance pi) {
        	return val/getPlanterSpeed(ref pi);
        }
        
        private static float getPlanterSpeed(ref PlanterInstance pi) {
        	float val = globalPlanterSpeedFactor;
        	val *= 1+planterCoreTech.currentEffect;
			if (t3Planter != null && t3Planter.isThisMachine(pi)) {
				val *= 4;
			}
        	return val;
        }
        
        public static float globalPlanterSpeedFactor = 1;
        
        public static void initializePlanterSettings(PlanterDefinition def) {
        	if (t3Planter.isThisMachine(def)) {
        		def.runtimePowerSettings.kWPowerConsumption = def.runtimePowerSettings.kWPowerConsumption*75/35; //from 350kW/slot to 750
        		TTUtil.log("Set "+def.toDebugString()+" power cost to "+def.runtimePowerSettings.GetPowerConsumption());
        	}
        	else if (def.displayName.EndsWith("MKII", StringComparison.InvariantCultureIgnoreCase)) {
        		def.runtimePowerSettings.kWPowerConsumption = def.runtimePowerSettings.kWPowerConsumption*5/7; //from 350kW/slot to 250
        		TTUtil.log("Set "+def.toDebugString()+" power cost to "+def.runtimePowerSettings.GetPowerConsumption());
        	}
        	else if (def.displayName == "Planter") {
        		def.runtimePowerSettings.kWPowerConsumption = def.runtimePowerSettings.kWPowerConsumption.multiplyBy(0.4); //from 50kW/slot to 20
        		TTUtil.log("Set "+def.toDebugString()+" power cost to "+def.runtimePowerSettings.GetPowerConsumption());
        	}
        }
        
        public static void initializeThresherSettings(ThresherDefinition def) {
        	if (def.displayName == "Thresher") { //mk1
        		def.runtimePowerSettings.kWPowerConsumption = def.runtimePowerSettings.kWPowerConsumption.multiplyBy(0.375); //from 400 to 150
        		TTUtil.log("Set "+def.toDebugString()+" power cost to "+def.runtimePowerSettings.GetPowerConsumption());
        	}
        	else if (def.displayName.EndsWith("MKII", StringComparison.InvariantCultureIgnoreCase)) {
        		def.runtimePowerSettings.kWPowerConsumption = def.runtimePowerSettings.kWPowerConsumption.multiplyBy(0.625); //from 4000 to 2500
        		TTUtil.log("Set "+def.toDebugString()+" power cost to "+def.runtimePowerSettings.GetPowerConsumption());
        	}
        }
        
        //public static float globalThresherSeedFactor = 1;
		
		internal static SeedYieldTech.TechLevel getHighestSeedTechLevelUnlocked() {
			for (int i = seedYieldBoostTechs.Length-1; i >= 0; i--) {
				if (seedYieldBoostTechs[i].isUnlocked)
					return techLevels[i];
			}
			return null;
		}
        
        public static int getThresherItemYield(int amt, int index, ref ThresherInstance machine) {
        	SchematicsRecipeData recipe = machine.curSchematic;
        	if (amt == 1 && TTUtil.isSeed(recipe.outputTypes[index])) {
        		BTConfig.ConfigEntries entry = machine._myDef.name.EndsWith("_2", StringComparison.InvariantCultureIgnoreCase) ? BTConfig.ConfigEntries.THRESHER2BONUSBASE : BTConfig.ConfigEntries.THRESHER1BONUSBASE;
        		float chance = config.getFloat(entry);
        		
        		chance += seedYieldCoreTech.currentEffect;
        		
				SeedYieldTech.TechLevel lvl = getHighestSeedTechLevelUnlocked();
				//TTUtil.log("Seed yield available tech level is "+lvl);
				if (lvl != null)
					chance += lvl.yieldBoost;
			
        		//chance *= globalThresherSeedFactor;
        		//TTUtil.log("Thresher "+machine._myDef.name+" has a "+(chance*100).ToString("0.0")+"% chance of making an extra seed (from "+amt+")");
        		if (UnityEngine.Random.Range(0F, 1F) < chance) {
        			amt += 1;
        			//TTUtil.log("Success");
        		}
        	}
        	return amt;
        }
        
        public static void restoreAllFlora() {
        	/*
        	PropManager props = PropManager.instance;
        	//Unity.Collections.NativeList<PropState> li = props.propStates;
        	for (int i = 0; i < props.propStates.Length; i++) {
        		PropState state = props.propStates[i];
        		state.currentHealth = (byte)127;
        		state.isAlive = true;
        	}
        	for (int i = 0; i < props.propIsAlive.Length; i++) {
        		props.propIsAlive[i] = true;
        	}*/
        	SaveState.instance.voxeland.destroyedProps.RemoveAll(inst => {
				return false;//inst.typeID == 0;
			});
        }
        
        //private static bool changedPTResources;
        
        //private static bool appliedPlanterTerminalReplace;
        
        public static void onProdTerminalResourceUpdate(ref ProductionTerminalDefinition.ResourceRequirementData[] arr, GatedDoorConfiguration cfg) {
        	//if (appliedPlanterTerminalReplace)
        	//	return;
        	if (cfg == null) {
        		return;
        	}
        	TTUtil.log("onProdTerminalResourceUpdate for "+cfg.name);
        	if (cfg.reqTypes == null) {
        		TTUtil.log("Null reqs!!");
        		return;
        	}
        	//terminal._myDef.tierChanges.ToList().ForEach(t => t.resourcesRequired.ToList().ForEach(replacePlanter2With3));
        	//terminal.resourcesRequired.ToList().ForEach(replacePlanter2With3);
        	for (int i = 0; i < cfg.reqTypes.Length; i++) {
        		ResourceInfo res = cfg.reqTypes[i];
        		TTUtil.log("Checking "+res.toDebugString());
        		bool flag = false;
        		if (res != null && res.uniqueId == t2Planter.uniqueId) {
        			CustomItem res2 = t3Planter.item;
        			if (res2 == null) {
        				throw new Exception("No such item to add to production terminal to replace '"+res.toDebugString()+"'!");
        			}
        			if (res2.item.rawSprite == null) {
        				throw new Exception("Item "+res2.item.toDebugString()+" not valid for production terminal, has no sprite!");
        			}
        			cfg.reqTypes[i] = res2.item;
        			TTUtil.log("Replaced planter 2 with 3 in PT "+cfg.name);
        			//appliedPlanterTerminalReplace = true;
        			flag = true;
        			break;
        		}
        	}
        	TTUtil.log("Finished replacing production terminal resources "+cfg.reqTypes.Select(res => res.toDebugString()).toDebugString()+" => "+arr.Select(res => res.resType.toDebugString()).toDebugString());
        }
        /*
        private static void replacePlanter2With3(ProductionTerminalDefinition.ResourceRequirementData res) {
        	TTUtil.log("res entry "+res.resType.toDebugString()+"x"+res.quantity);
        	if (res.resType != null && res.resType.uniqueId == t2Planter.uniqueId) {
        		res.resType = t3Planter.item;
        		changedPTResources = true;
        	}
        }*/

	}
}
