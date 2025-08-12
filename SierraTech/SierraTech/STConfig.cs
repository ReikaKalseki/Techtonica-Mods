using System;

using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Xml;
using ReikaKalseki.DIANEXCAL;

namespace ReikaKalseki.SierraTech
{
	public class STConfig
	{		
		public enum ConfigEntries {
			[ConfigEntry("Core Effectiveness Tech Cost multiplier", typeof(float), 1, 0, 100, float.NaN)]COREPOWERBOOSTCOST, //Core effectiveness tech cost multiplier, zero to disable the techs
			[ConfigEntry("Sand Pump Core Boost Effect", typeof(int), 10, 1, 50, float.NaN)]PUMPCOREEFFECT, //The speed boost in percent per core cluster from sand pump core tech
			[ConfigEntry("Spectral Cube Core Boost Effect", typeof(int), 5, 1, 100, float.NaN)]CUBECOREEFFECT, //The yield boost in percent per core cluster from spectral cube core tech
			[ConfigEntry("Spectral Cube Core Boost Cost", typeof(int), 200, 10, 10000, float.NaN)]CUBECORECOST, //The core cost for the spectral cube core tech
			[ConfigEntry("Sesamite Blasting Cost", typeof(int), 8, 1, 200, float.NaN)]SESAMITEBLASTCOST, //Sesamite stem cost per blast cycle
			[ConfigEntry("Scrap Recycling Loop Yield Multiplier", typeof(float), 1, 0.5F, 5, float.NaN)]SCRAPCYCLERATIO, //Scrap yield multiplier for the scrap recycling loop
			[ConfigEntry("Gold Purification recipe speed multiplier", typeof(float), 1, 0.1F, 25F, float.NaN)]GOLDDUSTSPEED, //Gold purification speed multiplier
			[ConfigEntry("Adjust excavator bit scaling", true)]ELEVATORDRILLADJUST, //Whether to slightly increase the usable range of elevator excavator bits, and the range on max-tier ones
			[ConfigEntry("Reduce Crusher Power Yield Recipe Tech Costs", true)]CRUSHERPOWERTECHCOST, //Whether to slightly reduce the cost of the two techs that unlock the main power-producing recipes for post-Sierra
			[ConfigEntry("Crusher crystal-to-power multiplier", typeof(float), 1, 0.1F, 25F, float.NaN)]CRUSHERPOWERSCALE, //Crusher crystal forming/crushing power yield multiplier
			[ConfigEntry("Atlantum Prismatic Cubes", true)]ATLANTCUBE, //Whether to replace the duplicated Sesamite ingot ingredient in Prismatic Cubes with a more interesting ingredient
		}
	}
}
