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
        private static CustomTech t3PlanterTech;
        private static CustomMachine<PlanterInstance, PlanterDefinition> t3Planter;
        
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
				
				t3Planter = new CustomMachine<PlanterInstance, PlanterDefinition>("Planter MKIII", "Grows plants at quadruple the speed of the MK1 planter.", "PlanterT3", EMU.Names.Unlocks.PlanterMKII, EMU.Names.Resources.PlanterMKII);
				t3Planter.register();
				
				NewRecipeDetails t2Recipe = t3Planter.addRecipe("ReikaKalseki.PlanterT3");
				TTUtil.setIngredients(t2Recipe, EMU.Names.Resources.IronComponents, 18, EMU.Names.Resources.Dirt, 12); //TODO temp recipe
                
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
                
				t3PlanterTech = t3Planter.createUnlock(Unlock.TechCategory.Synthesis, ResearchCoreDefinition.CoreType.Blue, 100);
				t3PlanterTech.requiredTier = TechTreeState.ResearchTier.Tier0;
				t3PlanterTech.treePosition = 0;
				t3PlanterTech.finalFixes = () => {
					t3PlanterTech.unlock.sprite = TTUtil.getUnlock(EMU.Names.Resources.PlanterMKII).sprite;					
				};
				t3PlanterTech.register();

				EMU.Events.GameDefinesLoaded += onDefinesLoaded;
				
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
			t3.coresNeeded = new List<Unlock.RequiredCores>(pu.coresNeeded);
			t3.dependencies = new List<Unlock>(pu.dependencies);
			t3.dependencies.Add(pu);
			t3.dependency1 = pu.dependency1;
			t3.dependency2 = pu.dependency2;
			t3.numScansNeeded = pu.numScansNeeded;
			
			TTUtil.log("Moving PlanterMKII tech");				
			Unlock thresh = TTUtil.getUnlock(EMU.Names.Resources.ThresherMKII);
			pu.requiredTier = TTUtil.getTierAfter(thresh.requiredTier);
			pu.treePosition = thresh.treePosition;
			pu.coresNeeded.Clear();
			pu.coresNeeded.Add(new Unlock.RequiredCores{type = ResearchCoreDefinition.CoreType.Blue, number = 50});
			pu.dependencies = new List<Unlock>{thresh};
			pu.dependency1 = thresh.dependency1;
			pu.dependency2 = thresh.dependency2;
			
			makeSeedToFuel(EMU.Names.Resources.KindlevineSeed, 1.5F);
			makeSeedToFuel(EMU.Names.Resources.ShiverthornSeed, 0.5F);
			makeSeedToFuel(EMU.Names.Resources.SesamiteSeed);
		}
        
        private static void onRecipesLoaded() {
        	//SchematicsRecipeData rec = GameDefines.instance.GetThreshingRecipeForResource(EMU.Resources.GetResourceIDByName(EMU.Names.Resources.ShiverthornBuds));
        }
        
        private static void makeSeedToFuel(string name, float relativeValue = 1) {
        	ResourceInfo template = EMU.Resources.GetResourceInfoByName(EMU.Names.Resources.PlantmatterFiber);
        	ResourceInfo res = EMU.Resources.GetResourceInfoByName(name);
        	res.fuelAmount = template.fuelAmount*1.5F*relativeValue;
        	TTUtil.log(name+" now has fuel value "+res.fuelAmount.ToString("0.0"));
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
        		def.runtimePowerSettings.kWPowerConsumption = def.runtimePowerSettings.kWPowerConsumption*750/350; //from 350kW/slot to 750
        		TTUtil.log("Set "+def.toDebugString()+" power cost to "+def.runtimePowerSettings.GetPowerConsumption());
        	}
        	else if (def.displayName.EndsWith("MKII", StringComparison.InvariantCultureIgnoreCase)) {
        		def.runtimePowerSettings.kWPowerConsumption = def.runtimePowerSettings.kWPowerConsumption*5/7; //from 350kW/slot to 250
        		TTUtil.log("Set "+def.toDebugString()+" power cost to "+def.runtimePowerSettings.GetPowerConsumption());
        	}
        	else if (def.displayName == "Planter") {
        		def.runtimePowerSettings.kWPowerConsumption = def.runtimePowerSettings.kWPowerConsumption*2/5; //from 50kW/slot to 20
        		TTUtil.log("Set "+def.toDebugString()+" power cost to "+def.runtimePowerSettings.GetPowerConsumption());
        	}
        }
        
        public static void initializeThresherSettings(ThresherDefinition def) {
        	if (def.displayName == "Thresher") { //mk1
        		def.runtimePowerSettings.kWPowerConsumption = def.runtimePowerSettings.kWPowerConsumption*3/8; //from 400 to 150
        		TTUtil.log("Set "+def.toDebugString()+" power cost to "+def.runtimePowerSettings.GetPowerConsumption());
        	}
        	else if (def.displayName.EndsWith("MKII", StringComparison.InvariantCultureIgnoreCase)) {
        		def.runtimePowerSettings.kWPowerConsumption = def.runtimePowerSettings.kWPowerConsumption*3/8; //from 4000 to 1500
        		TTUtil.log("Set "+def.toDebugString()+" power cost to "+def.runtimePowerSettings.GetPowerConsumption());
        	}
        }
        
        public static int getThresherItemYield(int amt, int index, ref ThresherInstance machine) {
        	SchematicsRecipeData recipe = machine.curSchematic;
        	if (amt == 1 && seedYieldCoreTech.isUnlocked && recipe.outputTypes[index].name.EndsWith(" Seed", StringComparison.InvariantCultureIgnoreCase)) {
        		float chance = machine._myDef.name.EndsWith("MKII", StringComparison.InvariantCultureIgnoreCase) ? 0.025F : 0.01F; //base 2% or 5% for thresher II
        		if (TechTreeState.instance.freeCores > 0)
        		 chance += TTUtil.getCoreClusterCount()*0.05F; //5% per core cluster
        		if (UnityEngine.Random.Range(0F, 1F) < chance)
        			amt += 1;
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

	}
}
