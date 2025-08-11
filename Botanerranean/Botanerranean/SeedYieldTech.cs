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

namespace ReikaKalseki.Botanerranean {

	public class SeedYieldTech : CustomTech {
		
		private static readonly Dictionary<int, SeedYieldTech> levels = new Dictionary<int, SeedYieldTech>();

		internal SeedYieldTech(TechLevel t) : base(Unlock.TechCategory.Synthesis, "Seed Yield "+RomanNumeral.getRomanNumeral(t.level), t.description, t.coreType, t.coreCount) {
			levels[t.level] = this;
			
			setPosition(t.ptTier, t.column);
			setSprite(EMU.Names.Resources.KindlevineSeed);
			if (t.level > 1)
				setDependencies(levels[t.level-1]);
		}
		
		internal class TechLevel {
			
			public readonly int level;
			public readonly TechTreeState.ResearchTier ptTier;
			public readonly TTUtil.TechColumns column;
			public readonly ResearchCoreDefinition.CoreType coreType;
			public readonly int coreCount;
			public readonly int yieldBoost;
			
			public string description {
				get {
					return "Increases seed yield from threshing by "+yieldBoost+"%.";
				}
			}
			
			internal TechLevel(int l, ResearchCoreDefinition.CoreType c, int cc, int f, TechTreeState.ResearchTier tt, TTUtil.TechColumns col = TTUtil.TechColumns.MIDLEFT) {
				level = l;
				coreType = c;
				coreCount = cc;
				yieldBoost = f;
				ptTier = tt;
				column = col;
			}
			
			public override string ToString() {
				return string.Format("[TechLevel Level={0}, CoreType={1}, CoreCount={2}, Yield+={3}]", level, coreType, coreCount, yieldBoost);
			}

			
		}
	}
}
