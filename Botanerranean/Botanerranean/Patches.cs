using System;
using System.IO;    //For data read/write methods
using System.Collections;   //Working with Lists and Collections
using System.Collections.Generic;   //Working with Lists and Collections
using System.Linq;   //More advanced manipulation of lists/collections
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;  //Needed for most Unity Enginer manipulations: Vectors, GameObjects, Audio, etc.
using ReikaKalseki.DIANEXCAL;

namespace ReikaKalseki.Botanerranean {

	public static class Patches {

		[HarmonyPatch(typeof(PlanterInstance))] 
		[HarmonyPatch("SimUpdate")]
		//[HarmonyDebug]
		public static class PlanterSpeedHook {

			static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
				List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
				try {
					int idx = InstructionHandlers.getMethodCallByName(codes, 0, 0, "PlanterInstance+PlantSlot", "UpdateGrowth");
					codes[idx] = InstructionHandlers.createMethodCall("ReikaKalseki.Botanerranean.BotanerraneanMod", "tickPlantSlot", false, typeof(PlanterInstance.PlantSlot).MakeByRefType(), typeof(float), typeof(PlanterInstance).MakeByRefType());
					codes.Insert(idx, new CodeInstruction(OpCodes.Ldarg_0));
					FileLog.Log("Done patch " + MethodBase.GetCurrentMethod().DeclaringType);
				}
				catch (Exception e) {
					FileLog.Log("Caught exception when running patch " + MethodBase.GetCurrentMethod().DeclaringType + "!");
					FileLog.Log(e.Message);
					FileLog.Log(e.StackTrace);
					FileLog.Log(e.ToString());
				}
				return codes.AsEnumerable();
			}
		}

		[HarmonyPatch(typeof(PlanterInspectorItem))] 
		[HarmonyPatch("UpdateSlots")]
		//[HarmonyDebug]
		public static class PlanterSpeedUIHook {

			static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
				List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
				try {
					int idx = InstructionHandlers.getInstruction(codes, 0, 0, OpCodes.Ldfld, "PlanterInstance+PlantSlot", "totalGrowthDuration");
					codes.InsertRange(idx+1, new List<CodeInstruction>{new CodeInstruction(OpCodes.Ldarg_1), InstructionHandlers.createMethodCall("ReikaKalseki.Botanerranean.BotanerraneanMod", "getPlanterGrowthTimeForUI", false, typeof(float), typeof(PlanterInstance).MakeByRefType())});
					FileLog.Log("Done patch " + MethodBase.GetCurrentMethod().DeclaringType);
				}
				catch (Exception e) {
					FileLog.Log("Caught exception when running patch " + MethodBase.GetCurrentMethod().DeclaringType + "!");
					FileLog.Log(e.Message);
					FileLog.Log(e.StackTrace);
					FileLog.Log(e.ToString());
				}
				return codes.AsEnumerable();
			}
		}

		[HarmonyPatch(typeof(PlanterDefinition))] 
		[HarmonyPatch("InitOverrideSettings")]
		//[HarmonyDebug]
		public static class PlanterPowerHook {

			static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
				List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
				try {
					InstructionHandlers.patchEveryReturnPre(codes, new CodeInstruction(OpCodes.Ldarg_0), InstructionHandlers.createMethodCall("ReikaKalseki.Botanerranean.BotanerraneanMod", "initializePlanterSettings", false, typeof(PlanterDefinition)));
					FileLog.Log("Done patch " + MethodBase.GetCurrentMethod().DeclaringType);
				}
				catch (Exception e) {
					FileLog.Log("Caught exception when running patch " + MethodBase.GetCurrentMethod().DeclaringType + "!");
					FileLog.Log(e.Message);
					FileLog.Log(e.StackTrace);
					FileLog.Log(e.ToString());
				}
				return codes.AsEnumerable();
			}
		}

		[HarmonyPatch(typeof(ThresherDefinition))] 
		[HarmonyPatch("InitOverrideSettings")]
		//[HarmonyDebug]
		public static class ThresherPowerHook {

			static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
				List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
				try {
					InstructionHandlers.patchEveryReturnPre(codes, new CodeInstruction(OpCodes.Ldarg_0), InstructionHandlers.createMethodCall("ReikaKalseki.Botanerranean.BotanerraneanMod", "initializeThresherSettings", false, typeof(ThresherDefinition)));
					FileLog.Log("Done patch " + MethodBase.GetCurrentMethod().DeclaringType);
				}
				catch (Exception e) {
					FileLog.Log("Caught exception when running patch " + MethodBase.GetCurrentMethod().DeclaringType + "!");
					FileLog.Log(e.Message);
					FileLog.Log(e.StackTrace);
					FileLog.Log(e.ToString());
				}
				return codes.AsEnumerable();
			}
		}

		[HarmonyPatch(typeof(ThresherInstance))] 
		[HarmonyPatch("UpdateCrafting")]
		//[HarmonyDebug]
		public static class ThresherSeedYield {

			static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
				List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
				try {
					int idx = InstructionHandlers.getInstruction(codes, 0, 0, OpCodes.Ldfld, "SchematicsRecipeData", "outputQuantities");
					idx = InstructionHandlers.getFirstOpcode(codes, idx, OpCodes.Stloc_3);
					codes.InsertRange(idx, new List<CodeInstruction>{new CodeInstruction(OpCodes.Ldloc_1), new CodeInstruction(OpCodes.Ldarg_0), InstructionHandlers.createMethodCall("ReikaKalseki.Botanerranean.BotanerraneanMod", "getThresherItemYield", false, typeof(int), typeof(int), typeof(ThresherInstance).MakeByRefType())});
					FileLog.Log("Done patch " + MethodBase.GetCurrentMethod().DeclaringType);
				}
				catch (Exception e) {
					FileLog.Log("Caught exception when running patch " + MethodBase.GetCurrentMethod().DeclaringType + "!");
					FileLog.Log(e.Message);
					FileLog.Log(e.StackTrace);
					FileLog.Log(e.ToString());
				}
				return codes.AsEnumerable();
			}
		}

		[HarmonyPatch(typeof(ProductionTerminalInstance))] 
		[HarmonyPatch("RefreshResourceRequirements")]
		//[HarmonyDebug]
		public static class PTUpgradeResourceHook {

			static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
				List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
				try {
					InstructionHandlers.patchInitialHook(codes, new CodeInstruction(OpCodes.Ldarg_0), InstructionHandlers.createMethodCall("ReikaKalseki.Botanerranean.BotanerraneanMod", "onProdTerminalResourceUpdate", false, typeof(ProductionTerminalInstance).MakeByRefType()));
					FileLog.Log("Done patch " + MethodBase.GetCurrentMethod().DeclaringType);
				}
				catch (Exception e) {
					FileLog.Log("Caught exception when running patch " + MethodBase.GetCurrentMethod().DeclaringType + "!");
					FileLog.Log(e.Message);
					FileLog.Log(e.StackTrace);
					FileLog.Log(e.ToString());
				}
				return codes.AsEnumerable();
			}
		}
	}
}
