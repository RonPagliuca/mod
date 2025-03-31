using BepInEx;
using Comfort.Common;
using EFT;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

[BepInPlugin("com.tomasino.sptarkov.pantsmod", "Pants Mod", "3.11.1")]
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
    private bool showText = true;
    private Camera mainCamera;

    private void Start()
    {
        AttachToGameCanvas();
    }

    private void Update()
    {
        FindEFTCamera();

        if (mainCamera == null)
            return;

        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.M))
        {
            isEnabled = !isEnabled;
            PantsMod.LogToFile($"⚙ PantsModUpdater functionality {(isEnabled ? "enabled" : "disabled")}");
        }

        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.H))
        {
            showText = !showText;
            PantsMod.LogToFile($"⚙ PantsModUpdater text portion {(showText ? "shown" : "hidden")}");
        }

        if (!isEnabled)
        {
            foreach (var enemyMarker in enemyMarkers.Values)
                enemyMarker.SetActive(false);
            return;
        }

        if (Singleton<GameWorld>.Instance == null || Singleton<GameWorld>.Instance.MainPlayer == null)
            return;

        UpdateEnemyMarkers();
    }

    private void FindEFTCamera()
    {
        if (mainCamera != null)
            return;

        foreach (var cam in FindObjectsOfType<Camera>())
        {
            if (cam.name.Contains("FPS Camera") || cam.name.Contains("GameWorld"))
            {
                mainCamera = cam;
                return;
            }
        }
    }

    private void UpdateEnemyMarkers()
    {
        var gameWorld = Singleton<GameWorld>.Instance;
        if (gameWorld == null || gameWorld.MainPlayer == null || mainCamera == null || uiCanvas == null)
            return;

        foreach (var enemyMarker in enemyMarkers.Values)
            enemyMarker.SetActive(false);

        List<Player> enemies = gameWorld.RegisteredPlayers
            .OfType<Player>()
            .Where(player => player.AIData != null) // include alive and dead with AI
            .ToList();

        foreach (var enemy in enemies)
        {
            if (enemy == null || enemy.Transform == null)
                continue;

            Vector3 headPosition = enemy.Transform.position + Vector3.up * 1.8f;
            Vector3 viewportPosition = mainCamera.WorldToViewportPoint(headPosition);

            if (viewportPosition.z < 0)
                continue;

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
                markerRect.sizeDelta = new Vector2(8, 8);
                markerRect.anchorMin = new Vector2(0.5f, 0.5f);
                markerRect.anchorMax = new Vector2(0.5f, 0.5f);

                Image image = marker.AddComponent<Image>();
                image.color = Color.red;

                GameObject textObj = new GameObject("EnemyText");
                textObj.transform.SetParent(marker.transform, false);

                RectTransform textRect = textObj.AddComponent<RectTransform>();
                textRect.anchorMin = new Vector2(0.5f, 0.5f);
                textRect.anchorMax = new Vector2(0.5f, 0.5f);
                textRect.anchoredPosition = new Vector2(0, 20);

                Text text = textObj.AddComponent<Text>();
                text.alignment = TextAnchor.MiddleCenter;
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                text.verticalOverflow = VerticalWrapMode.Overflow;

                enemyMarkers[enemyId] = marker;
            }

            GameObject markerGO = enemyMarkers[enemyId];
            markerGO.SetActive(true);
            RectTransform rectTransform = markerGO.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = screenPosition;

            // Set marker color based on alive/dead
            var img = markerGO.GetComponent<Image>();
            img.color = enemy.HealthController.IsAlive ? Color.red : Color.yellow;

            Text enemyText = markerGO.GetComponentInChildren<Text>();

            float distance = Vector3.Distance(gameWorld.MainPlayer.Transform.position, enemy.Transform.position);
            float scaleFactor = Mathf.Clamp(80f / distance, 0.4f, 1.2f);
            float alpha = Mathf.Clamp(0.1f + (distance / 300f), 0.05f, 0.6f);
            Color textColor = (distance <= 50f) ? new Color(1f, 0.65f, 0f, alpha) : new Color(1f, 1f, 1f, alpha);

            string aiNickname = enemy.Profile.Nickname;
            string newRoleString = enemy.Profile.Info.Settings.Role switch
            {
                WildSpawnType.assault => "Scav",
                WildSpawnType.pmcBot => "Bear",
                WildSpawnType.exUsec => "USEC",
                WildSpawnType.followerBully or WildSpawnType.followerKojaniy or WildSpawnType.followerSanitar or WildSpawnType.followerTagilla => "Guard",
                WildSpawnType.bossBully or WildSpawnType.bossKojaniy or WildSpawnType.bossSanitar or WildSpawnType.bossTagilla or WildSpawnType.bossGluhar or WildSpawnType.bossKilla
                    => enemy.Profile.Info.Settings.Role.ToString(),
                WildSpawnType.sectantPriest or WildSpawnType.sectantWarrior => "Cultist",
                _ when enemy.Profile.Info.Side == EPlayerSide.Bear => "Bear",
                _ when enemy.Profile.Info.Side == EPlayerSide.Usec => "USEC",
                _ => "Scav"
            };

            string displayText = $"{newRoleString}:{aiNickname}(lvl. {enemy.Profile.Info.Level})";

            if (showText)
            {
                enemyText.enabled = true;
                enemyText.text = $"{displayText} | HP: {enemy.HealthController.GetBodyPartHealth(EBodyPart.Common).Current} | {distance:F1}m";
            }
            else
            {
                enemyText.enabled = false;
            }

            enemyText.fontSize = Mathf.RoundToInt(18 * scaleFactor);
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
