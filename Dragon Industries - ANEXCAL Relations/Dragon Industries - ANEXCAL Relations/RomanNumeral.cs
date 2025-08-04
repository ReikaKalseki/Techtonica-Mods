using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using ReikaKalseki.DIANEXCAL;

namespace ReikaKalseki.DIANEXCAL {

	public static class RomanNumeral {
		
		private static readonly Dictionary<char, int> charValues = new Dictionary<char, int> {
				{ 'I', 1 },
				{ 'V', 5 },
				{ 'X', 10 },
				{ 'L', 50 },
				{ 'C', 100 },
				{ 'D', 500 },
				{ 'M', 1000 },
			};
		
		private static readonly Dictionary<int, string> numberStrings = new Dictionary<int, string> {
				{ 1000, "M" },
				{ 900, "CM" },
				{ 500, "D" },
				{ 400, "CD" },
				{ 100, "C" },
				{ 90, "XC" },
				{ 50, "L" },
				{ 40, "XL" },
				{ 10, "X" },
				{ 9, "IX" },
				{ 5, "V" },
				{ 4, "IV" },
				{ 1, "I" },
			};

		public static string getRomanNumeral(int n) {
			StringBuilder sb = new StringBuilder();
			foreach (KeyValuePair<int, string> kvp in numberStrings) {
				while (n >= kvp.Key) {
					sb.Append(kvp.Value);
					n -= kvp.Key;
				}
			}
			return sb.ToString();
		}

		public static int parseRomanNumeral(string s) {
			int total = 0;

			int cur, prev = 0;
			char ccur, cprev = '\0';

			for (int i = 0; i < s.Length; i++) {
				ccur = s[i];
				prev = cprev != '\0' ? charValues[cprev] : '\0';
				cur = charValues[ccur];

				if (prev != 0 && cur > prev)
					total = total-prev*2+cur;
				else
					total += cur;

				cprev = ccur;
			}

			return total;
		}
	}
}
