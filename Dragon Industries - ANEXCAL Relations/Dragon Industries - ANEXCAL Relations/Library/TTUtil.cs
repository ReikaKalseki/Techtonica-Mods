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
	    public static readonly Assembly gameDLL = Assembly.GetAssembly(typeof(GridManager));
	    public static readonly Assembly gameDLL2 = Assembly.GetAssembly(typeof(FluffyUnderware.Curvy.CurvyConnection));
	    
	    public static readonly string gameDir = Directory.GetParent(gameDLL.Location).Parent.Parent.FullName; //managed -> _Data -> root
		//public static readonly string savesDir = "C:/Users/Reika/AppData/LocalLow/Fire Hose Games/Techtonica";
		
		private static readonly Dictionary<int, int> recipeIDs = new Dictionary<int, int>();

        public static bool allowDIDLL = false;
	    
	    private static bool checkedReikaPC;
	    private static bool savedIsReikaPC;
	    
	    private static readonly HashSet<Assembly> assembliesToSkip = new HashSet<Assembly>(){
            diDLL,
            gameDLL,
            gameDLL2
        };
	    
	    static TTUtil() {
			
	    }
	    
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
		
		public static void log(string s, Assembly a = null, int indent = 0) {
			while (s.Length > 4096) {
				string part = s.Substring(0, 4096);
				log(part, a);
				s = s.Substring(4096);
			}
			string id = (a != null ? a : tryGetModDLL()).GetName().Name.ToUpperInvariant().Replace("PLUGIN_", "");
			if (indent > 0) {
				s = s.PadLeft(s.Length+indent, ' ');
			}
			UnityEngine.Debug.Log(id+": "+s);
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
	    
	    public static void compileRecipe(SchematicsRecipeData rec) {
	    	rec.runtimeIngQuantities = rec.ingQuantities;
	    	if (rec.ingTypes.Length > 3)
	    		throw new Exception("Too many ingredients in "+rec.toDebugString());
	    	log("Recipe "+rec.name+" changed: {"+rec.toDebugString()+"}");
	    }
	    
	    public static SchematicsRecipeData getSmelterRecipe(string item) {
	    	ResourceInfo res = EMU.Resources.GetResourceInfoByName(item);
	    	if (res == null)
	    		throw new Exception("No such item '"+item+"'!");
	    	List<SchematicsRecipeData> li = GameDefines.instance.GetValidSmelterRecipes(new List<int>{res.uniqueId}, 100, 100, false);
	    	if (li.Count == 0)
	    		log("No smelter recipe found using '"+item+"'!", diDLL);
	    	return li.Count == 0 ? null : li[0];
	    }
	    
	    public static Unlock getUnlock(string name) {
	    	if (!EMU.LoadingStates.hasGameDefinesLoaded)
	    		throw new Exception("Tried to access unlock database before defines were finished!");
	    	Unlock u = EMU.Unlocks.GetUnlockByName(name, false);
	    	if (u == null)
	    		throw new Exception("No such unlock '"+name+"'!");
	    	return u;
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
	    	SchematicsRecipeData[] arr = (SchematicsRecipeData[])f.GetValue(GameDefines.instance);
	    	if (arr == null)
	    		throw new Exception("No recipe array!");
	    	foreach (SchematicsRecipeData rec in arr) {
	    		if (rec == null)
	    			continue;
	    		if (rec.outputTypes.Length > 0 && rec.outputTypes[0] != null) {
	    			recipeIDs[rec.outputTypes[0].uniqueId] = rec.uniqueId;
	    		}
	    	}
	    	log("Compiled recipe cache with "+recipeIDs.Count+" entries", diDLL);
	    }
	    
	    public static int getRecipeID(int res) {
	    	return recipeIDs.ContainsKey(res) ? recipeIDs[res] : -1;
	    }
	    
	    public enum ProductionTerminal {
	    	LIMA,
	    	VICTOR,
	    	XRAY,
	    	SIERRA,
	    }
		
	}
}
