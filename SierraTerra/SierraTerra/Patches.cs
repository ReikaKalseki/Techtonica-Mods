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

namespace ReikaKalseki.SierraTech {

	public static class Patches {/*

		[HarmonyPatch(typeof(SandVolume))] 
		[HarmonyPatch("Update")]
		//[HarmonyDebug]
		public static class SandVolumeTickHook {

			static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
				List<CodeInstruction> codes = new List<CodeInstruction>();
				try {
					codes.Add(new CodeInstruction(OpCodes.Ldarg_0));
					codes.Add(new CodeInstruction(OpCodes.Ldarg_1));
					codes.Add(InstructionHandlers.createMethodCall("ReikaKalseki.SierraTerra.SierraTerraMod", "tickSandVolume", false, typeof(SandVolume), typeof(float)));
					codes.Add(new CodeInstruction(OpCodes.Ret));
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
		}*/

		[HarmonyPatch(typeof(SandVolume))] 
		[HarmonyPatch("Update")]
		//[HarmonyDebug]
		public static class SandVolumeRateHook {

			static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
				List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
				try {
					int idx = InstructionHandlers.getInstruction(codes, 0, 0, OpCodes.Ldfld, "GameDefines", "SandPumpDigAmount");
					codes.InsertRange(idx+1, new List<CodeInstruction>{
						new CodeInstruction(OpCodes.Ldarg_0),
						InstructionHandlers.createMethodCall("ReikaKalseki.SierraTech.SierraTechMod", "getSandPumpRate", false, typeof(float), typeof(SandVolume))});
					codes.Add(new CodeInstruction(OpCodes.Ret));
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

		[HarmonyPatch(typeof(SandPumpInspector))] 
		[HarmonyPatch("SetInspector")]
		//[HarmonyDebug]
		public static class SandInspectorRateHook {

			static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
				List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
				try {
					int idx = InstructionHandlers.getInstruction(codes, 0, 0, OpCodes.Ldfld, "GameDefines", "SandPumpDigAmount");
					codes.InsertRange(idx+1, new List<CodeInstruction>{
						new CodeInstruction(OpCodes.Ldarg_0),
						new CodeInstruction(OpCodes.Ldarg_1),
						InstructionHandlers.createMethodCall("ReikaKalseki.SierraTech.SierraTechMod", "getSandPumpRateDisplay", false, typeof(float), typeof(SandPumpInspector), typeof(SandPumpInstance))});
					codes.Add(new CodeInstruction(OpCodes.Ret));
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
