#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;

[CustomEditor(typeof(PhotoSlideshowUdon))]
public class PhotoSlideshowUdonEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PhotoSlideshowUdon slideshow = (PhotoSlideshowUdon)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Photo Loading", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Load All Photos from Folder"))
        {
            LoadPhotosFromFolder(slideshow);
        }

        if (slideshow.photos != null && slideshow.photos.Length > 0)
        {
            EditorGUILayout.HelpBox($"Currently loaded: {slideshow.photos.Length} photos", MessageType.Info);
        }
    }

    private void LoadPhotosFromFolder(PhotoSlideshowUdon slideshow)
    {
        if (string.IsNullOrEmpty(slideshow.photoFolderPath))
        {
            Debug.LogError("[PhotoSlideshowUdon] Photo folder path is empty!");
            return;
        }

        string fullPath = Application.dataPath + "/" + slideshow.photoFolderPath;

        if (!Directory.Exists(fullPath))
        {
            Debug.LogError($"[PhotoSlideshowUdon] Folder not found: {fullPath}");
            return;
        }

        // Get all image files from the folder
        var imageFiles = Directory.GetFiles(fullPath)
            .Where(file => slideshow.supportedExtensions.Any(ext =>
                file.ToLower().EndsWith(ext.ToLower())))
            .OrderBy(file => file) // Sort alphabetically
            .ToArray();

        if (imageFiles.Length == 0)
        {
            Debug.LogWarning($"[PhotoSlideshowUdon] No supported image files found in: {fullPath}");
            return;
        }

        // Convert file paths to Unity asset paths and load as Texture2D
        var loadedPhotos = new List<Texture2D>();

        foreach (string filePath in imageFiles)
        {
            // Convert absolute path to relative Unity asset path
            string relativePath = "Assets" + filePath.Substring(Application.dataPath.Length);
            relativePath = relativePath.Replace('\\', '/'); // Normalize path separators

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(relativePath);
            if (texture != null)
            {
                loadedPhotos.Add(texture);
                string fileName = filePath.Substring(filePath.LastIndexOf('/') + 1);
                Debug.Log($"[PhotoSlideshowUdon] Loaded: {fileName}");
            }
            else
            {
                Debug.LogWarning($"[PhotoSlideshowUdon] Failed to load: {relativePath}");
            }
        }

        // Update the photos array
        slideshow.photos = loadedPhotos.ToArray();

        // Mark the object as dirty so Unity knows to save the changes
        EditorUtility.SetDirty(slideshow);

        Debug.Log($"[PhotoSlideshowUdon] Successfully loaded {slideshow.photos.Length} photos from folder: {slideshow.photoFolderPath}");
    }
}
#endif