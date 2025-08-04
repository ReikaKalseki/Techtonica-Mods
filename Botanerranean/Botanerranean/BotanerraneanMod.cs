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
        
        private static CustomTech planterCoreTech;
        private static CustomTech seedYieldCoreTech;
        private static CustomTech seedYieldTech1;
        private static CustomTech seedYieldTech2;
        private static CustomTech seedYieldTech3;
        private static CustomTech t3PlanterTech;
        private static CustomMachine<PlanterInstance, PlanterDefinition> t3Planter;
        private static ResourceInfo t2Planter;
        
        public static SchematicsRecipeData planter2Recipe;
        public static SchematicsRecipeData planter3Recipe;
        
        public static readonly Assembly modDLL = Assembly.GetExecutingAssembly();

        public BotanerraneanMod() : base() {
            instance = this;
            TTUtil.log("Constructed BT mod object");
        }

        public void Awake() {
            TTUtil.log("Begin Initializing Botanerranean");
            try {
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
				t3PlanterTech.requiredTier = TechTreeState.ResearchTier.Tier0;
				t3PlanterTech.treePosition = 0;
				
				t3Planter.register();
                
				planterCoreTech = new CustomTech(Unlock.TechCategory.Science, ResearchCoreDefinition.CoreType.Blue, 100);
				planterCoreTech.displayName = "Core Boost (Planter)";
				planterCoreTech.description = "Increases speed of all Planters by 5% per Core Cluster.";
				planterCoreTech.requiredTier = TechTreeState.ResearchTier.Tier1;
				planterCoreTech.treePosition = 0;
				planterCoreTech.finalFixes = () => {
		        	TTUtil.log("Adjusting "+planterCoreTech.displayName);
		        	Unlock tu = planterCoreTech.unlock;
		        	tu.requiredTier = TTUtil.getUnlock("Core Boost (Smelting)").requiredTier;
		        	tu.treePosition = TTUtil.getUnlock("Core Boost (Threshing)").treePosition;
		        	tu.sprite = TTUtil.getUnlock("Planter").sprite;
				};
				planterCoreTech.register();
				
				seedYieldTech1 = new CustomTech(Unlock.TechCategory.Synthesis, ResearchCoreDefinition.CoreType.Blue, 80);
				seedYieldTech1.displayName = "Seed Yield I";
				seedYieldTech1.description = "Increases seed yield from threshing by 5%.";
				seedYieldTech1.requiredTier = TTUtil.getTierAtStation(TTUtil.ProductionTerminal.VICTOR, 1);
				seedYieldTech1.treePosition = 0;
				seedYieldTech1.finalFixes = () => {
		        	TTUtil.log("Adjusting "+seedYieldTech1.displayName);
		        	Unlock tu = seedYieldTech1.unlock;
		        	tu.treePosition = TTUtil.getUnlock(EMU.Names.Resources.AssemblerMKII).treePosition;
		        	tu.sprite = EMU.Resources.GetResourceInfoByName(EMU.Names.Resources.KindlevineSeed).sprite;
				};
				seedYieldTech1.register();
				
				seedYieldTech2 = new CustomTech(Unlock.TechCategory.Synthesis, ResearchCoreDefinition.CoreType.Blue, 160);
				seedYieldTech2.displayName = "Seed Yield II";
				seedYieldTech2.description = "Increases seed yield from threshing by 10%.";
				seedYieldTech2.requiredTier = TTUtil.getTierAtStation(TTUtil.ProductionTerminal.VICTOR, 5);
				seedYieldTech2.treePosition = 0;
				seedYieldTech2.finalFixes = () => {
		        	TTUtil.log("Adjusting "+seedYieldTech2.displayName);
		        	Unlock tu = seedYieldTech2.unlock;
		        	tu.treePosition = TTUtil.getUnlock(EMU.Names.Resources.ThresherMKII).treePosition;
		        	tu.sprite = EMU.Resources.GetResourceInfoByName(EMU.Names.Resources.KindlevineSeed).sprite;
		        	tu.dependency1 = seedYieldTech1.unlock;
		        	tu.dependencies = new List<Unlock>{tu.dependency1};
				};
				seedYieldTech2.register();
				
				seedYieldTech3 = new CustomTech(Unlock.TechCategory.Synthesis, ResearchCoreDefinition.CoreType.Blue, 160);
				seedYieldTech3.displayName = "Seed Yield III";
				seedYieldTech3.description = "Increases seed yield from threshing by 25%.";
				seedYieldTech3.requiredTier = TTUtil.getTierAtStation(TTUtil.ProductionTerminal.XRAY, 4);
				seedYieldTech3.treePosition = 0;
				seedYieldTech3.finalFixes = () => {
		        	TTUtil.log("Adjusting "+seedYieldTech3.displayName);
		        	Unlock tu = seedYieldTech3.unlock;
		        	tu.treePosition = TTUtil.getUnlock(EMU.Names.Resources.CarbonPowder).treePosition;
		        	tu.sprite = EMU.Resources.GetResourceInfoByName(EMU.Names.Resources.KindlevineSeed).sprite;
		        	tu.dependency1 = seedYieldTech2.unlock;
		        	tu.dependencies = new List<Unlock>{tu.dependency2};
				};
				seedYieldTech3.register();
				
				seedYieldCoreTech = new CustomTech(Unlock.TechCategory.Science, ResearchCoreDefinition.CoreType.Green, 100);
				seedYieldCoreTech.displayName = "Core Boost (Seed Threshing)";
				seedYieldCoreTech.description = "Increases seed yield from threshing by 5% per Core Cluster.";
				seedYieldCoreTech.requiredTier = TechTreeState.ResearchTier.Tier8;
				seedYieldCoreTech.treePosition = 0;
				seedYieldCoreTech.finalFixes = () => {
		        	TTUtil.log("Adjusting "+seedYieldCoreTech.displayName);
		        	Unlock tu = seedYieldCoreTech.unlock;
		        	tu.requiredTier = TTUtil.getUnlock("Core Boost (Smelting)").requiredTier;
		        	tu.treePosition = TTUtil.getUnlock("Core Boost (Threshing)").treePosition-20;
		        	tu.sprite = EMU.Resources.GetResourceInfoByName(EMU.Names.Resources.KindlevineSeed).sprite;
				};
				seedYieldCoreTech.register();

				EMU.Events.GameDefinesLoaded += onDefinesLoaded;
				EMU.Events.TechTreeStateLoaded += onTechsLoaded;
				
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
			t3.description = t3Planter.item.displayName;
			
			makeSeedToFuel(EMU.Names.Resources.KindlevineSeed, 1.5F);
			makeSeedToFuel(EMU.Names.Resources.ShiverthornSeed, 0.5F);
			makeSeedToFuel(EMU.Names.Resources.SesamiteSeed);
		}
        
        private static void onRecipesLoaded() {
        	int id2 = EMU.Resources.GetResourceIDByName(EMU.Names.Resources.PlanterMKII);
        	int id3 = EMU.Resources.GetResourceIDByName(t3Planter.name);
        	planter2Recipe = GameDefines.instance.GetSchematicsRecipeDataById(TTUtil.getRecipeID(id2));
        	planter3Recipe = GameDefines.instance.GetSchematicsRecipeDataById(TTUtil.getRecipeID(id3));
        	
        	if (planter2Recipe == null) {
        		TTUtil.log("No planter 2 recipe (id="+id2+")");
        		return;
        	}
        	if (planter3Recipe == null) {
        		TTUtil.log("No planter 3 recipe (id="+id3+")");
        		return;
        	}
        	
        	planter3Recipe.ingTypes = new ResourceInfo[planter2Recipe.ingTypes.Length];
        	for (int i = 0; i < planter3Recipe.ingTypes.Length; i++) {
        		planter3Recipe.ingTypes[i] = planter2Recipe.ingTypes[i];
        		if (planter3Recipe.ingTypes[i].uniqueId == EMU.Resources.GetResourceIDByName(EMU.Names.Resources.Planter))
        			planter3Recipe.ingTypes[i] = EMU.Resources.GetResourceInfoByName(EMU.Names.Resources.PlanterMKII);
        	}
        	planter3Recipe.ingQuantities = new int[planter2Recipe.ingQuantities.Length];
        	planter3Recipe.duration = planter2Recipe.duration*3;
        	Array.Copy(planter2Recipe.ingQuantities, planter3Recipe.ingQuantities, planter3Recipe.ingQuantities.Length);
        	TTUtil.compileRecipe(planter3Recipe);
        	
        	TTUtil.setIngredients(planter2Recipe, EMU.Names.Resources.Planter, 2, EMU.Names.Resources.SteelFrame, 1, EMU.Names.Resources.AdvancedCircuit, 15);
        }
        
        private static void onTechsLoaded() {
        	TTUtil.setUnlockRecipes(EMU.Names.Resources.PlanterMKII, planter2Recipe);
        	TTUtil.setUnlockRecipes(t3Planter.name, planter3Recipe);
        }
        
        private static void makeSeedToFuel(string name, float relativeValue = 1) {
        	ResourceInfo template = EMU.Resources.GetResourceInfoByName(EMU.Names.Resources.PlantmatterFiber);
        	ResourceInfo res = EMU.Resources.GetResourceInfoByName(name);
        	res.fuelAmount = template.fuelAmount*1.5F*relativeValue;
        	TTUtil.log(name+" now has fuel value "+res.fuelAmount.ToString("0.0"));
        	
        	//TODO add plantmatter crafting
        }
        
        public static bool tickPlantSlot(ref PlanterInstance.PlantSlot slot, float increment, ref PlanterInstance owner) { //no need to handle T2, as it is already doubled
			return slot.UpdateGrowth(increment*getPlanterSpeed(ref owner));
		}
        
        public static float getPlanterGrowthTimeForUI(float val, ref PlanterInstance pi) {
        	return val/getPlanterSpeed(ref pi);
        }
        
        private static float getPlanterSpeed(ref PlanterInstance pi) {
        	float val = globalPlanterSpeedFactor;
        	if (planterCoreTech.isUnlocked && TechTreeState.instance.freeCores > 0) {
        		val *= 1+TTUtil.getCoreClusterCount()*0.05F;
			}
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
        		def.runtimePowerSettings.kWPowerConsumption = def.runtimePowerSettings.kWPowerConsumption.multiplyBy(0.375); //from 4000 to 1500
        		TTUtil.log("Set "+def.toDebugString()+" power cost to "+def.runtimePowerSettings.GetPowerConsumption());
        	}
        }
        
        public static float globalThresherSeedFactor = 1;
        
        public static int getThresherItemYield(int amt, int index, ref ThresherInstance machine) {
        	SchematicsRecipeData recipe = machine.curSchematic;
        	if (amt == 1 && TTUtil.isSeed(recipe.outputTypes[index])) {
        		float chance = machine._myDef.name.EndsWith("_2", StringComparison.InvariantCultureIgnoreCase) ? 0.025F : 0.01F; //base 1% or 2.5% for thresher II
        		if (TechTreeState.instance.freeCores > 0 && seedYieldCoreTech.isUnlocked)
        			chance += TTUtil.getCoreClusterCount()*0.05F; //5% per core cluster
        		if (seedYieldTech3.isUnlocked)
        			chance += 0.25F;
        		else if (seedYieldTech2.isUnlocked)
        			chance += 0.1F;
        		else if (seedYieldTech1.isUnlocked)
        			chance += 0.05F;
        		chance *= globalThresherSeedFactor;
        		TTUtil.log("Thresher "+machine._myDef.name+" has a "+(chance*100).ToString("0.0")+"% chance of making an extra seed (from "+amt+")");
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
        
        private static bool changedPTResources;
        
        public static void onProdTerminalResourceUpdate(ref ProductionTerminalInstance terminal) {
        	changedPTResources = false;
        	terminal._myDef.tierChanges.ToList().ForEach(t => t.resourcesRequired.ToList().ForEach(replacePlanter2With3));
        	terminal.resourcesRequired.ToList().ForEach(replacePlanter2With3);
        	if (changedPTResources)
        		TTUtil.log("Replaced planter 2 with 3 in PT "+terminal._myDef.displayName);
        }
        
        private static void replacePlanter2With3(ProductionTerminalDefinition.ResourceRequirementData res) {
        	if (res.resType != null && res.resType.uniqueId == t2Planter.uniqueId) {
        		res.resType = t3Planter.item;
        		changedPTResources = true;
        	}
        }

	}
}
