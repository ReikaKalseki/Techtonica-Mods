using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Linq;

using BepInEx;
using BepInEx.Configuration;

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
		public static event Action onDefinesLoadedFirstTime;
		public static event Action onTechsLoadedFirstTime;
		
		public static event Action<int> onTechActivatedEvent;
		public static event Action<int> onTechDeactivatedEvent;
        
		private static bool definesLoadedFired;
		private static bool techsLoadedFired;

		public DIMod()
			: base() {
			instance = this;
			TTUtil.log("Constructed DI object", TTUtil.diDLL);
		}

		public void Awake() {
			TTUtil.log("Begin Initializing Dragon Industries", TTUtil.diDLL);
			try {
				Harmony harmony = new Harmony("Dragon Industries");
				Harmony.DEBUG = true;
				FileLog.logPath = Path.Combine(Path.GetDirectoryName(TTUtil.diDLL.Location), "harmony-log_" + Path.GetFileName(Assembly.GetExecutingAssembly().Location) + ".txt");
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
				EMU.Events.TechTreeStateLoaded += onTechsLoaded;
				EMU.Events.GameLoaded += () => {
					resyncResearchCoreUse(u => false);
				};
			}
			catch (Exception e) {
				TTUtil.log("Failed to load DI: " + e, TTUtil.diDLL);
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
			if (!definesLoadedFired) {
				if (onDefinesLoadedFirstTime != null) {
					try {
						onDefinesLoadedFirstTime.Invoke();
					}
					catch (Exception ex) {
						TTUtil.log("Exception caught when handling one-time defines load: " + ex, TTUtil.diDLL);
					}
				}
				definesLoadedFired = true;
			}
		}
        
		private static void onTechsLoaded() {
			if (!techsLoadedFired) {
				if (onTechsLoadedFirstTime != null) {
					try {
						onTechsLoadedFirstTime.Invoke();
					}
					catch (Exception ex) {
						TTUtil.log("Exception caught when handling one-time techs load: " + ex, TTUtil.diDLL);
					}
				}
				techsLoadedFired = true;
			}
		}
        
		public static void onRecipeDataLoaded() {
			TTUtil.log("Recipe data loaded, running hooks", TTUtil.diDLL);
        	
			TTUtil.buildRecipeCache();
        	/*
			doubleRecipe(TTUtil.getSmelterRecipe(EMU.Names.Resources.IronOre));
			doubleRecipe(TTUtil.getSmelterRecipe(EMU.Names.Resources.CopperOre));
			doubleRecipe(TTUtil.getSmelterRecipe(EMU.Names.Resources.GoldOre));
        	
			doubleRecipe(GameDefines.instance.GetThreshingRecipeForResource(EMU.Resources.GetResourceIDByName(EMU.Names.Resources.IronOre)));
			doubleRecipe(GameDefines.instance.GetThreshingRecipeForResource(EMU.Resources.GetResourceIDByName(EMU.Names.Resources.CopperOre)));
			doubleRecipe(GameDefines.instance.GetThreshingRecipeForResource(EMU.Resources.GetResourceIDByName(EMU.Names.Resources.AtlantumOre)));
        	*/
			if (onRecipesLoaded != null) {
				try {
					onRecipesLoaded.Invoke();
				}
				catch (Exception ex) {
					TTUtil.log("Exception caught when handling recipe loading: " + ex, TTUtil.diDLL);
				}
			}
		}
        
		private static void doubleRecipe(SchematicsRecipeData rec) {
			if (rec == null)
				return;
			TTUtil.log("Doubling yield for recipe " + rec.toDebugString(), TTUtil.diDLL);
			for (int i = 0; i < rec.outputQuantities.Length; i++) {
				rec.outputQuantities[i] = rec.outputQuantities[i] * 2;
			}
		}
        
		public static void initializeAccumulatorSettings(AccumulatorDefinition def) {
			def.runtimeSettings.energyCapacity *= 5 / 2; //2.5x
		}
        
		public static bool protectionZonesActive = true;
        
		public static bool interceptProtection(bool prot) {
			return prot && protectionZonesActive;
		}
        
		public static void setMassScanRange(int r = 0) {
			Type t = InstructionHandlers.getTypeBySimpleName("MassScan.MassScan");
			if (t == null) {
				TTUtil.log("No mass scan DLL found", TTUtil.diDLL);
			}
			else {
				FieldInfo fi = t.GetField("ScanRange", BindingFlags.NonPublic | BindingFlags.Static);
				ConfigEntry<int> c = (ConfigEntry<int>)fi.GetValue(null);
				TTUtil.log("Changing mass scan range from " + c.Value + " to " + r);
				c.Value = r;
			}
		}
		
		public static void onTechActivated(int id) {
			if (onTechActivatedEvent != null) {
				try {
					onTechActivatedEvent.Invoke(id);
				}
				catch (Exception ex) {
					TTUtil.log("Exception caught when handling tech activation: " + ex, TTUtil.diDLL);
				}
			}
		}
		
		public static void onTechDeactivated(int id) {
			if (onTechDeactivatedEvent != null) {
				try {
					onTechDeactivatedEvent.Invoke(id);
				}
				catch (Exception ex) {
					TTUtil.log("Exception caught when handling tech deactivation: " + ex, TTUtil.diDLL);
				}
			}
		}
        
		public static void setupUpgradeCosts(ref ProductionTerminalDefinition.ResourceRequirementData[] arr, ref float nrg, ref Inventory inv, GatedDoorConfiguration cfg, bool valid, EGameModeSettingType resMult = EGameModeSettingType.None, EGameModeSettingType nrgMult = EGameModeSettingType.None) {
			if (cfg == null)
				return;
			TTUtil.log("Intercepting upgrade cost setup for " + cfg.name, TTUtil.diDLL);
			TTUtil.log("PRE: " + cfg.reqTypes.Select(res => res.toDebugString()).toDebugString() + " => " + arr.Select(res => res.resType.toDebugString()).toDebugString(), TTUtil.diDLL);
			RepairableElevatorInstance.SetResourceRequirements(ref arr, ref nrg, ref inv, cfg, valid, resMult, nrgMult);
			TTUtil.log("POST: " + cfg.reqTypes.Select(res => res.toDebugString()).toDebugString() + " => " + arr.Select(res => res.resType.toDebugString()).toDebugString(), TTUtil.diDLL);
			bool flag3 = false;
			for (int i = 0; i < cfg.reqTypes.Length; i++) {
				if (true)
					continue;
				if (i < arr.Length && cfg.reqTypes[i] != null && (arr[i].resType == null || arr[i].resType.uniqueId != cfg.reqTypes[i].uniqueId || arr[i].quantity == 0)) {
					arr[i].resType = cfg.reqTypes[i];
					arr[i].quantity = cfg.reqQuantities[i];
					flag3 = true;
					TTUtil.log("Fixing null slot " + i + ": " + arr[i].resType.toDebugString(), TTUtil.diDLL);
				}
			}
			//List<ProductionTerminalDefinition.ResourceRequirementData> li = arr.ToList();
			//List<ResourceInfo> li2 = cfg.reqTypes.ToList();
			bool flag = false;
			bool flag2 = false;/*
        	while (li[li.Count-1].resType == null && li.Count > 0) {
        		TTUtil.log("Removing null from arr at "+(li.Count-1), TTUtil.diDLL);
        		li.RemoveAt(li.Count-1);
        		flag = true;
        	}
        	while (li2[li2.Count-1] == null && li2.Count > 0) {
        		TTUtil.log("Removing null from cfg at "+(li2.Count-1), TTUtil.diDLL);
        		li2.RemoveAt(li.Count-1);
        		flag2 = true;
        	}
        	if (flag2) {
        		cfg.reqTypes = li2.ToArray();
        		TTUtil.log("FIXED: "+cfg.reqTypes.Select(res => res.toDebugString()).toDebugString(), TTUtil.diDLL);
        	}
        	if (flag) {
        		arr = li.ToArray();
        		TTUtil.log("FIXED: "+arr.Select(res => res.resType.toDebugString()).toDebugString(), TTUtil.diDLL);
        	}*/
			if (flag || flag2 || flag3) {
				TTUtil.log("FIXED: " + cfg.reqTypes.Select(res => res.toDebugString()).toDebugString() + " => " + arr.Select(res => res.resType.toDebugString()).toDebugString(), TTUtil.diDLL);
			}
		}
		
		public static void resyncResearchCoreUse() {
			resyncResearchCoreUse(u => true);
		}
		
		public static void resyncResearchCoreUse(ResearchCoreDefinition.CoreType type) {
			resyncResearchCoreUse(u => u != null && u.coresNeeded != null && u.coresNeeded.Count > 0 && u.coresNeeded[0].type == type);
		}
        
		public static void resyncResearchCoreUse(Predicate<Unlock> log) {
			TTUtil.log("Rebuilding core usage counts", TTUtil.diDLL);
			List<int> orig = TechTreeState.instance.usedResearchCores;
			TechTreeState.instance.usedResearchCores = new List<int>();
			foreach (ResearchCoreDefinition.CoreType c in Enum.GetValues(typeof(ResearchCoreDefinition.CoreType)))
				TechTreeState.instance.usedResearchCores.Add(0);
			foreach (TechTreeState.UnlockState ptr in TechTreeState.instance.unlockStates) {
				if (!ptr.exists) {
					//TTUtil.log(string.Format("Can't handle reading unlock info for {0}", ptr.unlockRef.toDebugString()), TTUtil.diDLL);
				}
				else if (ptr.isActive) {
					if (log.Invoke(ptr.unlockRef))
						TTUtil.log("Handling tech "+ptr.unlockRef.toDebugString(), TTUtil.diDLL);
					foreach (Unlock.RequiredCores c in ptr.unlockRef.coresNeeded) {
						TechTreeState.instance.usedResearchCores[(int)c.type] += c.number;
						if (log.Invoke(ptr.unlockRef))
							TTUtil.log("Adding "+c.number+" "+c.type+" cores, total is now "+TechTreeState.instance.usedResearchCores[(int)c.type], TTUtil.diDLL);
					}
					if (log.Invoke(ptr.unlockRef))
						TTUtil.log("Total is now "+TechTreeState.instance.usedResearchCores.toDebugString(), TTUtil.diDLL);
				}
			}
			while (TechTreeState.instance.usedResearchCores.Count > orig.Count && TechTreeState.instance.usedResearchCores.Last() == 0)
				TechTreeState.instance.usedResearchCores.RemoveAt(TechTreeState.instance.usedResearchCores.Count-1);
			
			TTUtil.log("Core indices: Purple,Green,Blue,Gold,Lemon,Ultraviolet", TTUtil.diDLL);
			TTUtil.log("Original value: "+orig.toDebugString(), TTUtil.diDLL);
			printCoreBudget();
		}
		
		public static void printCoreBudget() {			
			TTUtil.log("Total of used cores: "+TechTreeState.instance.usedResearchCores.toDebugString(), TTUtil.diDLL);
			List<int> placed = new List<int>();
			List<int> spendable = new List<int>();
			foreach (ResearchCoreDefinition.CoreType type in Enum.GetValues(typeof(ResearchCoreDefinition.CoreType))) {
				int num = TechTreeState.instance.NumCoresOfTypePlaced((int)type);
				placed.Add(num);
				spendable.Add((num * TechTreeState.coreEffiencyMultipliers[(int)type]).FloorToInt());
			}
			TTUtil.log("Placed cores: "+placed.toDebugString(), TTUtil.diDLL);
			TTUtil.log("Spendable cores: "+spendable.toDebugString(), TTUtil.diDLL);
			List<int> free = new List<int>();
			for (int i = 0; i < spendable.Count; i++) {
				free.Add(spendable[i]-(TechTreeState.instance.usedResearchCores.Count > i ? TechTreeState.instance.usedResearchCores[i] : 0));
			}
			TTUtil.log("Final core budget: "+free.toDebugString(), TTUtil.diDLL);
		}

	}
}
