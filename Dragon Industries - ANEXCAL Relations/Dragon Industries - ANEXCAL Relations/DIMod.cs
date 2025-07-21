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

    [BepInPlugin("ReikaKalseki.DIMod", "Dragon Industries ANEXCAL Relations", "1.0.0")]
    public class DIMod : BaseUnityPlugin {

        public static DIMod instance;
        
        private static CustomTech planterCoreTech;
        private static CustomTech t2PlanterTech;
        private static CustomMachine<PlanterInstance, PlanterDefinition> t2Planter;

        public DIMod() : base() {
            instance = this;
            TTUtil.log("Constructed DI object", TTUtil.diDLL);
        }

        public void Awake() {
            TTUtil.log("Begin Initializing Dragon Industries", TTUtil.diDLL);
            try {
                Harmony harmony = new Harmony("Dragon Industries");
                Harmony.DEBUG = true;
                FileLog.logPath = Path.Combine(Path.GetDirectoryName(TTUtil.diDLL.Location), "harmony-log_"+Path.GetFileName(Assembly.GetExecutingAssembly().Location)+".txt");
                FileLog.Log("Ran mod register, started harmony (harmony log)");
                TTUtil.log("Ran mod register, started harmony", TTUtil.diDLL);
                try {
                    harmony.PatchAll(TTUtil.diDLL);
                }
                catch (Exception ex) {
                    FileLog.Log("Caught exception when running patchers!");
					FileLog.Log(ex.Message);
					FileLog.Log(ex.StackTrace);
					FileLog.Log(ex.ToString());
				}
				
				t2Planter = new CustomMachine<PlanterInstance, PlanterDefinition>("Planter MKII", "Grows plants at double-speed of the base planter.", "PlanterT2", EMU.Names.Unlocks.PlanterMKII, EMU.Names.Resources.PlanterMKII);
				t2Planter.register();
				
				NewRecipeDetails t2Recipe = t2Planter.addRecipe("ReikaKalseki.PlanterT2");
				TTUtil.setIngredients(t2Recipe, EMU.Names.Resources.IronComponents, 18, EMU.Names.Resources.Dirt, 12); //TODO temp recipe
                
				planterCoreTech = new CustomTech(Unlock.TechCategory.Science, ResearchCoreDefinition.CoreType.Blue, 100);
				planterCoreTech.displayName = "Core Boost (Planter)";
				planterCoreTech.description = "Increases speed of all Planters by 5% per Core Cluster.";
				planterCoreTech.requiredTier = TechTreeState.ResearchTier.Tier1;
				planterCoreTech.treePosition = 0;
				planterCoreTech.finalFixes = () => {
		        	TTUtil.log("Adjusting "+planterCoreTech.displayName, TTUtil.diDLL);
					EMU.Unlocks.UpdateUnlockTier(planterCoreTech.displayName, TTUtil.getUnlock("Core Boost (Smelting)").requiredTier, true);
					EMU.Unlocks.UpdateUnlockTreePosition(planterCoreTech.displayName, TTUtil.getUnlock("Core Boost (Threshing)").treePosition-0.25F, true);
					EMU.Unlocks.UpdateUnlockSprite(planterCoreTech.displayName, TTUtil.getUnlock("Planter").sprite, true);
				};
				planterCoreTech.register();
                
				t2PlanterTech = t2Planter.createUnlock(Unlock.TechCategory.Synthesis, ResearchCoreDefinition.CoreType.Blue, 100);
				t2PlanterTech.requiredTier = TechTreeState.ResearchTier.Tier0;
				t2PlanterTech.treePosition = 0;
				t2PlanterTech.finalFixes = () => {
		        	TTUtil.log("Adjusting "+t2PlanterTech.displayName, TTUtil.diDLL);
					EMU.Unlocks.UpdateUnlockSprite(t2PlanterTech.displayName, TTUtil.getUnlock("Planter").sprite, true);					
					Unlock thresh = TTUtil.getUnlock("Thresher MKII");
					Unlock pu = TTUtil.getUnlock(t2Planter.name);
					pu.requiredTier = thresh.requiredTier;
					pu.treePosition = thresh.treePosition-0.25F;
				};
				t2PlanterTech.register();

			}
			catch (Exception e) {
                TTUtil.log("Failed to load DI: "+e, TTUtil.diDLL);
            }
            TTUtil.log("Finished Initializing Dragon Industries", TTUtil.diDLL);
        }
        
        public static bool tickPlantSlot(ref PlanterInstance.PlantSlot slot, float increment, ref PlanterInstance owner) {
        	if (TechTreeState.instance == null)
        		return false;
        	if (TechTreeState.instance.freeCores > 0) {
        		increment *= 1+TTUtil.getCoreClusterCount()*0.05F;
        	}
        	if (t2Planter != null && t2Planter.isThisMachine(owner)) {
        		increment *= 2;
        	}
        	return slot.UpdateGrowth(increment);
        }

	}
}
