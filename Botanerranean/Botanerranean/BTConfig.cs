using System;

using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Xml;
using ReikaKalseki.DIANEXCAL;

namespace ReikaKalseki.Botanerranean
{
	public class BTConfig
	{		
		public enum ConfigEntries {
			[ConfigEntry("Seed Yield I Effect", typeof(int), 5, 1, 10, float.NaN)]SEED1EFFECT, //The yield boost from the first seed yield tech
			[ConfigEntry("Seed Yield II Effect", typeof(int), 10, 5, 50, float.NaN)]SEED2EFFECT, //The yield boost from the second seed yield tech
			[ConfigEntry("Seed Yield III Effect", typeof(int), 25, 10, 100, float.NaN)]SEED3EFFECT, //The yield boost from the third seed yield tech
			[ConfigEntry("Planter Core Boost Effect", typeof(int), 5, 1, 50, float.NaN)]PLANTERCOREEFFECT, //The speed boost in percent per core cluster from planter core tech
			[ConfigEntry("Seed Yield Core Boost Effect", typeof(int), 5, 1, 50, float.NaN)]SEEDCOREEFFECT, //The yield boost in percent per core cluster from seed yield core tech
			[ConfigEntry("Seed Recycling I Yield Scalar", typeof(float), 1, 0, 5, float.NaN)]SEEDPLANTMATTERRATIO1, //Plantmatter yield multiplier for seed recycling I
			[ConfigEntry("Seed Recycling II Yield Scalar", typeof(float), 1, 0, 5, float.NaN)]SEEDPLANTMATTERRATIO2, //Carbon yield multiplier for seed recycling II
			[ConfigEntry("Thresher I Base Extra Seed Yield Chance", typeof(float), 1, 0, 5, float.NaN)]THRESHER1BONUSBASE, //Base percent chance for an extra seed from thresher I 
			[ConfigEntry("Thresher II Base Extra Seed Yield Chance", typeof(float), 2.5F, 0, 10, float.NaN)]THRESHER2BONUSBASE, //Base percent chance for an extra seed from thresher II
		}
	}
}
