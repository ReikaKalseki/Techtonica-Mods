using System;
using System.Collections.Generic;
using System.Linq;
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
	
	public class CoreBoostTech : CustomTech {
		
		public readonly int effectValue;
		
		public float currentEffect {
			get {
				return TechTreeState.instance.freeCores > 0 && isUnlocked ? TTUtil.getCoreClusterCount()*effectValue/100F : 0;
			}
		}
		
		public CoreBoostTech(int pct, Unlock.RequiredCores cores, string otherDep = null) : this(pct, cores.type, cores.number, otherDep) {
			
		}
		
		public CoreBoostTech(int pct, ResearchCoreDefinition.CoreType type, int cores, string otherDep = null) : base(Unlock.TechCategory.Science, null, null, type, cores) {
			effectValue = pct;
			setDependencies(EMU.Names.Unlocks.CoreBoosting, otherDep);
		}
		
		public CoreBoostTech setText(string label, string stat) {
			displayName = "Core Boost ("+label+")";
			description = "Increases "+stat+" by "+effectValue+"% per Core Cluster.";
			return this;
		}
		
	}
}
