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
	
	public class CustomTech : NewUnlockDetails, Unlockable {
		
		public Action finalFixes;
		
		private Unlock techInstance;
		
		public string name { get { return displayName; } }
		
		public Unlock unlock {
			get {
				return techInstance;
			}
		}
		
		public bool isUnlocked {
			get {
				return TechTreeState.instance.IsUnlockActive(techInstance.uniqueId);
			}
		}
		
		public CustomTech(Unlock.TechCategory cat, ResearchCoreDefinition.CoreType type, int cores) {
			category = cat;
			coreTypeNeeded = type;
			coreCountNeeded = cores;
		}
		
		public void register() {
			try {
				EMUAdditions.AddNewUnlock(this, true);
				EMU.Events.GameDefinesLoaded += () => {
					techInstance = TTUtil.getUnlock(displayName);
					if (finalFixes != null)
						finalFixes.Invoke();
				};
				TTUtil.log("Registered tech "+this, TTUtil.tryGetModDLL(true));
			}
			catch (Exception ex) {
				TTUtil.log("Failed to register "+this+": "+ex.ToString(), TTUtil.tryGetModDLL(true));
			}
		}
		
		public override sealed string ToString() {
			return displayName+" = "+Enum.GetName(typeof(ResearchCoreDefinition.CoreType), coreTypeNeeded)+"x"+coreCountNeeded;
		}
		
	}
}
