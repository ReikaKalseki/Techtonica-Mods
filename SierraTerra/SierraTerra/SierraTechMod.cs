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
        
		private static CustomTech[] sandPumpBoostTechs;
		private static CustomTech sandPumpCoreTech;
        
		public static readonly Assembly modDLL = Assembly.GetExecutingAssembly();
		
		private static readonly SandPumpTech.TechLevel[] techLevels = new SandPumpTech.TechLevel[]{
			new SandPumpTech.TechLevel(1, ResearchCoreDefinition.CoreType.Green, 25, 1.5F, 0),
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

				DIMod.onDefinesLoadedFirstTime += onDefinesLoaded;
				
				DIMod.onRecipesLoaded += onRecipesLoaded;
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
        
		private static void onRecipesLoaded() {
			
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
