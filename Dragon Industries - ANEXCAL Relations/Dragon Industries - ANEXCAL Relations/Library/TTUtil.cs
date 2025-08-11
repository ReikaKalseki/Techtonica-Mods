using System;
using System.Reflection;
using System.Linq;
using System.Xml;
using System.Diagnostics;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

using UnityEngine;

using EquinoxsModUtils;
using EquinoxsModUtils.Additions;

namespace ReikaKalseki.DIANEXCAL {

	public static class TTUtil {
	    
	    public static readonly Assembly diDLL = Assembly.GetExecutingAssembly();
	    public static readonly Assembly gameDLL = Assembly.GetAssembly(typeof(ThresherDefinition));
	    public static readonly Assembly gameDLL2 = Assembly.GetAssembly(typeof(FluffyUnderware.Curvy.CurvyConnection));
	    
	    public static readonly string gameDir = Directory.GetParent(gameDLL.Location).Parent.Parent.FullName; //managed -> _Data -> root
		//public static readonly string savesDir = "C:/Users/Reika/AppData/LocalLow/Fire Hose Games/Techtonica";
		
		private static readonly Dictionary<int, List<int>> recipeIDs = new Dictionary<int, List<int>>();
		private static readonly Dictionary<ProductionTerminal, TechTreeState.ResearchTier> terminalTiers = new Dictionary<ProductionTerminal, TechTreeState.ResearchTier>();
		private static SchematicsRecipeData[] allRecipes;

        public static bool allowDIDLL = false;
	    
	    private static bool checkedReikaPC;
	    private static bool savedIsReikaPC;
	    
	    private static readonly HashSet<Assembly> assembliesToSkip = new HashSet<Assembly>(){
            diDLL,
            gameDLL,
            gameDLL2
        };
	    
	    private static bool evaluateReikaPC() {
	    	try {
		    	OperatingSystem os = Environment.OSVersion;
		    	if (os.Platform != PlatformID.Win32NT || os.Version.Major != 10)
		    		return false;
		    	if (!System.Security.Principal.WindowsIdentity.GetCurrent().Name.EndsWith("\\Reika", StringComparison.InvariantCultureIgnoreCase)) //windows username
		    		return false;
		    	if (System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture != System.Runtime.InteropServices.Architecture.X64)
		    		return false;
		    	return true;//Steamworks.SteamUser..m_SteamID == 76561198068058411;
	    	}
	    	catch (Exception e) {
	    		log("Error evaluating PC: "+e.ToString(), diDLL);
	    		return false;
	    	}
	    }
	    
	    public static bool isReikaPC() {
	    	if (!checkedReikaPC)
	    		savedIsReikaPC = evaluateReikaPC();
	    	checkedReikaPC = true;
	    	return savedIsReikaPC;
	    }
		
		internal static Assembly tryGetModDLL(bool acceptDI = false) {
	    	try {
				Assembly di = Assembly.GetExecutingAssembly();
				StackFrame[] sf = new StackTrace().GetFrames();
		        if (sf == null || sf.Length == 0)
		        	return Assembly.GetCallingAssembly();
		        foreach (StackFrame f in sf) {
		        	Assembly a = f.GetMethod().DeclaringType.Assembly;
		        	if ((a != di || acceptDI || allowDIDLL) && a != gameDLL && a != gameDLL2)
		                return a;
		        }
		        log("Could not find valid mod assembly: "+string.Join("\n", sf.Select<StackFrame, string>(s => s.GetMethod()+" in "+s.GetMethod().DeclaringType)), diDLL);
	    	}
	    	catch (Exception e) {
	    		log("Failed to find a DLL due to an exception: "+e, diDLL);
	    		return diDLL;
	    	}
	        return Assembly.GetCallingAssembly();
		}
		
		public static void log(string msg, Assembly a = null, int indent = 0) {
			while (msg.Length > 4096) {
				string part = msg.Substring(0, 4096);
				log(part, a);
				msg = msg.Substring(4096);
			}
			string id = (a != null ? a : tryGetModDLL()).GetName().Name.ToUpperInvariant().Replace("PLUGIN_", "");
			if (indent > 0) {
				msg = msg.PadLeft(msg.Length+indent, ' ');
			}
			UnityEngine.Debug.Log(id+": "+msg);
		}
    
	    public static bool canUseDebug() {
	    	return isReikaPC();
	    }
		
		public static bool checkPiracy() {
			HashSet<string> files = new HashSet<string> {"steam_api64.cdx", "steam_api64.ini", "steam_emu.ini", "valve.ini", "chuj.cdx", "SteamUserID.cfg", "Achievements.bin", "steam_settings", "user_steam_id.txt", "account_name.txt", "ScreamAPI.dll", "ScreamAPI32.dll", "ScreamAPI64.dll", "SmokeAPI.dll", "SmokeAPI32.dll", "SmokeAPI64.dll", "Free Steam Games Pre-installed for PC.url", "Torrent-Igruha.Org.URL", "oalinst.exe"};
            foreach (string file in files) {
                if (File.Exists(Path.Combine(Environment.CurrentDirectory, file)))
                	return true;
            }
			return false;
		}
		
		public static bool match(string s, string seek) {
			return s == seek || (!string.IsNullOrEmpty(s) && !string.IsNullOrEmpty(seek) && (seek[0] == '*' && s.EndsWith(seek.Substring(1), StringComparison.InvariantCulture)) || (seek[seek.Length-1] == '*' && s.StartsWith(seek.Substring(0, seek.Length-1), StringComparison.InvariantCulture)));
		}
	    
		public static float getCoreClusterCount() {
			return TechTreeState.instance.freeCores * 0.01F;
		}
	    
	    public static void setIngredients(NewRecipeDetails rec, params object[] items) {
	    	rec.ingredients = new List<RecipeResourceInfo>();
	    	for (int i = 0; i < items.Length; i += 2) {
	    		rec.ingredients.Add(new RecipeResourceInfo(){name = (string)items[i], quantity = (int)items[i+1]});
	    	}
		}
	    
	    public static void setProducts(NewRecipeDetails rec, params object[] items) {
	    	rec.outputs = new List<RecipeResourceInfo>();
	    	for (int i = 0; i < items.Length; i += 2) {
	    		rec.outputs.Add(new RecipeResourceInfo(){name = (string)items[i], quantity = (int)items[i+1]});
	    	}
		}
	    
	    public static void setIngredients(SchematicsRecipeData rec, params object[] items) {
	    	int len = items.Length/2;
	    	rec.ingTypes = new ResourceInfo[len];
	    	rec.ingQuantities = new int[len];
	    	for (int i = 0; i < items.Length; i += 2) {
	    		int idx = i/2;
	    		object ing = items[i];
	    		ResourceInfo info = ing is ResourceInfo ? (ResourceInfo)ing : EMU.Resources.GetResourceInfoByName((string)ing);
	    		if (info == null)
	    			throw new Exception("No such ingredient '"+ing+"'");
	    		rec.ingTypes[idx] = info;
	    		rec.ingQuantities[idx] = (int)items[i+1];
        	};
	    	//rec.ingTypes.ToList().ForEach(res => {if (res == null)throw new Exception("Null ingredient in "+rec.name);});
	    	compileRecipe(rec);
		}
	    
	    public static void setProducts(SchematicsRecipeData rec, params object[] items) {
	    	int len = items.Length/2;
	    	rec.outputTypes = new ResourceInfo[len];
	    	rec.outputQuantities = new int[len];
	    	for (int i = 0; i < items.Length; i += 2) {
	    		int idx = i/2;
	    		object ing = items[i];
	    		ResourceInfo info = ing is ResourceInfo ? (ResourceInfo)ing : EMU.Resources.GetResourceInfoByName((string)ing);
	    		if (info == null)
	    			throw new Exception("No such product '"+ing+"'");
	    		rec.outputTypes[idx] = info;
	    		rec.outputQuantities[idx] = (int)items[i+1];
        	};
	    	//rec.ingTypes.ToList().ForEach(res => {if (res == null)throw new Exception("Null ingredient in "+rec.name);});
	    	compileRecipe(rec);
		}
	    
	    public static void compileRecipe(SchematicsRecipeData rec) {
	    	rec.runtimeIngQuantities = rec.ingQuantities;
	    	if (rec.ingTypes.Length > getMaxAllowedInputs(rec.craftingMethod))
	    		throw new Exception("Too many ingredients in "+rec.toDebugString());
	    	if (rec.outputTypes.Length > getMaxAllowedOutputs(rec.craftingMethod))
	    		throw new Exception("Too many products in "+rec.toDebugString());
	    	log("Recipe "+rec.name+" changed: {"+rec.toDebugString()+"}");
		}
	    
		public static int getMaxAllowedInputs(CraftingMethod cm) {
			switch (cm) {
				case CraftingMethod.Assembler: 
					return 3;
				case CraftingMethod.Uncraftable: 
					return 1;
				case CraftingMethod.Smelter: 
					return 1;
				case CraftingMethod.Thresher: 
					return 1;
				case CraftingMethod.BlastSmelter: 
					return 1;
				case CraftingMethod.Planter:
					return 1;
				case CraftingMethod.Crusher:
					return 2;
			}
	    	return -1;
		}
	    
		public static int getMaxAllowedOutputs(CraftingMethod cm) {
			switch (cm) {
				case CraftingMethod.Assembler: 
					return 1;
				case CraftingMethod.Uncraftable: 
					return 1;
				case CraftingMethod.Smelter: 
					return 1;
				case CraftingMethod.Thresher: 
					return 2;
				case CraftingMethod.BlastSmelter: 
					return 1;
				case CraftingMethod.Planter:
					return 1;
				case CraftingMethod.Crusher:
					return 20;
			}
	    	return -1;
		}
	    
	    public static Unlock getUnlock(string name, bool throwIfNone = true) {
	    	if (!EMU.LoadingStates.hasGameDefinesLoaded)
	    		throw new Exception("Tried to access unlock database before defines were finished!");
	    	Unlock u = EMU.Unlocks.GetUnlockByName(name, false);
	    	if (u == null && throwIfNone)
	    		throw new Exception("No such unlock '"+name+"'!");
	    	return u;
	    }
	    
	    public static void setUnlockRecipes(string name, params SchematicsRecipeData[] recs) {
	    	Unlock u = getUnlock(name);
	    	List<SchematicsRecipeData> li = u.unlockedRecipes;
	    	li.Clear();
	    	li.AddRange(recs.ToList());
	    	foreach (SchematicsRecipeData rec in recs) {
	    		rec.unlock = u;
	    	}
	    	log("Unlock '"+name+"' ("+u.displayName+") now unlocks the following recipes: "+li.toDebugString());
	    }
	    
	    public static void moveUnlockChain(Unlock u, Unlock.TechCategory page) {
	    	moveTechCategory(u, page);
	    	if (u.dependencies != null) {
		    	foreach (Unlock u2 in u.dependencies)
		    		moveUnlockChain(u2, page);
	    	}
	    }
	    
	    public static void moveTechCategory(Unlock u, Unlock.TechCategory page) {
	    	TechTreeState.instance.categoryMapping[(int)u.category].Remove(u.uniqueId);
	    	int pageIdx = (int)page;
	    	TechTreeState.instance.categoryMapping[pageIdx].Add(u.uniqueId);
	    	u.category = page;
	    	
			TechTreeState.instance.categoryMapping[pageIdx].Sort((a, b) => {
				int v = TechTreeState.instance.unlockStates[a].tier.CompareTo(TechTreeState.instance.unlockStates[b].tier);
				return v != 0 ? v : TechTreeState.instance.unlockStates[a].unlockRef.treePosition.CompareTo(TechTreeState.instance.unlockStates[b].unlockRef.treePosition);
			});
	    }
	    
	    public static TechTreeState.ResearchTier getTierAfter(TechTreeState.ResearchTier tier, int steps = 1) {
	    	int val = (int)tier;
	    	val *= MathUtil.intpow2(2, steps);
	    	return (TechTreeState.ResearchTier)val;
	    }
	    
	    public static TechTreeState.ResearchTier getTierAtStation(ProductionTerminal station, int tier) {
	    	TechTreeState.ResearchTier start = TechTreeState.ResearchTier.NONE;
	    	switch (station) {
	    		case ProductionTerminal.LIMA:
	    			start = TechTreeState.ResearchTier.Tier0;
	    			break;
	    		case ProductionTerminal.VICTOR:
	    			start = TechTreeState.ResearchTier.Tier4;
	    			break;
	    		case ProductionTerminal.XRAY:
	    			start = TechTreeState.ResearchTier.Tier11;
	    			break;
	    		case ProductionTerminal.SIERRA:
	    			start = TechTreeState.ResearchTier.Tier16;
	    			break;
	    	}
	    	return getTierAfter(start, tier);
	    }
	    
	    public static void buildRecipeCache() {
	    	FieldInfo f = typeof(GameDefines).GetField("_cachedRecipeLookupArray", BindingFlags.Instance | BindingFlags.NonPublic);
	    	if (f == null)
	    		throw new Exception("No recipe field!");
	    	allRecipes = (SchematicsRecipeData[])f.GetValue(GameDefines.instance);
	    	if (allRecipes == null)
	    		throw new Exception("No recipe array!");
	    	foreach (SchematicsRecipeData rec in allRecipes) {
	    		if (rec == null)
	    			continue;
	    		if (rec.outputTypes.Length > 0 && rec.outputTypes[0] != null) {
	    			int id = rec.outputTypes[0].uniqueId;
	    			if (!recipeIDs.ContainsKey(id))
	    				recipeIDs[id] = new List<int>();
	    			recipeIDs[id].Add(rec.uniqueId);
	    		}
	    	}
	    	log("Compiled recipe cache with "+recipeIDs.Count+" entries", diDLL);
	    }
	    
	    public static List<SchematicsRecipeData> getRecipesByOutput(string name) {
	    	return getRecipesByOutput(EMU.Resources.GetResourceIDByName(name));
	    }
	    
	    public static List<SchematicsRecipeData> getRecipesByOutput(int res) {
	    	if (recipeIDs == null || recipeIDs.Count == 0)
	    		throw new Exception("Checked recipes before recipes are loaded!");
	    	List<int> ids = recipeIDs.ContainsKey(res) ? recipeIDs[res] : null;
	    	return ids == null ? null : ids.Select(id => GameDefines.instance.GetSchematicsRecipeDataById(id)).ToList();
	    }
	    
	    public static SchematicsRecipeData getRecipe(Predicate<SchematicsRecipeData> check) {
	    	if (allRecipes == null || allRecipes.Length == 0)
	    		throw new Exception("Checked recipes before recipes are loaded!");
	    	return allRecipes.ToList().Find(check);
	    }
	    
	    public static List<SchematicsRecipeData> getRecipes(Predicate<SchematicsRecipeData> check) {
	    	if (allRecipes == null || allRecipes.Length == 0)
	    		throw new Exception("Checked recipes before recipes are loaded!");
	    	return allRecipes.ToList().FindAll(check);
	    }
	    
	    public static bool isSeed(ResourceInfo ri) {
	    	return ri.uniqueId == EMU.Resources.GetResourceIDByName(EMU.Names.Resources.KindlevineSeed) || ri.uniqueId == EMU.Resources.GetResourceIDByName(EMU.Names.Resources.ShiverthornSeed) || ri.uniqueId == EMU.Resources.GetResourceIDByName(EMU.Names.Resources.SesamiteSeed);
	    }
	    
	    public static void setDrillUsableUntil(string name, ElevatorLevels lvl, bool include = true) {
        	ResourceInfo res = EMU.Resources.GetResourceInfoByName(name);
        	res.digFuelTier = (int)lvl;
        	if (include)
        		res.digFuelTier++;
        	TTUtil.log(res.displayName+" dig tier now "+res.digFuelTier+" ("+(include ? "<=" : "<")+" "+lvl+")");
	    }
	    
	    public static string getTierDescription(TechTreeState.ResearchTier tier) {
	    	ProductionTerminal from = ProductionTerminal.LIMA;
	    	foreach (KeyValuePair<ProductionTerminal, TechTreeState.ResearchTier> kvp in terminalTiers) {
	    		if (kvp.Value <= tier) {
	    			from = kvp.Key;
	    		}
	    	}
	    	return from+"_T"+(getEnumIndex(tier)-getEnumIndex(terminalTiers[from])+1);
	    }
	    
	    public static int getEnumIndex(object e)/* where E : System.Enum not possible < C#7.3*/{
	    	return Array.IndexOf(Enum.GetValues(e.GetType()), e);
	    }
	    
	    public enum ProductionTerminal {
	    	LIMA,
	    	VICTOR,
	    	XRAY,
	    	SIERRA,
	    }
	    
	    public enum ElevatorLevels {
	    	LIMA,
	    	VICTOR,
	    	STORAGE,
	    	HYDRO,
	    	ADMIN,
	    	XRAY, //6
	    	EXCALIBUR,
	    	RESEARCH,
	    	FOUNDRY, //9
	    	BARRACKS,
	    	FREIGHT,	    	
	    	SIERRA, //12
	    	ARCHIVE,
	    	LABORATORY,
	    	MECH,
	    	UNKNOWN, //16
	    }
	    
	    public enum TechColumns {
	    	LEFT = 0,
	    	MIDLEFT = 20,
	    	CENTERLEFT = 40,
	    	CENTERRIGHT = 60,
	    	MIDRIGHT = 80,
	    	RIGHT = 100,
	    }
	    
	    static TTUtil() {
	    	terminalTiers[ProductionTerminal.LIMA] = TechTreeState.ResearchTier.Tier0;
	    	terminalTiers[ProductionTerminal.VICTOR] = TechTreeState.ResearchTier.Tier4;
	    	terminalTiers[ProductionTerminal.XRAY] = TechTreeState.ResearchTier.Tier11;
	    	terminalTiers[ProductionTerminal.SIERRA] = TechTreeState.ResearchTier.Tier16;
	    }
		
	}
}
