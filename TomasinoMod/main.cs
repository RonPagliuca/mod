using BepInEx;
using Comfort.Common;
using EFT;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

[BepInPlugin("com.tomasino.sptarkov.pantsmod", "Pants Mod", "1.0.0")]
public class PantsMod : BaseUnityPlugin
{
    private static string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PantsMod.log");

    public static void LogToFile(string message)
    {
        string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
        File.AppendAllText(logFilePath, logMessage + "\n");
    }

    private void Awake()
    {
        LogToFile("🟢 PantsMod Loaded - Mod is running!");

        GameObject updaterObj = new GameObject("PantsModUpdater");
        updaterObj.AddComponent<PantsModUpdater>();
        DontDestroyOnLoad(updaterObj);
    }
}

public class PantsModUpdater : MonoBehaviour
{
    private GameObject uiCanvas = null;
    private Dictionary<int, GameObject> enemyMarkers = new Dictionary<int, GameObject>();
    private bool isEnabled = true;
    private Camera mainCamera;

    private void Start()
    {
        PantsMod.LogToFile("🟢 PantsModUpdater started - Attaching to Game Canvas...");
        AttachToGameCanvas();
    }

    private void Update()
    {
        FindEFTCamera();

        if (mainCamera == null)
        {
            return;
        }

        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.M))
        {
            isEnabled = !isEnabled;
            PantsMod.LogToFile($"⚙ PantsModUpdater functionality {(isEnabled ? "enabled" : "disabled")}");
        }

        if (!isEnabled)
        {
            foreach (var enemyMarker in enemyMarkers.Values)
                enemyMarker.SetActive(false);
            return;
        }

        if (Singleton<GameWorld>.Instance == null || Singleton<GameWorld>.Instance.MainPlayer == null)
        {
            return;
        }

        UpdateEnemyMarkers();
    }

    private void FindEFTCamera()
    {
        if (mainCamera != null)
            return;

        var allCameras = FindObjectsOfType<Camera>();
        foreach (var cam in allCameras)
        {
            if (cam.name.Contains("FPS Camera") || cam.name.Contains("GameWorld"))
            {
                mainCamera = cam;
                PantsMod.LogToFile($"✅ Found EFT In-Game Camera: {mainCamera.name}");
                return;
            }
        }

        
    }

    private void UpdateEnemyMarkers()
    {
        var gameWorld = Singleton<GameWorld>.Instance;
        if (gameWorld == null || gameWorld.MainPlayer == null || mainCamera == null || uiCanvas == null)
        {
            return;
        }

        foreach (var enemyMarker in enemyMarkers.Values)
            enemyMarker.SetActive(false);

        var enemies = gameWorld.RegisteredPlayers
            .Where(p => p.IsAI && p.HealthController.IsAlive)
            .ToList();

        foreach (var enemy in enemies)
        {
            if (enemy == null || enemy.Transform == null)
                continue;

            Vector3 headPosition = enemy.Transform.position + Vector3.up * 1.8f;
            Vector3 viewportPosition = mainCamera.WorldToViewportPoint(headPosition);

            if (viewportPosition.z < 0)
            {
                continue;
            }

            // Convert viewport position to UI coordinates
            Vector2 screenPosition = new Vector2(
                (viewportPosition.x * uiCanvas.GetComponent<RectTransform>().sizeDelta.x) - (uiCanvas.GetComponent<RectTransform>().sizeDelta.x / 2),
                (viewportPosition.y * uiCanvas.GetComponent<RectTransform>().sizeDelta.y) - (uiCanvas.GetComponent<RectTransform>().sizeDelta.y / 2)
            );

            int enemyId = enemy.GetHashCode();
            if (!enemyMarkers.ContainsKey(enemyId))
            {
                GameObject marker = new GameObject($"EnemyMarker_{enemyId}");
                marker.transform.SetParent(uiCanvas.transform, false);

                RectTransform markerRect = marker.AddComponent<RectTransform>();
                markerRect.sizeDelta = new Vector2(8, 8); // ⬅ Red box size reduced by 50%
                markerRect.anchorMin = new Vector2(0.5f, 0.5f);
                markerRect.anchorMax = new Vector2(0.5f, 0.5f);

                Image image = marker.AddComponent<Image>();
                image.color = Color.red;

                GameObject textObj = new GameObject("EnemyText");
                textObj.transform.SetParent(marker.transform, false);

                RectTransform textRect = textObj.AddComponent<RectTransform>();
                textRect.anchorMin = new Vector2(0.5f, 0.5f);
                textRect.anchorMax = new Vector2(0.5f, 0.5f);
                textRect.anchoredPosition = new Vector2(0, -12); // ⬅ Moves text slightly below the red box

                Text text = textObj.AddComponent<Text>();
                text.alignment = TextAnchor.MiddleCenter;
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                text.verticalOverflow = VerticalWrapMode.Overflow;

                enemyMarkers[enemyId] = marker;
            }

            float distance = Vector3.Distance(gameWorld.MainPlayer.Transform.position, enemy.Transform.position);

            // 🔹 Adjust scale factor for smaller min/max font sizes
            float scaleFactor = Mathf.Clamp(80f / distance, 0.4f, 1.2f);  // ⬅ Reduced max font size

            // 🔹 Adjust transparency to remain slightly transparent even when close
            // 🔹 Makes text much more transparent overall and even more when close
            float alpha = Mathf.Clamp(0.1f + (distance / 300f), 0.05f, 0.6f);  


            // 🔹 Change color to orange if enemy is within 50m
            Color textColor = (distance <= 50f) ? new Color(1f, 0.65f, 0f, alpha) : new Color(1f, 1f, 1f, alpha);

            enemyMarkers[enemyId].SetActive(true);
            RectTransform rectTransform = enemyMarkers[enemyId].GetComponent<RectTransform>();
            rectTransform.anchoredPosition = screenPosition;

            Text enemyText = enemyMarkers[enemyId].GetComponentInChildren<Text>();
            enemyText.text = $"{enemy.Profile.Info.Settings.Role} | HP: {enemy.HealthController.GetBodyPartHealth(EBodyPart.Common).Current} | Lvl {enemy.Profile.Info.Level} | {distance:F1}m";
            enemyText.fontSize = Mathf.RoundToInt(18 * scaleFactor); // ⬅ Reduced font max size
            enemyText.color = textColor;


        }
    }

    private void AttachToGameCanvas()
    {
        Canvas existingCanvas = GameObject.FindObjectsOfType<Canvas>()
            .FirstOrDefault(c => c.renderMode == RenderMode.ScreenSpaceOverlay);

        if (existingCanvas == null)
        {
            CreateTemporaryCanvas();
            return;
        }

        uiCanvas = existingCanvas.gameObject;
    }

    private void CreateTemporaryCanvas()
    {
        uiCanvas = new GameObject("PantsModOverlay");
        Canvas canvas = uiCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        uiCanvas.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        uiCanvas.AddComponent<GraphicRaycaster>();
    }
}
