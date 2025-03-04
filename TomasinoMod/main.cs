using BepInEx; // Importing BepInEx, a plugin framework for Unity games.
using Comfort.Common; // Importing common utilities.
using EFT; // Importing Escape from Tarkov specific classes.
using UnityEngine; // Importing Unity engine classes.
using UnityEngine.UI; // Importing Unity UI classes.
using System; // Importing system utilities.
using System.IO; // Importing file input/output utilities.
using System.Linq; // Importing LINQ for collections.
using System.Collections.Generic; // Importing generic collections.

[BepInPlugin("com.tomasino.sptarkov.pantsmod", "Pants Mod", "1.0.0")]
public class PantsMod : BaseUnityPlugin
{
    private static string logFilePath = @"F:\SPT\mymod.log"; // Path to the log file.

    // Method to log messages to a file.
    public static void LogToFile(string message)
    {
        string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}"; // Format the log message with a timestamp.
        File.AppendAllText(logFilePath, logMessage + "\n"); // Append the log message to the file.
    }

    // Method called when the mod is loaded.
    private void Awake()
    {
        LogToFile("PantsMod Loaded - Mod is running!"); // Log that the mod has loaded.

        GameObject updaterObj = new GameObject("PantsModUpdater"); // Create a new GameObject for the updater.
        updaterObj.AddComponent<PantsModUpdater>(); // Add the PantsModUpdater component to the GameObject.
        DontDestroyOnLoad(updaterObj); // Prevent the GameObject from being destroyed when loading a new scene.
    }
}

public class PantsModUpdater : MonoBehaviour
{
    private GameObject uiCanvas = null; // Reference to the UI canvas.
    private Dictionary<int, GameObject> enemyMarkers = new Dictionary<int, GameObject>(); // Dictionary to store enemy markers.

    // Method called when the updater starts.
    private void Start()
    {
        PantsMod.LogToFile("PantsModUpdater started - Attaching to Game Canvas..."); // Log that the updater has started.
        AttachToGameCanvas(); // Attach the updater to the game canvas.
    }

    // Method called every frame.
    private void Update()
    {
        if (Singleton<GameWorld>.Instance == null || Singleton<GameWorld>.Instance.MainPlayer == null)
        {
            return; // If the game world or main player is not available, do nothing.
        }

        UpdateEnemyMarkers(); // Update the enemy markers.
    }

    // Method to update enemy markers.
    private void UpdateEnemyMarkers()
    {
        var gameWorld = Singleton<GameWorld>.Instance; // Get the game world instance.
        if (gameWorld == null || gameWorld.MainPlayer == null)
            return; // If the game world or main player is not available, do nothing.

        Camera mainCamera = Camera.main; // Get the main camera.
        if (mainCamera == null)
            return; // If the main camera is not available, do nothing.

        foreach (var enemyMarker in enemyMarkers.Values)
            enemyMarker.SetActive(false); // Deactivate all enemy markers.

        var enemies = gameWorld.RegisteredPlayers
            .Where(p => p.IsAI && p.HealthController.IsAlive)
            .ToList(); // Get a list of all alive AI enemies.

        foreach (var enemy in enemies)
        {
            if (enemy == null || enemy.Transform == null)
                continue; // If the enemy or its transform is not available, skip to the next enemy.

            Vector3 headPosition = enemy.Transform.position + Vector3.up * 1.8f; // Calculate the head position of the enemy.
            Vector3 viewportPos = mainCamera.WorldToViewportPoint(headPosition); // Convert the head position to viewport coordinates.

            if (viewportPos.z < 0) continue; // If the enemy is behind the camera, skip to the next enemy.

            Vector2 screenPos = new Vector2(
                (viewportPos.x - 0.5f) * Screen.width, 
                (viewportPos.y - 0.5f) * Screen.height
            ); // Convert the viewport coordinates to screen coordinates.

            int enemyId = enemy.GetHashCode(); // Get a unique ID for the enemy.
            if (!enemyMarkers.ContainsKey(enemyId))
            {
                GameObject marker = new GameObject($"EnemyMarker_{enemyId}"); // Create a new GameObject for the enemy marker.
                marker.transform.SetParent(uiCanvas.transform, false); // Set the parent of the marker to the UI canvas.

                Image image = marker.AddComponent<Image>(); // Add an Image component to the marker.
                image.color = Color.red; // Set the color of the image to red.
                image.rectTransform.sizeDelta = new Vector2(12, 12); // Set the size of the image.

                GameObject textObj = new GameObject("EnemyText"); // Create a new GameObject for the enemy text.
                textObj.transform.SetParent(marker.transform, false); // Set the parent of the text to the marker.
                Text text = textObj.AddComponent<Text>(); // Add a Text component to the text object.
                text.color = Color.white; // Set the color of the text to white.
                text.alignment = TextAnchor.MiddleCenter; // Center the text.
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf"); // Set the font of the text to Arial.

                enemyMarkers[enemyId] = marker; // Add the marker to the dictionary.
            }

            float distance = Vector3.Distance(gameWorld.MainPlayer.Transform.position, enemy.Transform.position); // Calculate the distance between the player and the enemy.
            
            // ✅ Adjust text size dynamically based on distance
            float scaleFactor = Mathf.Clamp(100f / distance, 0.6f, 1.5f); // Calculate a scale factor based on the distance.

            // ✅ Adjust transparency dynamically based on distance
            float alpha = Mathf.Clamp(1f - (distance / 200f), 0.2f, 1f); // Calculate the transparency based on the distance.

            enemyMarkers[enemyId].SetActive(true); // Activate the enemy marker.
            RectTransform rectTransform = enemyMarkers[enemyId].GetComponent<RectTransform>(); // Get the RectTransform of the marker.
            rectTransform.anchoredPosition = screenPos; // Set the position of the marker on the screen.

            Text enemyText = enemyMarkers[enemyId].GetComponentInChildren<Text>(); // Get the Text component of the marker.
            
            // Prevent text wrapping by adjusting RectTransform size
            RectTransform textRect = enemyText.GetComponent<RectTransform>(); // Get the RectTransform of the text.
            textRect.sizeDelta = new Vector2(150, 30); // ✅ Wider to prevent wrapping
            textRect.anchoredPosition = new Vector2(0, -15); // ✅ Aligns text under dot

            // Set text properties
            enemyText.text = $"{enemy.Profile.Info.Settings.Role}"; // Set the text to the enemy's role.
            enemyText.fontSize = Mathf.RoundToInt(24 * scaleFactor); // Set the font size based on the scale factor.
            enemyText.color = new Color(1f, 1f, 1f, alpha); // Set the color of the text with the calculated transparency.
            enemyText.resizeTextForBestFit = true; // ✅ Auto-scale if needed
            enemyText.resizeTextMinSize = 10; // Set the minimum font size.
            enemyText.resizeTextMaxSize = 24; // Set the maximum font size.
            enemyText.horizontalOverflow = HorizontalWrapMode.Overflow; // ✅ Prevents wrapping
            enemyText.verticalOverflow = VerticalWrapMode.Overflow; // ✅ Ensures single-line text
        }
    }

    // Method to attach the updater to the game canvas.
    private void AttachToGameCanvas()
    {
        Canvas existingCanvas = GameObject.FindObjectsOfType<Canvas>()
            .FirstOrDefault(c => c.renderMode == RenderMode.ScreenSpaceOverlay); // Find an existing canvas with ScreenSpaceOverlay render mode.

        if (existingCanvas == null)
        {
            CreateTemporaryCanvas(); // If no existing canvas is found, create a temporary canvas.
            return;
        }

        uiCanvas = existingCanvas.gameObject; // Set the UI canvas to the existing canvas.
    }

    // Method to create a temporary canvas.
    private void CreateTemporaryCanvas()
    {
        uiCanvas = new GameObject("PantsModOverlay"); // Create a new GameObject for the canvas.
        Canvas canvas = uiCanvas.AddComponent<Canvas>(); // Add a Canvas component to the GameObject.
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; // Set the render mode to ScreenSpaceOverlay.
        uiCanvas.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // Add a CanvasScaler component and set the scale mode.
        uiCanvas.AddComponent<GraphicRaycaster>(); // Add a GraphicRaycaster component.
    }
}
