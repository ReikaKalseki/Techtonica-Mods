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

	public class SandPumpTech : CustomTech {
		
		private static readonly Dictionary<int, SandPumpTech> levels = new Dictionary<int, SandPumpTech>();

		internal SandPumpTech(TechLevel t) : base(Unlock.TechCategory.Terraforming, "Sand Pump Power "+RomanNumeral.getRomanNumeral(t.level), t.description, t.coreType, t.coreCount) {
			levels[t.level] = this;
			setSprite(EMU.Names.Unlocks.SandPump);
			setPosition(TTUtil.getTierAtStation(TTUtil.ProductionTerminal.SIERRA, t.ptTier), (int)TTUtil.TechColumns.LEFT);
			if (t.level > 1)
				setDependencies(levels[t.level-1]);
		}
		
		internal class TechLevel {
			
			public readonly int level;
			public readonly int ptTier;
			public readonly ResearchCoreDefinition.CoreType coreType;
			public readonly int coreCount;
			public readonly float pumpMultiplier;
			
			public string description {
				get {
					return "Increases pumping rate of all Sand Pumps by "+((pumpMultiplier-1)*100).ToString("0")+"%. Does not increase sand production rate.";
				}
			}
			
			internal TechLevel(int l, ResearchCoreDefinition.CoreType c, int cc, float f, int tt) {
				level = l;
				coreType = c;
				coreCount = cc;
				pumpMultiplier = f;
				ptTier = tt;
			}
			
			public override string ToString() {
				return string.Format("[TechLevel Level={0}, CoreType={1}, CoreCount={2}, PumpMultiplier={3}]", level, coreType, coreCount, pumpMultiplier);
			}

			
		}
	}
}
