using System;
using System.IO;
using System.Xml;
using System.Reflection;

using System.Collections.Generic;

using UnityEngine;

using EquinoxsModUtils;
using Techtonica;

namespace ReikaKalseki.DIANEXCAL
{
	public static class TextureManager {
		
		private static readonly Dictionary<Assembly, Dictionary<string, Texture2D>> textures = new Dictionary<Assembly, Dictionary<string, Texture2D>>();
		//private static readonly Texture2D NOT_FOUND = ImageUtils.LoadTextureFromFile(path); 
		
		static TextureManager() {
			
		}
		
		public static void refresh() {
			textures.Clear();
		}
		
		public static Texture2D getTexture(Assembly a, string path) {
			if (a == null)
				throw new Exception("You must specify a mod to load the texture for!");
			if (!textures.ContainsKey(a))
				textures[a] = new Dictionary<string, Texture2D>();
			if (!textures[a].ContainsKey(path)) {
				textures[a][path] = loadTexture(a, path);
			}
			return textures[a][path];
		}
		
		private static Texture2D loadTexture(Assembly a, string relative) {
			if (a == null)
				a = TTUtil.diDLL;
			string folder = Path.GetDirectoryName(a.Location);
			string path = Path.Combine(folder, relative+".png");
			TTUtil.log("Loading texture from '"+path+"'", a);
			return loadTextureFromFile(path);
		}
		
		private static Texture2D loadTextureFromFile(string file) {
	        Texture2D tex = new Texture2D(1, 1);
	        if (File.Exists(file)) {
	        	tex.LoadImage(File.ReadAllBytes(file));
	        }
	        else {
	        	TTUtil.log("No texture found at "+file, TTUtil.diDLL);
	        }
	        return tex;
		}
		
		public static Sprite createSprite(Texture2D tex) {
			return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0, 0), 512);
		}
		
	}
}
