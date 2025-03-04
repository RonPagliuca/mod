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
    private static string logFilePath = @"F:\SPT\mymod.log";

    public static void LogToFile(string message)
    {
        string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
        File.AppendAllText(logFilePath, logMessage + "\n");
    }

    private void Awake()
    {
        LogToFile("PantsMod Loaded - Mod is running!");

        GameObject updaterObj = new GameObject("PantsModUpdater");
        updaterObj.AddComponent<PantsModUpdater>();
        DontDestroyOnLoad(updaterObj);
    }
}

public class PantsModUpdater : MonoBehaviour
{
    private GameObject uiCanvas = null;
    private Dictionary<int, GameObject> enemyMarkers = new Dictionary<int, GameObject>();

    private void Start()
    {
        PantsMod.LogToFile("PantsModUpdater started - Attaching to Game Canvas...");
        AttachToGameCanvas();
    }

    private void Update()
    {
        if (Singleton<GameWorld>.Instance == null || Singleton<GameWorld>.Instance.MainPlayer == null)
        {
            return;
        }

        UpdateEnemyMarkers();
    }

    private void UpdateEnemyMarkers()
    {
        var gameWorld = Singleton<GameWorld>.Instance;
        if (gameWorld == null || gameWorld.MainPlayer == null)
            return;

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return;

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
            Vector3 viewportPos = mainCamera.WorldToViewportPoint(headPosition);

            if (viewportPos.z < 0) continue;

            Vector2 screenPos = new Vector2(
                (viewportPos.x - 0.5f) * Screen.width, 
                (viewportPos.y - 0.5f) * Screen.height
            );

            int enemyId = enemy.GetHashCode();
            if (!enemyMarkers.ContainsKey(enemyId))
            {
                GameObject marker = new GameObject($"EnemyMarker_{enemyId}");
                marker.transform.SetParent(uiCanvas.transform, false);

                Image image = marker.AddComponent<Image>();
                image.color = Color.red;
                image.rectTransform.sizeDelta = new Vector2(12, 12);

                GameObject textObj = new GameObject("EnemyText");
                textObj.transform.SetParent(marker.transform, false);
                Text text = textObj.AddComponent<Text>();
                text.color = Color.white;
                text.alignment = TextAnchor.MiddleCenter;
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

                enemyMarkers[enemyId] = marker;
            }

            float distance = Vector3.Distance(gameWorld.MainPlayer.Transform.position, enemy.Transform.position);
            
            // ✅ Adjust text size dynamically based on distance
            float scaleFactor = Mathf.Clamp(100f / distance, 0.6f, 1.5f);

            // ✅ Adjust transparency dynamically based on distance
            float alpha = Mathf.Clamp(1f - (distance / 200f), 0.2f, 1f);

            enemyMarkers[enemyId].SetActive(true);
            RectTransform rectTransform = enemyMarkers[enemyId].GetComponent<RectTransform>();
            rectTransform.anchoredPosition = screenPos;

            Text enemyText = enemyMarkers[enemyId].GetComponentInChildren<Text>();
            enemyText.text = $"{enemy.Profile.Info.Settings.Role}";
            enemyText.fontSize = Mathf.RoundToInt(24 * scaleFactor);
            enemyText.color = new Color(1f, 1f, 1f, alpha); // ✅ Apply transparency
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
        uiCanvas.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        uiCanvas.AddComponent<GraphicRaycaster>();
    }
}
