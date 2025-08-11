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

	public class CrusherPowerTrimTech : CustomTech {
		
		private static readonly List<CrusherPowerTrimTech> techs = new List<CrusherPowerTrimTech>();
		
		private readonly string techLink;
		public float efficiency { get; private set; }

		internal CrusherPowerTrimTech(string link) : base(Unlock.TechCategory.Energy, link.Replace("ASM", "Crush"), "placeholder", ResearchCoreDefinition.CoreType.Purple, 0) {
			setSprite(EMU.Names.Unlocks.CrusherMKI);
			setDependencies(link, techs.Count == 0 ? null : techs[techs.Count-1].name);
			finalFixes = () => {
				Unlock u = unlock;
				Unlock from = TTUtil.getUnlock(techLink);
				TTUtil.log("Adjusting "+this+" to match "+techLink);
				u.treePosition = from.treePosition-20;
				u.requiredTier = from.requiredTier;
				u.coresNeeded = from.coresNeeded.Select(rc => new Unlock.RequiredCores{type = rc.type, number = rc.number}).ToList();
				u.setDescription(from.getDescription().Replace("Assembler", "Crusher"));
				string pct = u.description.Substring(u.description.LastIndexOf(' ')).Trim();
				efficiency = 1-int.Parse(pct.Substring(0, 2))/100F; //clip to just the number
				TTUtil.log("Adjustment complete: "+u.toDebugString()+" and "+efficiency.ToString("0.00")+"x power");
			};
			techs.Add(this);
			techLink = link;
		}
	}
}
