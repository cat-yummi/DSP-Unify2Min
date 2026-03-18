using System;
using System.Globalization;
using HarmonyLib;
using UnityEngine.UI;

namespace Unify2Min
{
	/// <summary>
	/// 统一为「/ min」的五处：
	/// 1. 传送带物品提示：运载量 x/s → x / min
	/// 2. 普通分拣器物品提示（Grade 1-3）：x往返/秒/格 → x / min/格
	/// 3. 集装分拣器物品提示（Grade 4）：120格子/秒 → 7200 / min
	/// 4. 传送带点开面板：速度 x个货物/s → x / min
	/// 5. 分拣器点开面板：x往返/s → x / min（格已确定，不带/格）
	/// 6. 分拣器建造时箭头提示：x往返/s → x / min
	/// </summary>
	internal static class Unify2MinPatches
	{
		private const string UnitPerMin = " / min";
		private const string UnitPerMinPerGrid = " /min/格";

		/// <summary>统一的日志辅助方法</summary>
		private static void TryConvertAndLog(bool converted, ref bool logged, string itemType)
		{
			if (!converted || logged) return;
			logged = true;
			Unify2MinPlugin.Log?.LogInfo($"[Unify2Min] 物品提示：{itemType}已改为 / min");
		}

		/// <summary> 1. 传送带物品提示：运载量 x/s → x / min </summary>
		[HarmonyPatch(typeof(ItemProto), nameof(ItemProto.GetPropValue))]
		internal static class ItemProto_GetPropValue_Patch
		{
			private static bool _loggedBelt;
			private static bool _loggedInserter;

			[HarmonyPostfix]
			private static void Postfix(ItemProto __instance, int index, ref string __result)
			{
				if (string.IsNullOrEmpty(__result) || __instance.prefabDesc == null) return;

				if (__instance.prefabDesc.isBelt)
				{
					TryConvertAndLog(ConvertBeltTooltip(ref __result), ref _loggedBelt, "传送带运载量");
					return;
				}

				if (__instance.prefabDesc.isInserter)
				{
					TryConvertAndLog(ConvertInserterTooltip(__instance.prefabDesc, ref __result), ref _loggedInserter, "分拣器运送速度");
				}
			}
		}

		/// <summary>转换传送带提示：x/s → x / min</summary>
		private static bool ConvertBeltTooltip(ref string text)
		{
			if (!text.EndsWith("/s")) return false;
			string numPart = text.Substring(0, text.Length - 2).Trim();
			if (!double.TryParse(numPart, NumberStyles.Any, CultureInfo.InvariantCulture, out double v)) return false;
			text = ((int)Math.Round(v * 60.0)).ToString() + UnitPerMin;
			return true;
		}

		/// <summary>转换分拣器提示：普通分拣器 x/s/格 → x / min/格，集装分拣器 120格子/秒 → 7200 / min</summary>
		private static bool ConvertInserterTooltip(PrefabDesc prefabDesc, ref string text)
		{
			if (!IsInserterSpeedText(text)) return false;
			if (!TryParseNumber(text, out int numStart, out double rate)) return false;

			int perMinInt = (int)Math.Round(rate * 60.0);
			string unit = (prefabDesc.inserterGrade == 4) ? UnitPerMin : UnitPerMinPerGrid;
			text = FormatWithOptionalColor(text, numStart, perMinInt.ToString(), unit);
			return true;
		}

		/// <summary>判断是否为分拣器速率文本（排除耗时、功率等）</summary>
		private static bool IsInserterSpeedText(string text)
		{
			if (text.IndexOf("单程", StringComparison.Ordinal) >= 0 || text.IndexOf("耗时", StringComparison.Ordinal) >= 0)
				return false;
			return text.IndexOf("往返", StringComparison.Ordinal) >= 0 || 
			       text.IndexOf("每秒", StringComparison.Ordinal) >= 0 || 
			       text.IndexOf("/秒", StringComparison.Ordinal) >= 0;
		}

		/// <summary>从文本中解析数字（可能在 color 标签内）</summary>
		private static bool TryParseNumber(string text, out int numStart, out double rate)
		{
			numStart = 0;
			rate = 0;

			if (text.StartsWith("<color="))
			{
				int close = text.IndexOf('>', 7);
				if (close < 0) return false;
				numStart = close + 1;
			}

			int i = numStart;
			while (i < text.Length && (char.IsDigit(text[i]) || text[i] == '.')) i++;
			if (i <= numStart) return false;

			return double.TryParse(text.Substring(numStart, i - numStart), NumberStyles.Any, CultureInfo.InvariantCulture, out rate);
		}

		/// <summary>格式化文本，保留或去除 color 标签</summary>
		private static string FormatWithOptionalColor(string original, int numStart, string value, string unit)
		{
			return numStart > 0 
				? original.Substring(0, numStart) + value + unit + "</color>"
				: value + unit;
		}

		/// <summary> 2. 传送带点开面板：速度 x个货物/s → x / min </summary>
		[HarmonyPatch(typeof(UIBeltWindow), "_OnUpdate")]
		internal static class UIBeltWindow_OnUpdate_Patch
		{
			private static bool _logged;

			[HarmonyPostfix]
			private static void Postfix(UIBeltWindow __instance)
			{
				if (!ValidateBeltWindow(__instance, out var belt)) return;

				float perMin = (float)belt.speed * 60f / 10f * 60f;
				__instance.speedText.text = "带速度".Translate() + perMin.ToString("0") + UnitPerMin;
				
				LogOnce(ref _logged, "传送带面板速度");
			}

			private static bool ValidateBeltWindow(UIBeltWindow window, out BeltComponent belt)
			{
				belt = default;
				if (window.beltId == 0 || window.traffic?.beltPool == null) return false;
				if (window.beltId >= window.traffic.beltPool.Length) return false;
				belt = window.traffic.beltPool[window.beltId];
				return belt.id == window.beltId;
			}
		}

		/// <summary> 4. 分拣器点开面板：x往返/s → x / min </summary>
		[HarmonyPatch(typeof(UIInserterWindow), "_OnUpdate")]
		internal static class UIInserterWindow_OnUpdate_Patch
		{
			private static bool _logged;

			[HarmonyPostfix]
			private static void Postfix(UIInserterWindow __instance)
			{
				if (!ValidateInserterWindow(__instance, out var inserter)) return;

				__instance.rttText.text = CalculateInserterPanelRate(inserter);
				LogOnce(ref _logged, "分拣器面板往返");
			}

			private static bool ValidateInserterWindow(UIInserterWindow window, out InserterComponent inserter)
			{
				inserter = default;
				if (window.inserterId == 0 || window.factorySystem?.inserterPool == null) return false;
				if (window.inserterId >= window.factorySystem.inserterPool.Length) return false;
				inserter = window.factorySystem.inserterPool[window.inserterId];
				return inserter.id == window.inserterId;
			}

			private static string CalculateInserterPanelRate(InserterComponent inserter)
			{
				return inserter.bidirectional
					? "7200" + UnitPerMin
					: ((int)Math.Round(18000000.0 / inserter.stt)).ToString() + UnitPerMin;
			}
		}

		/// <summary> 5. 分拣器建造时箭头提示：x往返/s → x / min，集装分拣器 7200 / min </summary>
		[HarmonyPatch(typeof(UIInserterBuildTip), "_OnUpdate")]
		internal static class UIInserterBuildTip_OnUpdate_Patch
		{
			private static bool _logged;

			[HarmonyPostfix]
			private static void Postfix(UIInserterBuildTip __instance)
			{
				if (__instance.arrowTipText2 == null || __instance.desc == null) return;
				if (!__instance.topGroup.activeSelf) return;

				// 集装分拣器未满级时，不做任何修改（保持原版显示）
				if (__instance.desc.inserterGrade == 4 && !GameMain.history.inserterBidirectional)
					return;

				__instance.arrowTipText2.text = CalculateBuildTipRate(__instance.desc, __instance.gridLen);
				LogOnce(ref _logged, "分拣器建造提示");
			}

			private static string CalculateBuildTipRate(PrefabDesc desc, int gridLen)
			{
				// 本逻辑不兼容低级集装分拣器
				// 集装分拣器满级（已解锁双向科技）：固定 7200 / min
				if (desc.inserterGrade == 4 && GameMain.history.inserterBidirectional)
				{
					return "7200" + UnitPerMin;
				}

				// 普通分拣器：按距离计算
				int num3 = Math.Max((int)(desc.inserterSTT * gridLen + 0.499f), 10000);
				if (gridLen < 0.001f) return "0" + UnitPerMin;
				return ((int)Math.Round(300000f / num3 * 60f)).ToString() + UnitPerMin;
			}
		}

		/// <summary>统一的单次日志方法</summary>
		private static void LogOnce(ref bool logged, string description)
		{
			if (logged) return;
			logged = true;
			Unify2MinPlugin.Log?.LogInfo($"[Unify2Min] {description}已改为 / min");
		}
	}
}
