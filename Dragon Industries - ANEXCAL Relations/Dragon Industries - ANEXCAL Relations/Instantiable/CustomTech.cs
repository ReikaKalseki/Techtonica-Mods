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
	
	public class CustomTech : NewUnlockDetails, Unlockable {
		
		public Action finalFixes;
		
		public string name { get { return displayName; } }
		
		public Assembly ownerMod { get; private set; }
		
		public Unlock unlock { get; private set; }
		
		private string unlockPositionReferenceTier;
		private string unlockPositionReferenceColumn;
		
		private string spriteSource;
		private bool spriteFromUnlock;
		
		public bool isUnlocked {
			get {
				return unlock != null && TechTreeState.instance.IsUnlockActive(unlock.uniqueId);
			}
		}
		
		public CustomTech(Unlock.TechCategory cat, string name, string desc, Unlock.RequiredCores cores) : this(cat, name, desc, cores.type, cores.number) {
			
		}
		
		public CustomTech(Unlock.TechCategory cat, string name, string desc, ResearchCoreDefinition.CoreType type, int cores) {
			category = cat;
			coreTypeNeeded = type;
			coreCountNeeded = cores;
			displayName = name;
			description = desc;
			requiredTier = TechTreeState.ResearchTier.Tier0;
			treePosition = 0;
			
			ownerMod = TTUtil.tryGetModDLL();
		}
		
		public CustomTech setPosition(string positionRef) {
			return setPosition(positionRef, positionRef);
		}
		
		public CustomTech setPosition(string tier, string col) {
			unlockPositionReferenceTier = tier;
			unlockPositionReferenceColumn = col;
			return this;
		}
		
		public CustomTech setPosition(TechTreeState.ResearchTier tier, TTUtil.TechColumns slot) {
			requiredTier = tier;
			treePosition = (int)slot;
			return this;
		}
		
		public CustomTech setSprite(string name, bool isUnlock = true) {
			spriteSource = name;
			spriteFromUnlock = isUnlock;
			return this;
		}
		
		public CustomTech setDependencies(CustomTech dep1, CustomTech dep2 = null) {
			return setDependencies(dep1.name, dep2 == null ? null : dep2.name);
		}
		
		public CustomTech setDependencies(string dep1, string dep2 = null) {
			dependencyNames.Clear();
			this.dependencyNames.Add(dep1);
			if (!string.IsNullOrEmpty(dep2))
				dependencyNames.Add(dep2);
			TTUtil.log("Setting dependencies for tech "+this+": "+dependencyNames.toDebugString(), ownerMod);
			return this;
		}
		
		public void register() {
			if (string.IsNullOrEmpty(name))
				throw new Exception("Invalid tech with empty name: "+this);
			if (string.IsNullOrEmpty(description))
				throw new Exception("Invalid tech with empty description: "+this);
			try {
				EMUAdditions.AddNewUnlock(this, true);
				DIMod.onTechsLoadedFirstTime += () => {
					unlock = TTUtil.getUnlock(displayName, false);
					if (unlock == null)
						throw new Exception("Tech "+this+" failed to find its registered counterpart");
					else
						TTUtil.log("Tech "+name+" injected: "+unlock.toDebugString(), ownerMod);
					
					//unlock.descriptionHash = "";
					//unlock.displayNameHash = "";
					
					if (unlockPositionReferenceTier != null) {
						TTUtil.log("Aligning tech "+this+" to unlock '"+unlockPositionReferenceTier+"/"+unlockPositionReferenceColumn+"'", ownerMod);
						Unlock u1 = TTUtil.getUnlock(unlockPositionReferenceTier);
						Unlock u2 = TTUtil.getUnlock(unlockPositionReferenceColumn);
						unlock.treePosition = u2.treePosition;
						unlock.requiredTier = u1.requiredTier;
						//unlock.category = u.category;
					}
					
					if (!string.IsNullOrEmpty(spriteSource)) {
						TTUtil.log("Fetching sprite for tech "+this+" from: '"+spriteSource+"' ("+spriteFromUnlock+")", ownerMod);
						if (spriteFromUnlock) {
							Unlock src = TTUtil.getUnlock(spriteSource, false);
							if (src != null) {
								unlock.sprite = src.sprite;
								if (src.sprite == null) {
									TTUtil.log("Target unlock has no sprite either!", ownerMod);
								}
							}
							else {
								TTUtil.log("No unlock found by that name, searching for item", ownerMod);
								ResourceInfo item = EMU.Resources.GetResourceInfoByName(spriteSource, false);
								if (item != null) {
									unlock.sprite = item.sprite;
								}
								else {
									throw new Exception("Source was neither the name of an unlock nor an item");
								}
							}
						}
						else {
							unlock.sprite = TextureManager.createSprite(TextureManager.getTexture(TTUtil.tryGetModDLL(true), "Textures/" + spriteSource));
						}
						
						if (unlock.sprite == null) {
							TTUtil.log("No sprite found!", ownerMod);
						}
					}
					
					if (finalFixes != null)
						finalFixes.Invoke();
				};
				TTUtil.log("Registered tech "+this, ownerMod);
			}
			catch (Exception ex) {
				TTUtil.log("Failed to register "+this, ownerMod);
				throw ex;
			}
		}
		
		public void setRecipes(IEnumerable<CustomRecipe> recipes) {
			setRecipes(recipes.ToArray());
		}
		
		public void setRecipes(params CustomRecipe[] recipes) {
			EMU.Events.TechTreeStateLoaded += () => {
				setRecipes(recipes.Select(cr => cr.recipe).ToArray());
			};
			TTUtil.log("Linking tech "+this+" to recipes "+recipes.toDebugString(), ownerMod);
		}
		
		public void setRecipes(params SchematicsRecipeData[] recipes) {
			TTUtil.setUnlockRecipes(name, recipes);
		}
		
		public override sealed string ToString() {
			return displayName+" = "+Enum.GetName(typeof(ResearchCoreDefinition.CoreType), coreTypeNeeded)+"x"+coreCountNeeded;
		}
		
	}
}
