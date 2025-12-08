/**
 * ScenePerformanceAnalyzer.cs
 *
 * Analyzes scene performance by testing each visible object individually.
 * Rotates objects in a temporary scene and measures draw calls, set pass calls, and FPS.
 *
 * CC0-Attribution License by Lilithe for CozyCon 2025
 */

using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Collections;

namespace CozyCon.Tools
{
	/// <summary>
	/// Analyzes scene performance by testing individual objects
	/// </summary>
	public class ScenePerformanceAnalyzer : EditorWindow
	{
		[MenuItem("Tools/CozyCon/Performance/Scene Performance Analyzer")]
		public static void ShowWindow()
		{
			GetWindow<ScenePerformanceAnalyzer>("Scene Performance Analyzer");
		}

		private Vector2 scrollPosition;
		private List<ObjectPerformanceData> performanceResults = new List<ObjectPerformanceData>();
		private bool isAnalyzing = false;
		private float analysisProgress = 0f;
		private string currentAnalyzingObject = "";
		private bool includeInactiveObjects = false;
		private bool testWithRotation = true;
		private int rotationFrames = 60; // Frames to test each object
		private float rotationSpeed = 90f; // Degrees per second
		private bool showDetailedResults = true;
		private Scene originalScene;
		private Scene testScene;
		private Camera testCamera;
		private Light testLight;
		private int testLayer = 31; // Use layer 31 for isolated testing
		private RenderTexture cameraPreview;
		private bool showCameraPreview = true;

		[System.Serializable]
		public class ObjectPerformanceData
		{
			public string objectName;
			public string objectPath;
			public GameObject gameObject;
			public PerformanceMetrics metrics;
			public int triangleCount;
			public int materialCount;
			public bool hasLOD;
			public bool isStatic;
			public float performanceImpact; // 0-100 scale
			public PerformanceRating rating;
		}

		[System.Serializable]
		public class PerformanceMetrics
		{
			public float averageFPS;
			public float minFPS;
			public float maxFPS;
			public int averageDrawCalls;
			public int maxDrawCalls;
			public int averageSetPassCalls;
			public int maxSetPassCalls;
			public int peakTriangles;
			public float frameTimeMS;
		}

		public enum PerformanceRating
		{
			Excellent, // 90+ FPS, minimal impact
			Good,      // 60-90 FPS, low impact
			Fair,      // 30-60 FPS, moderate impact
			Poor,      // 15-30 FPS, high impact
			Critical   // <15 FPS, severe impact
		}

		void OnGUI()
		{
			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

			GUILayout.Label("Scene Performance Analyzer", EditorStyles.boldLabel);
			EditorGUILayout.Space();

			if (!isAnalyzing)
			{
				DrawAnalysisSettings();
				DrawAnalysisButtons();
			}
			else
			{
				DrawAnalysisProgress();
			}

			if (performanceResults.Count > 0)
			{
				DrawResults();
			}

			EditorGUILayout.EndScrollView();
		}

		private void DrawAnalysisSettings()
		{
			GUILayout.Label("Analysis Settings", EditorStyles.boldLabel);
			EditorGUILayout.BeginVertical(GUI.skin.box);

			includeInactiveObjects = EditorGUILayout.Toggle("Include Inactive Objects", includeInactiveObjects);
			testWithRotation = EditorGUILayout.Toggle("Test with Rotation", testWithRotation);

			if (testWithRotation)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("Rotation Frames:", GUILayout.Width(100));
				rotationFrames = EditorGUILayout.IntSlider(rotationFrames, 30, 300);
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("Rotation Speed:", GUILayout.Width(100));
				rotationSpeed = EditorGUILayout.Slider(rotationSpeed, 30f, 360f);
				EditorGUILayout.LabelField("°/s", GUILayout.Width(25));
				EditorGUILayout.EndHorizontal();
			}

			showDetailedResults = EditorGUILayout.Toggle("Show Detailed Results", showDetailedResults);
			showCameraPreview = EditorGUILayout.Toggle("Show Camera Preview", showCameraPreview);

			EditorGUILayout.EndVertical();

			EditorGUILayout.HelpBox(
				"This tool will:\n" +
				"• Create a temporary scene for testing\n" +
				"• Test each visible object individually\n" +
				"• Measure draw calls, set pass calls, and FPS\n" +
				"• Rotate objects to test from multiple angles\n" +
				"• Generate a performance report",
				MessageType.Info);
		}

		private void DrawAnalysisButtons()
		{
			EditorGUILayout.Space();
			GUILayout.Label("Analysis Actions", EditorStyles.boldLabel);

			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

			if (GUILayout.Button("🔍 Analyze Scene Performance", GUILayout.Height(30)))
			{
				StartAnalysis();
			}

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("📊 Quick Scan (Static Only)"))
			{
				QuickScan();
			}
			if (GUILayout.Button("🎯 Test Selected Objects"))
			{
				TestSelectedObjects();
			}
			EditorGUILayout.EndHorizontal();

			EditorGUI.EndDisabledGroup();

			if (EditorApplication.isPlaying)
			{
				EditorGUILayout.HelpBox("Performance analysis cannot run in Play Mode.", MessageType.Warning);
			}
		}

		private void DrawAnalysisProgress()
		{
			EditorGUILayout.Space();
			GUILayout.Label("Analysis in Progress...", EditorStyles.boldLabel);

			var rect = EditorGUILayout.GetControlRect(false, 20);
			EditorGUI.ProgressBar(rect, analysisProgress, $"{analysisProgress * 100:F0}%");

			EditorGUILayout.LabelField($"Currently analyzing: {currentAnalyzingObject}");

			// Show live camera preview if enabled
			if (showCameraPreview && cameraPreview != null)
			{
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Live Camera Preview:", EditorStyles.boldLabel);

				// Calculate preview size (maintain aspect ratio)
				float previewWidth = EditorGUIUtility.currentViewWidth - 40f;
				float previewHeight = previewWidth * (cameraPreview.height / (float)cameraPreview.width);

				// Limit preview height
				if (previewHeight > 200f)
				{
					previewHeight = 200f;
					previewWidth = previewHeight * (cameraPreview.width / (float)cameraPreview.height);
				}

				var previewRect = EditorGUILayout.GetControlRect(false, previewHeight);
				previewRect.width = previewWidth;
				previewRect.x = (EditorGUIUtility.currentViewWidth - previewWidth) * 0.5f;

				EditorGUI.DrawPreviewTexture(previewRect, cameraPreview);

				// Show current frame info
				EditorGUILayout.LabelField($"Frame: {currentFrameCount}/{rotationFrames} | Rotation: {(currentFrameCount / (float)rotationFrames * 360f):F0}°");
			}

			if (GUILayout.Button("Cancel Analysis"))
			{
				CancelAnalysis();
			}
		}

		private void DrawResults()
		{
			EditorGUILayout.Space();
			GUILayout.Label($"📊 Performance Results ({performanceResults.Count} objects)", EditorStyles.boldLabel);

			// Summary Statistics
			DrawResultsSummary();

			// Sorting Options
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Sort by Performance Impact"))
			{
				performanceResults = performanceResults.OrderByDescending(r => r.performanceImpact).ToList();
			}
			if (GUILayout.Button("Sort by Draw Calls"))
			{
				performanceResults = performanceResults.OrderByDescending(r => r.metrics.averageDrawCalls).ToList();
			}
			if (GUILayout.Button("Sort by Triangles"))
			{
				performanceResults = performanceResults.OrderByDescending(r => r.triangleCount).ToList();
			}
			EditorGUILayout.EndHorizontal();

			// Individual Results
			EditorGUILayout.Space();
			foreach (var result in performanceResults.Take(20)) // Show top 20
			{
				DrawObjectResult(result);
			}

			if (performanceResults.Count > 20)
			{
				EditorGUILayout.LabelField($"... and {performanceResults.Count - 20} more objects");
			}

			// Export Options
			EditorGUILayout.Space();
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("📋 Export Report"))
			{
				ExportReport();
			}
			if (GUILayout.Button("🔄 Re-analyze"))
			{
				StartAnalysis();
			}
			if (GUILayout.Button("❌ Clear Results"))
			{
				performanceResults.Clear();
			}
			EditorGUILayout.EndHorizontal();
		}

		private void DrawResultsSummary()
		{
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.LabelField("Summary", EditorStyles.boldLabel);

			var critical = performanceResults.Count(r => r.rating == PerformanceRating.Critical);
			var poor = performanceResults.Count(r => r.rating == PerformanceRating.Poor);
			var fair = performanceResults.Count(r => r.rating == PerformanceRating.Fair);
			var good = performanceResults.Count(r => r.rating == PerformanceRating.Good);
			var excellent = performanceResults.Count(r => r.rating == PerformanceRating.Excellent);

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField($"🚨 Critical: {critical}");
			EditorGUILayout.LabelField($"⚠️ Poor: {poor}");
			EditorGUILayout.LabelField($"🔶 Fair: {fair}");
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField($"✅ Good: {good}");
			EditorGUILayout.LabelField($"⭐ Excellent: {excellent}");
			EditorGUILayout.EndHorizontal();

			var totalDrawCalls = performanceResults.Sum(r => r.metrics.averageDrawCalls);
			var totalTriangles = performanceResults.Sum(r => r.triangleCount);
			EditorGUILayout.LabelField($"Total Draw Calls: {totalDrawCalls:N0}");
			EditorGUILayout.LabelField($"Total Triangles: {totalTriangles:N0}");

			EditorGUILayout.EndVertical();
		}

		private void DrawObjectResult(ObjectPerformanceData result)
		{
			EditorGUILayout.BeginVertical(GUI.skin.box);

			// Header with rating
			EditorGUILayout.BeginHorizontal();
			string ratingIcon = GetRatingIcon(result.rating);
			EditorGUILayout.LabelField($"{ratingIcon} {result.objectName}", EditorStyles.boldLabel);

			if (GUILayout.Button("Select", GUILayout.Width(60)) && result.gameObject != null)
			{
				Selection.activeGameObject = result.gameObject;
				EditorGUIUtility.PingObject(result.gameObject);
			}
			EditorGUILayout.EndHorizontal();

			// Performance Impact Bar
			var rect = EditorGUILayout.GetControlRect(false, 15);
			Color impactColor = GetImpactColor(result.performanceImpact);
			EditorGUI.DrawRect(rect, Color.gray);
			var fillRect = new Rect(rect.x, rect.y, rect.width * (result.performanceImpact / 100f), rect.height);
			EditorGUI.DrawRect(fillRect, impactColor);
			EditorGUI.LabelField(rect, $"Performance Impact: {result.performanceImpact:F1}%", EditorStyles.centeredGreyMiniLabel);

			if (showDetailedResults)
			{
				// Detailed metrics
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField($"FPS: {result.metrics.averageFPS:F1} (min: {result.metrics.minFPS:F1})");
				EditorGUILayout.LabelField($"Draw Calls: {result.metrics.averageDrawCalls}");
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField($"Set Pass: {result.metrics.averageSetPassCalls}");
				EditorGUILayout.LabelField($"Triangles: {result.triangleCount:N0}");
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField($"Materials: {result.materialCount}");
				EditorGUILayout.LabelField($"Static: {(result.isStatic ? "Yes" : "No")}");
				EditorGUILayout.LabelField($"LOD: {(result.hasLOD ? "Yes" : "No")}");
				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.EndVertical();
		}

		private string GetRatingIcon(PerformanceRating rating)
		{
			return rating switch
			{
				PerformanceRating.Excellent => "⭐",
				PerformanceRating.Good => "✅",
				PerformanceRating.Fair => "🔶",
				PerformanceRating.Poor => "⚠️",
				PerformanceRating.Critical => "🚨",
				_ => "•"
			};
		}

		private Color GetImpactColor(float impact)
		{
			if (impact < 20) return Color.green;
			if (impact < 40) return Color.yellow;
			if (impact < 70) return new Color(1f, 0.5f, 0f); // Orange
			return Color.red;
		}

		private void StartAnalysis()
		{
			if (isAnalyzing) return;

			Debug.Log("[Scene Performance Analyzer] Starting scene performance analysis...");

			originalScene = SceneManager.GetActiveScene();
			performanceResults.Clear();
			isAnalyzing = true;
			analysisProgress = 0f;

			EditorApplication.update += UpdateAnalysis;
			CreateTestScene();
		}

		private void CreateTestScene()
		{
			// Create temporary scene for testing
			testScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
			SceneManager.SetActiveScene(testScene);

			// Create render texture for camera preview
			if (showCameraPreview)
			{
				cameraPreview = new RenderTexture(512, 512, 24);
				cameraPreview.Create();
			}

			// Create test camera with isolated layer rendering
			var cameraObj = new GameObject("Test Camera");
			testCamera = cameraObj.AddComponent<Camera>();
			testCamera.transform.position = new Vector3(0, 1, -5);
			testCamera.clearFlags = CameraClearFlags.SolidColor;
			testCamera.backgroundColor = Color.black;

			// Only render the test layer (layer 31)
			testCamera.cullingMask = 1 << testLayer;

			// Set render texture for preview
			if (cameraPreview != null)
			{
				testCamera.targetTexture = cameraPreview;
			}

			// Create test light
			var lightObj = new GameObject("Test Light");
			testLight = lightObj.AddComponent<Light>();
			testLight.type = LightType.Directional;
			testLight.transform.rotation = Quaternion.Euler(50, 330, 0);
			testLight.cullingMask = 1 << testLayer; // Only light the test layer

			Debug.Log("[Scene Performance Analyzer] Test scene created with isolated rendering and camera preview");
		}
		private List<GameObject> objectsToTest = new List<GameObject>();
		private int currentObjectIndex = 0;
		private int currentFrameCount = 0;
		private List<float> currentFPSReadings = new List<float>();
		private List<int> currentDrawCallReadings = new List<int>();
		private List<int> currentSetPassReadings = new List<int>();
		private GameObject currentTestObject;

		private void UpdateAnalysis()
		{
			if (!isAnalyzing) return;

			// Initialize object list if needed
			if (objectsToTest.Count == 0)
			{
				SceneManager.SetActiveScene(originalScene);
				objectsToTest = CollectObjectsToTest();
				SceneManager.SetActiveScene(testScene);
				Debug.Log($"[Scene Performance Analyzer] Found {objectsToTest.Count} objects to test");
			}

			// Check if we're done
			if (currentObjectIndex >= objectsToTest.Count)
			{
				FinishAnalysis();
				return;
			}

			// Start testing new object
			if (currentTestObject == null)
			{
				StartTestingObject(objectsToTest[currentObjectIndex]);
			}

			// Continue testing current object
			if (currentFrameCount < rotationFrames)
			{
				ContinueTestingObject();
			}
			else
			{
				FinishTestingObject();
				currentObjectIndex++;
				analysisProgress = (float)currentObjectIndex / objectsToTest.Count;
			}
		}

		private List<GameObject> CollectObjectsToTest()
		{
			var objects = new List<GameObject>();
			var rootObjects = originalScene.GetRootGameObjects();

			foreach (var rootObj in rootObjects)
			{
				CollectObjectsRecursive(rootObj, objects);
			}

			return objects.Where(obj => HasVisibleRenderer(obj)).ToList();
		}

		private void CollectObjectsRecursive(GameObject obj, List<GameObject> objects)
		{
			if (obj.activeInHierarchy || includeInactiveObjects)
			{
				if (obj.GetComponent<Renderer>() != null)
				{
					objects.Add(obj);
				}

				for (int i = 0; i < obj.transform.childCount; i++)
				{
					CollectObjectsRecursive(obj.transform.GetChild(i).gameObject, objects);
				}
			}
		}

		private bool HasVisibleRenderer(GameObject obj)
		{
			var renderer = obj.GetComponent<Renderer>();
			return renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy;
		}

		private void StartTestingObject(GameObject originalObject)
		{
			currentAnalyzingObject = originalObject.name;
			currentFrameCount = 0;
			currentFPSReadings.Clear();
			currentDrawCallReadings.Clear();
			currentSetPassReadings.Clear();

			// Instantiate object in test scene
			SceneManager.SetActiveScene(testScene);
			currentTestObject = Instantiate(originalObject);

			// Position object in front of camera
			currentTestObject.transform.position = Vector3.zero;

			// Set all objects to the test layer for isolated rendering
			SetObjectLayerRecursive(currentTestObject, testLayer);

			// Adjust camera distance based on object bounds
			AdjustCameraForObject(currentTestObject);

			Debug.Log($"[Scene Performance Analyzer] Testing: {originalObject.name} (isolated on layer {testLayer})");
		}

		/// <summary>
		/// Recursively sets all objects and children to the specified layer
		/// </summary>
		private void SetObjectLayerRecursive(GameObject obj, int layer)
		{
			obj.layer = layer;

			for (int i = 0; i < obj.transform.childCount; i++)
			{
				SetObjectLayerRecursive(obj.transform.GetChild(i).gameObject, layer);
			}
		}

		private void AdjustCameraForObject(GameObject obj)
		{
			var renderer = obj.GetComponent<Renderer>();
			if (renderer != null)
			{
				var bounds = renderer.bounds;
				float distance = Mathf.Max(bounds.size.magnitude * 1.5f, 2f);
				testCamera.transform.position = new Vector3(0, bounds.center.y, -distance);
				testCamera.transform.LookAt(bounds.center);
			}
		}

		private void ContinueTestingObject()
		{
			if (currentTestObject == null) return;

			// Rotate object if enabled - ensure complete 360 degree rotation over test period
			if (testWithRotation)
			{
				// Calculate rotation based on frame progress to ensure 360 degrees total
				float rotationProgress = (float)currentFrameCount / rotationFrames;
				float targetRotation = rotationProgress * 360f;
				currentTestObject.transform.rotation = Quaternion.Euler(0, targetRotation, 0);
			}

			// Force camera to render for preview update
			if (cameraPreview != null && testCamera != null)
			{
				testCamera.Render();
			}

			// Collect performance metrics
			float currentFPS = 1f / Time.unscaledDeltaTime;
			currentFPSReadings.Add(currentFPS);

			// Note: Draw calls and set pass calls would need to be collected via Unity's internal APIs
			// For now, we'll estimate based on object complexity
			int drawCalls = EstimateDrawCalls(currentTestObject);
			int setPassCalls = EstimateSetPassCalls(currentTestObject);

			currentDrawCallReadings.Add(drawCalls);
			currentSetPassReadings.Add(setPassCalls);

			currentFrameCount++;

			// Force editor window repaint to update preview
			Repaint();
		}

		private int EstimateDrawCalls(GameObject obj)
		{
			int drawCalls = 0;
			var renderers = obj.GetComponentsInChildren<Renderer>();

			foreach (var renderer in renderers)
			{
				if (renderer.enabled && renderer.gameObject.activeInHierarchy)
				{
					drawCalls += renderer.sharedMaterials.Length;
				}
			}

			return drawCalls;
		}

		private int EstimateSetPassCalls(GameObject obj)
		{
			var materials = new HashSet<Material>();
			var renderers = obj.GetComponentsInChildren<Renderer>();

			foreach (var renderer in renderers)
			{
				if (renderer.enabled && renderer.gameObject.activeInHierarchy)
				{
					foreach (var material in renderer.sharedMaterials)
					{
						if (material != null)
						{
							materials.Add(material);
						}
					}
				}
			}

			return materials.Count;
		}

		private void FinishTestingObject()
		{
			if (currentTestObject == null) return;

			var originalObject = objectsToTest[currentObjectIndex];

			// Calculate performance metrics
			var metrics = new PerformanceMetrics
			{
				averageFPS = currentFPSReadings.Count > 0 ? currentFPSReadings.Average() : 0,
				minFPS = currentFPSReadings.Count > 0 ? currentFPSReadings.Min() : 0,
				maxFPS = currentFPSReadings.Count > 0 ? currentFPSReadings.Max() : 0,
				averageDrawCalls = currentDrawCallReadings.Count > 0 ? (int)currentDrawCallReadings.Average() : 0,
				maxDrawCalls = currentDrawCallReadings.Count > 0 ? currentDrawCallReadings.Max() : 0,
				averageSetPassCalls = currentSetPassReadings.Count > 0 ? (int)currentSetPassReadings.Average() : 0,
				maxSetPassCalls = currentSetPassReadings.Count > 0 ? currentSetPassReadings.Max() : 0,
				frameTimeMS = currentFPSReadings.Count > 0 ? 1000f / currentFPSReadings.Average() : 0
			};

			// Analyze object properties
			var triangleCount = GetTriangleCount(originalObject);
			var materialCount = GetMaterialCount(originalObject);
			var hasLOD = originalObject.GetComponent<LODGroup>() != null;
			var isStatic = originalObject.isStatic;

			// Calculate performance impact and rating
			var impact = CalculatePerformanceImpact(metrics, triangleCount, materialCount);
			var rating = CalculatePerformanceRating(metrics.averageFPS, impact);

			var result = new ObjectPerformanceData
			{
				objectName = originalObject.name,
				objectPath = GetObjectPath(originalObject),
				gameObject = originalObject,
				metrics = metrics,
				triangleCount = triangleCount,
				materialCount = materialCount,
				hasLOD = hasLOD,
				isStatic = isStatic,
				performanceImpact = impact,
				rating = rating
			};

			performanceResults.Add(result);

			// Cleanup
			DestroyImmediate(currentTestObject);
			currentTestObject = null;
		}

		private int GetTriangleCount(GameObject obj)
		{
			int triangles = 0;
			var meshFilters = obj.GetComponentsInChildren<MeshFilter>();

			foreach (var meshFilter in meshFilters)
			{
				if (meshFilter.sharedMesh != null)
				{
					triangles += meshFilter.sharedMesh.triangles.Length / 3;
				}
			}

			return triangles;
		}

		private int GetMaterialCount(GameObject obj)
		{
			var materials = new HashSet<Material>();
			var renderers = obj.GetComponentsInChildren<Renderer>();

			foreach (var renderer in renderers)
			{
				foreach (var material in renderer.sharedMaterials)
				{
					if (material != null)
					{
						materials.Add(material);
					}
				}
			}

			return materials.Count;
		}

		private float CalculatePerformanceImpact(PerformanceMetrics metrics, int triangles, int materials)
		{
			float impact = 0f;

			// FPS impact (higher weight)
			if (metrics.averageFPS < 60) impact += (60 - metrics.averageFPS) * 2f;

			// Draw call impact
			impact += metrics.averageDrawCalls * 0.5f;

			// Triangle impact
			impact += (triangles / 1000f) * 0.1f;

			// Material impact
			impact += materials * 2f;

			return Mathf.Clamp(impact, 0f, 100f);
		}

		private PerformanceRating CalculatePerformanceRating(float avgFPS, float impact)
		{
			if (avgFPS >= 90 && impact < 10) return PerformanceRating.Excellent;
			if (avgFPS >= 60 && impact < 25) return PerformanceRating.Good;
			if (avgFPS >= 30 && impact < 50) return PerformanceRating.Fair;
			if (avgFPS >= 15) return PerformanceRating.Poor;
			return PerformanceRating.Critical;
		}

		private string GetObjectPath(GameObject obj)
		{
			string path = obj.name;
			Transform parent = obj.transform.parent;

			while (parent != null)
			{
				path = parent.name + "/" + path;
				parent = parent.parent;
			}

			return path;
		}

		private void QuickScan()
		{
			// Quick static analysis without creating test scene
			Debug.Log("[Scene Performance Analyzer] Performing quick scan...");

			performanceResults.Clear();
			var objects = CollectObjectsToTest();

			foreach (var obj in objects)
			{
				var triangleCount = GetTriangleCount(obj);
				var materialCount = GetMaterialCount(obj);
				var drawCalls = EstimateDrawCalls(obj);
				var setPassCalls = EstimateSetPassCalls(obj);

				// Estimate performance based on static analysis
				var estimatedFPS = EstimateFPS(triangleCount, materialCount, drawCalls);
				var impact = CalculatePerformanceImpact(new PerformanceMetrics { averageFPS = estimatedFPS, averageDrawCalls = drawCalls }, triangleCount, materialCount);

				var result = new ObjectPerformanceData
				{
					objectName = obj.name,
					objectPath = GetObjectPath(obj),
					gameObject = obj,
					metrics = new PerformanceMetrics
					{
						averageFPS = estimatedFPS,
						averageDrawCalls = drawCalls,
						averageSetPassCalls = setPassCalls
					},
					triangleCount = triangleCount,
					materialCount = materialCount,
					hasLOD = obj.GetComponent<LODGroup>() != null,
					isStatic = obj.isStatic,
					performanceImpact = impact,
					rating = CalculatePerformanceRating(estimatedFPS, impact)
				};

				performanceResults.Add(result);
			}

			performanceResults = performanceResults.OrderByDescending(r => r.performanceImpact).ToList();
			Debug.Log($"[Scene Performance Analyzer] Quick scan complete. Analyzed {performanceResults.Count} objects.");
		}

		private float EstimateFPS(int triangles, int materials, int drawCalls)
		{
			// Simple estimation based on complexity
			float complexity = (triangles / 1000f) + (materials * 5f) + (drawCalls * 2f);
			float estimatedFPS = Mathf.Clamp(120f - complexity, 5f, 120f);
			return estimatedFPS;
		}

		private void TestSelectedObjects()
		{
			var selectedObjects = Selection.gameObjects;
			if (selectedObjects.Length == 0)
			{
				EditorUtility.DisplayDialog("No Selection", "Please select objects to test.", "OK");
				return;
			}

			Debug.Log($"[Scene Performance Analyzer] Testing {selectedObjects.Length} selected objects...");

			performanceResults.Clear();
			objectsToTest = selectedObjects.Where(obj => HasVisibleRenderer(obj)).ToList();

			if (objectsToTest.Count == 0)
			{
				EditorUtility.DisplayDialog("No Renderers", "Selected objects don't have visible renderers.", "OK");
				return;
			}

			isAnalyzing = true;
			analysisProgress = 0f;
			currentObjectIndex = 0;

			EditorApplication.update += UpdateAnalysis;
			CreateTestScene();
		}

		private void CancelAnalysis()
		{
			isAnalyzing = false;
			EditorApplication.update -= UpdateAnalysis;
			CleanupTestScene();
			Debug.Log("[Scene Performance Analyzer] Analysis cancelled.");
		}

		private void FinishAnalysis()
		{
			isAnalyzing = false;
			EditorApplication.update -= UpdateAnalysis;
			CleanupTestScene();

			// Sort results by performance impact
			performanceResults = performanceResults.OrderByDescending(r => r.performanceImpact).ToList();

			Debug.Log($"[Scene Performance Analyzer] Analysis complete! Tested {performanceResults.Count} objects.");

			// Show summary
			var critical = performanceResults.Count(r => r.rating == PerformanceRating.Critical);
			var poor = performanceResults.Count(r => r.rating == PerformanceRating.Poor);

			if (critical > 0 || poor > 0)
			{
				Debug.LogWarning($"[Scene Performance Analyzer] Found {critical} critical and {poor} poor performing objects!");
			}
		}

		private void CleanupTestScene()
		{
			if (testScene.IsValid())
			{
				SceneManager.SetActiveScene(originalScene);
				EditorSceneManager.CloseScene(testScene, true);
			}

			// Clean up render texture
			if (cameraPreview != null)
			{
				cameraPreview.Release();
				DestroyImmediate(cameraPreview);
				cameraPreview = null;
			}

			currentTestObject = null;
			objectsToTest.Clear();
			currentObjectIndex = 0;
		}

		private void ExportReport()
		{
			string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
			string filename = $"ScenePerformanceReport_{timestamp}.csv";
			string path = EditorUtility.SaveFilePanel("Export Performance Report", Application.dataPath, filename, "csv");

			if (string.IsNullOrEmpty(path)) return;

			try
			{
				var csv = GenerateCSVReport();
				File.WriteAllText(path, csv);

				EditorUtility.DisplayDialog("Export Complete",
					$"Performance report exported to:\n{path}", "OK");

				Debug.Log($"[Scene Performance Analyzer] Report exported to: {path}");
			}
			catch (System.Exception e)
			{
				EditorUtility.DisplayDialog("Export Error",
					$"Failed to export report:\n{e.Message}", "OK");
			}
		}

		private string GenerateCSVReport()
		{
			var csv = new System.Text.StringBuilder();

			// Header
			csv.AppendLine("Object Name,Path,Rating,Performance Impact,Avg FPS,Min FPS,Draw Calls,Set Pass Calls,Triangles,Materials,Has LOD,Is Static");

			// Data
			foreach (var result in performanceResults)
			{
				csv.AppendLine($"{result.objectName},{result.objectPath},{result.rating},{result.performanceImpact:F1}," +
							  $"{result.metrics.averageFPS:F1},{result.metrics.minFPS:F1},{result.metrics.averageDrawCalls}," +
							  $"{result.metrics.averageSetPassCalls},{result.triangleCount},{result.materialCount}," +
							  $"{result.hasLOD},{result.isStatic}");
			}

			return csv.ToString();
		}

		void OnDestroy()
		{
			if (isAnalyzing)
			{
				CancelAnalysis();
			}
		}
	}
}