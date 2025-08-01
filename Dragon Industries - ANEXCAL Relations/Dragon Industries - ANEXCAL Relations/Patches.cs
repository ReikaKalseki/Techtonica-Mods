﻿using System;
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

		[HarmonyPatch(typeof(GameDefines))] 
		[HarmonyPatch("PostLoadRuntimeInit")]
		public static class RecipeLoadHook {

			static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
				List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
				try {
					InstructionHandlers.patchEveryReturnPre(codes, InstructionHandlers.createMethodCall("ReikaKalseki.DIANEXCAL.DIMod", "onRecipeDataLoaded", false, new Type[0]));
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

		[HarmonyPatch(typeof(AccumulatorDefinition))] 
		[HarmonyPatch("InitOverrideSettings")]
		public static class AccumulatorCapacityHook {

			static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
				List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
				try {
					InstructionHandlers.patchEveryReturnPre(codes, new CodeInstruction(OpCodes.Ldarg_0), InstructionHandlers.createMethodCall("ReikaKalseki.DIANEXCAL.DIMod", "initializeAccumulatorSettings", false, typeof(AccumulatorDefinition)));
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

		[HarmonyPatch(typeof(GridManager))] 
		public static class DigProtectionHook {

		    public static MethodBase TargetMethod() {
		        return AccessTools.Method(typeof(GridManager), "IsVoxelTerrainProtected", new Type[]{typeof(GridPos).MakeByRefType()});
		    }

			static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
				List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
				try {
					InstructionHandlers.patchEveryReturnPre(codes, InstructionHandlers.createMethodCall("ReikaKalseki.DIANEXCAL.DIMod", "interceptProtection", false, typeof(bool)));
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
			
		public static class PatchLib {
			
			public static void replaceMaybeInlinedFieldWithConstant(List<CodeInstruction> codes, string owner, string name, float origVal, float newVal) {
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
