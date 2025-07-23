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

namespace ReikaKalseki.DIANEXCAL {

	public static class Patches {

		[HarmonyPatch(typeof(BuildMachineAction))] //see GridManager::Get*Frills*, PropManager::GetBuildFrillListForCoord, and DestructibleData 
		[HarmonyPatch(MethodType.Constructor, new Type[]{typeof(NetworkAction.NetworkActionType)})]
		public static class StopFoliageDestruction {

			static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
				List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
				try {
					int idx = InstructionHandlers.getFirstOpcode(codes, 0, OpCodes.Ldc_I4_1);
					codes[idx].opcode = OpCodes.Ldc_I4_0;
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

		[HarmonyPatch(typeof(PlanterInstance))] 
		[HarmonyPatch("SimUpdate")]
		//[HarmonyDebug]
		public static class PlanterSpeedHook {

			static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
				List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
				try {
					int idx = InstructionHandlers.getMethodCallByName(codes, 0, 0, "PlanterInstance+PlantSlot", "UpdateGrowth");
					codes[idx] = InstructionHandlers.createMethodCall("ReikaKalseki.DIANEXCAL.DIMod", "tickPlantSlot", false, typeof(PlanterInstance.PlantSlot).MakeByRefType(), typeof(float), typeof(PlanterInstance).MakeByRefType());
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
					codes.InsertRange(idx+1, new List<CodeInstruction>{new CodeInstruction(OpCodes.Ldarg_1), InstructionHandlers.createMethodCall("ReikaKalseki.DIANEXCAL.DIMod", "getPlanterGrowthTimeForUI", false, typeof(float), typeof(PlanterInstance).MakeByRefType())});
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
					InstructionHandlers.patchEveryReturnPre(codes, new CodeInstruction(OpCodes.Ldarg_0), InstructionHandlers.createMethodCall("ReikaKalseki.DIANEXCAL.DIMod", "initializePlanterSettings", false, typeof(PlanterDefinition)));
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
					InstructionHandlers.patchEveryReturnPre(codes, new CodeInstruction(OpCodes.Ldarg_0), InstructionHandlers.createMethodCall("ReikaKalseki.DIANEXCAL.DIMod", "initializeThresherSettings", false, typeof(ThresherDefinition)));
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
			
		static class PatchLib {
			
			internal static void replaceMaybeInlinedFieldWithConstant(List<CodeInstruction> codes, string owner, string name, float origVal, float newVal) {
				int idx = -1;
				try {
					idx = InstructionHandlers.getInstruction(codes, 0, 0, OpCodes.Ldsfld, owner, name);
				}
				catch {
					idx = InstructionHandlers.getInstruction(codes, 0, 0, OpCodes.Ldc_R4, origVal);
				}
				codes[idx] = new CodeInstruction(OpCodes.Ldc_R4, newVal);
			}
			
		}
	}
}
