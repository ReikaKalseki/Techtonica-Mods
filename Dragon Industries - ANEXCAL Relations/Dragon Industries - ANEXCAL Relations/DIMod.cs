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

namespace ReikaKalseki.DIANEXCAL {

    [BepInPlugin("ReikaKalseki.DIMod", "Dragon Industries ANEXCAL Relations", "1.0.0")]
    public class DIMod : BaseUnityPlugin {

        public static DIMod instance;
        
        public static event Action onRecipesLoaded;

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

				EMU.Events.GameDefinesLoaded += onDefinesLoaded;
			}
			catch (Exception e) {
                TTUtil.log("Failed to load DI: "+e, TTUtil.diDLL);
            }
            TTUtil.log("Finished Initializing Dragon Industries", TTUtil.diDLL);
        }
        
		private static void onDefinesLoaded() {
        	/*
        	foreach (FieldInfo item in typeof(EMU.Names.Resources).GetFields()) {
        		string id = (string)item.GetValue(null);
        		ResourceInfo ri = EMU.Resources.GetResourceInfoByName(id);
				RenderUtil.dumpTexture(TTUtil.diDLL, id, ri.sprite.texture);
			}*/
        	
        }
        
		public static void onRecipeDataLoaded() {
        	TTUtil.log("Recipe data loaded, running hooks", TTUtil.diDLL);
        	
        	TTUtil.buildRecipeCache();
        	
        	doubleRecipe(TTUtil.getSmelterRecipe(EMU.Names.Resources.IronOre));
        	doubleRecipe(TTUtil.getSmelterRecipe(EMU.Names.Resources.CopperOre));
        	doubleRecipe(TTUtil.getSmelterRecipe(EMU.Names.Resources.GoldOre));
        	
        	doubleRecipe(GameDefines.instance.GetThreshingRecipeForResource(EMU.Resources.GetResourceIDByName(EMU.Names.Resources.IronOre)));
        	doubleRecipe(GameDefines.instance.GetThreshingRecipeForResource(EMU.Resources.GetResourceIDByName(EMU.Names.Resources.CopperOre)));
        	doubleRecipe(GameDefines.instance.GetThreshingRecipeForResource(EMU.Resources.GetResourceIDByName(EMU.Names.Resources.AtlantumOre)));
        	doubleRecipe(GameDefines.instance.GetThreshingRecipeForResource(EMU.Resources.GetResourceIDByName(EMU.Names.Resources.GoldOre)));
        	
        	if (onRecipesLoaded != null) {
        		try {
        			onRecipesLoaded.Invoke();
        		}
        		catch (Exception ex) {
        			TTUtil.log("Exception caught when handling recipe loading: "+ex, TTUtil.diDLL);
        		}
        	}
        }
        
		private static void doubleRecipe(SchematicsRecipeData rec) {
        	if (rec == null)
        		return;
        	TTUtil.log("Doubling yield for recipe "+rec.toDebugString(), TTUtil.diDLL);
			for (int i = 0; i < rec.outputQuantities.Length; i++) {
        		rec.outputQuantities[i] = rec.outputQuantities[i]*2;
			}
		}
        
        public static void initializeAccumulatorSettings(AccumulatorDefinition def) {
        	def.runtimeSettings.energyCapacity *= 5/2; //2.5x
        }
        
        public static bool protectionZonesActive = true;
        
        public static bool interceptProtection(bool prot) {
        	return prot && protectionZonesActive;
        }

	}
}
