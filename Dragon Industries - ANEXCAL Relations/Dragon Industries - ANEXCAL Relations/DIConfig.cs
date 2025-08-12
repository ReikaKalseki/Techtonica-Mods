using System;

using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Xml;
using ReikaKalseki.DIANEXCAL;

namespace ReikaKalseki.DIANEXCAL
{
	public class DIConfig
	{		
		public enum ConfigEntries {
			[ConfigEntry("Metal ore smelting/threshing is 1:1 rather than 2:1", false)]EFFICIENTMETAL,
		}
	}
}
