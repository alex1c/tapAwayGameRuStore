using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace TapAway.Editor
{
	/// <summary>
	/// Phase 0 verification helpers invoked from Unity batchmode.
	/// Keeps foundation smoke checks scriptable without gameplay code.
	/// </summary>
	public static class Phase0Verify
	{
		private const string ReportPath = "Logs/phase0-verify-report.txt";
		private const string ApkPath = "Builds/Android/TapAway-dev.apk";

		/// <summary>
		/// Writes Android / player / bootstrap verification facts for the report.
		/// </summary>
		public static void WriteStatusReport()
		{
			var sb = new StringBuilder();
			sb.AppendLine("UNITY_VERSION=" + Application.unityVersion);
			sb.AppendLine("PRODUCT_NAME=" + PlayerSettings.productName);
			sb.AppendLine("COMPANY_NAME=" + PlayerSettings.companyName);
			sb.AppendLine("BUNDLE_VERSION=" + PlayerSettings.bundleVersion);
			sb.AppendLine("ANDROID_APP_ID=" + PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android));
			sb.AppendLine("DEFAULT_ORIENTATION=" + PlayerSettings.defaultInterfaceOrientation);
			sb.AppendLine("ANDROID_MIN_SDK=" + (int)PlayerSettings.Android.minSdkVersion);
			sb.AppendLine("ANDROID_TARGET_SDK=" + (int)PlayerSettings.Android.targetSdkVersion);
			sb.AppendLine("ANDROID_TARGET_ARCHES=" + PlayerSettings.Android.targetArchitectures);
			sb.AppendLine("ANDROID_SCRIPTING_BACKEND=" + PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android));
			sb.AppendLine("ANDROID_USE_CUSTOM_KEYSTORE=" + PlayerSettings.Android.useCustomKeystore);
			sb.AppendLine("ACTIVE_BUILD_TARGET=" + EditorUserBuildSettings.activeBuildTarget);
			sb.AppendLine("ANDROID_BUILD_APP_BUNDLE=" + EditorUserBuildSettings.buildAppBundle);

			var androidRoot = BuildPipeline.GetPlaybackEngineDirectory(BuildTarget.Android, BuildOptions.None);
			sb.AppendLine("ANDROID_PLAYBACK_ENGINE_DIR=" + androidRoot);
			sb.AppendLine("ANDROID_SDK_EXISTS=" + Directory.Exists(Path.Combine(androidRoot, "SDK")));
			sb.AppendLine("ANDROID_NDK_EXISTS=" + Directory.Exists(Path.Combine(androidRoot, "NDK")));
			sb.AppendLine("ANDROID_OPENJDK_EXISTS=" + Directory.Exists(Path.Combine(androidRoot, "OpenJDK")));

			var scenePath = "Assets/_Project/Scenes/Bootstrap.unity";
			sb.AppendLine("BOOTSTRAP_IN_BUILD=" + EditorBuildSettings.scenes.Any(s => s.enabled && s.path == scenePath));

			var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
			sb.AppendLine("BOOTSTRAP_OPENED=" + scene.IsValid());
			sb.AppendLine("BOOTSTRAP_ROOT_COUNT=" + scene.rootCount);

			var camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
			var light = UnityEngine.Object.FindFirstObjectByType<Light>();
			var marker = GameObject.Find("FoundationMarker");
			var bootstrap = GameObject.Find("Bootstrap");
			sb.AppendLine("HAS_CAMERA=" + (camera != null));
			sb.AppendLine("HAS_LIGHT=" + (light != null));
			sb.AppendLine("HAS_FOUNDATION_MARKER=" + (marker != null));
			sb.AppendLine("HAS_BOOTSTRAP_GO=" + (bootstrap != null));

			if (camera != null)
			{
				sb.AppendLine("CAMERA_CLEAR_FLAGS=" + camera.clearFlags);
				sb.AppendLine("CAMERA_BG=" + ColorUtility.ToHtmlStringRGB(camera.backgroundColor));
			}

			if (bootstrap != null)
			{
				var comps = bootstrap.GetComponents<MonoBehaviour>();
				sb.AppendLine("BOOTSTRAP_MB_COUNT=" + comps.Length);
				foreach (var c in comps)
				{
					sb.AppendLine("BOOTSTRAP_MB=" + (c == null ? "MISSING_SCRIPT" : c.GetType().FullName));
				}
			}

			sb.AppendLine("RENDER_PIPELINE=" + (GraphicsSettings.currentRenderPipeline == null ? "Built-in" : GraphicsSettings.currentRenderPipeline.name));

			Directory.CreateDirectory("Logs");
			File.WriteAllText(ReportPath, sb.ToString());
			Debug.Log("[Phase0Verify] Wrote " + ReportPath);
			EditorApplication.Exit(0);
		}

		/// <summary>
		/// Builds a development Android APK smoke artifact (not a release AAB).
		/// </summary>
		public static void BuildAndroidDevelopmentApk()
		{
			Directory.CreateDirectory(Path.GetDirectoryName(ApkPath) ?? "Builds/Android");

			EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
			EditorUserBuildSettings.buildAppBundle = false;
			EditorUserBuildSettings.development = true;
			EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.Generic;

			PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.calculatorplatform.tapaway");
			PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
			PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
			PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

			var scenes = EditorBuildSettings.scenes
				.Where(s => s.enabled)
				.Select(s => s.path)
				.ToArray();

			var options = new BuildPlayerOptions
			{
				scenes = scenes,
				locationPathName = ApkPath,
				target = BuildTarget.Android,
				targetGroup = BuildTargetGroup.Android,
				options = BuildOptions.Development
			};

			var report = BuildPipeline.BuildPlayer(options);
			var summary = report.summary;
			var resultPath = "Logs/phase0-android-build.txt";
			var body =
				"RESULT=" + summary.result + Environment.NewLine +
				"TOTAL_ERRORS=" + summary.totalErrors + Environment.NewLine +
				"TOTAL_WARNINGS=" + summary.totalWarnings + Environment.NewLine +
				"OUTPUT=" + summary.outputPath + Environment.NewLine +
				"SIZE_BYTES=" + summary.totalSize + Environment.NewLine +
				"TIME_SEC=" + summary.totalTime.TotalSeconds + Environment.NewLine;

			Directory.CreateDirectory("Logs");
			File.WriteAllText(resultPath, body);
			Debug.Log("[Phase0Verify] Android build:\n" + body);

			EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
		}

		/// <summary>
		/// Menu / batch helper: builds indicators for all six directions and
		/// validates multi-face readability geometry without pixel tests.
		/// </summary>
		[MenuItem("TapAway/Validate Direction Readability")]
		public static void ValidateDirectionReadabilityMenu()
		{
			var report = ValidateDirectionReadabilityInternal();
			Debug.Log("[DirectionReadability]\n" + report);
			if (Application.isBatchMode)
			{
				Directory.CreateDirectory("Logs");
				File.WriteAllText("Logs/direction-readability.txt", report);
				EditorApplication.Exit(report.StartsWith("PASS", StringComparison.Ordinal) ? 0 : 1);
			}
		}

		/// <summary>
		/// Batchmode entry: TapAway.Editor.Phase0Verify.ValidateDirectionReadabilityMenu
		/// </summary>
		public static void ValidateDirectionReadabilityBatch()
		{
			ValidateDirectionReadabilityMenu();
		}

		private static string ValidateDirectionReadabilityInternal()
		{
			var root = new GameObject("DirectionReadabilityProbe");
			try
			{
				var plate = new Material(Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default"))
				{
					color = new Color(0.05f, 0.05f, 0.07f, 1f)
				};
				var accent = new Material(Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default"))
				{
					color = new Color(1f, 0.92f, 0.12f, 1f)
				};

				var views = new System.Collections.Generic.List<TapAway.Runtime.BlockView>();
				var id = 1;
				foreach (TapAway.Core.EscapeDirection direction in Enum.GetValues(typeof(TapAway.Core.EscapeDirection)))
				{
					var blockGo = new GameObject("ProbeBlock_" + id);
					blockGo.transform.SetParent(root.transform, false);
					var indicator = TapAway.Runtime.DirectionIndicatorBuilder.Build(
						blockGo.transform,
						direction,
						plate,
						accent);
					var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
					body.transform.SetParent(blockGo.transform, false);
					UnityEngine.Object.DestroyImmediate(body.GetComponent<Collider>());
					var view = blockGo.AddComponent<TapAway.Runtime.BlockView>();
					view.SetVisualParts(indicator, body.GetComponent<Renderer>());
					view.Bind(
						new TapAway.Core.PuzzleBlock(id, 0, 0, 0, direction),
						Color.cyan);
					views.Add(view);
					id++;
				}

				return TapAway.Runtime.DirectionReadabilityValidator.ValidateActiveViews(views);
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(root);
			}
		}
	}
}
