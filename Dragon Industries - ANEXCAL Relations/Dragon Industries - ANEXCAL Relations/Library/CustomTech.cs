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
	
	public class CustomTech : NewUnlockDetails {
		
		public Action finalizer;
		
		public bool isUnlocked {
			get {
				return TechTreeState.instance.IsUnlockActive(techInstance.uniqueId);
			}
		}
		
		private Unlock techInstance;
		
		public CustomTech(Unlock.TechCategory cat, ResearchCoreDefinition.CoreType type, int cores) {
			category = cat;
			coreTypeNeeded = type;
			coreCountNeeded = cores;
		}
		
		public void register() {
			EMUAdditions.AddNewUnlock(this, true);
			techInstance = EMU.Unlocks.GetUnlockByName(displayName, true);
			EMU.Events.GameDefinesLoaded += finalizer;
			TTUtil.log("Registered tech "+this, TTUtil.diDLL);
		}
		
		public override sealed string ToString() {
			return displayName+" = "+Enum.GetName(typeof(ResearchCoreDefinition.CoreType), coreTypeNeeded)+"x"+coreCountNeeded;
		}
		
	}
}
