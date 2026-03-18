using System;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace Unify2Min
{
	[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
	public class Unify2MinPlugin : BaseUnityPlugin
	{
		public const string PLUGIN_GUID = "com.dspmod.unify2min";
		public const string PLUGIN_NAME = "统一为分钟 (Unify 2 Min)";
		public const string PLUGIN_VERSION = "1.0.1";

		internal static ManualLogSource Log;
		private static Harmony _harmony;

		private void Awake()
		{
			Log = Logger;
			Log.LogInfo("[Unify2Min] Awake 入口");
			_harmony = new Harmony(PLUGIN_GUID);

			// 手动打补丁并记录每个绑定结果，便于排查「绑定错函数」问题
			ApplyPatch(
				"ItemProto.GetPropValue",
				AccessTools.Method(typeof(ItemProto), "GetPropValue", new[] { typeof(int), typeof(StringBuilder), typeof(int) }),
				AccessTools.Method(typeof(Unify2MinPatches.ItemProto_GetPropValue_Patch), "Postfix"));
			ApplyPatch(
				"UIBeltWindow._OnUpdate",
				AccessTools.Method(typeof(UIBeltWindow), "_OnUpdate"),
				AccessTools.Method(typeof(Unify2MinPatches.UIBeltWindow_OnUpdate_Patch), "Postfix"));
			ApplyPatch(
				"UIInserterWindow._OnUpdate",
				AccessTools.Method(typeof(UIInserterWindow), "_OnUpdate"),
				AccessTools.Method(typeof(Unify2MinPatches.UIInserterWindow_OnUpdate_Patch), "Postfix"));
			ApplyPatch(
				"UIInserterBuildTip._OnUpdate",
				AccessTools.Method(typeof(UIInserterBuildTip), "_OnUpdate"),
				AccessTools.Method(typeof(Unify2MinPatches.UIInserterBuildTip_OnUpdate_Patch), "Postfix"));

			Log.LogInfo($"[Unify2Min] {PLUGIN_NAME} v{PLUGIN_VERSION} 已加载；传送带/分拣器单位已统一为 / min");
		}

		private void ApplyPatch(string label, MethodBase target, MethodBase postfix)
		{
			if (target == null)
			{
				Log.LogWarning($"[Unify2Min] 未找到目标方法: {label}");
				return;
			}
			if (postfix == null)
			{
				Log.LogWarning($"[Unify2Min] 未找到补丁方法: {label}");
				return;
			}
			try
			{
				var postfixMethod = postfix as MethodInfo ?? throw new InvalidOperationException("Postfix must be MethodInfo");
				_harmony.Patch(target, postfix: new HarmonyMethod(postfixMethod));
				Log.LogInfo($"[Unify2Min] 已绑定: {label}");
			}
			catch (Exception ex)
			{
				Log.LogError($"[Unify2Min] 绑定失败 {label}: {ex.Message}");
			}
		}

		private void OnDestroy()
		{
			_harmony?.UnpatchSelf();
		}
	}
}
