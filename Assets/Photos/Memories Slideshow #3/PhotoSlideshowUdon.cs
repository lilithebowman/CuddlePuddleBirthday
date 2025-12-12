/**
 * PhotoSlideshowUdon.cs
 *
 * Assigns the images in a folder to a slideshow component.
 * Created by: Lilithe Bowman (@lilithebowman on github, @lilithelotor on vrchat)
 * Created on: 2025/12/12
 */

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;
#endif

public class PhotoSlideshowUdon : UdonSharpBehaviour
{
    [Header("Slideshow Settings")]
    [Tooltip("All the photos to display in the slideshow")]
    public Texture2D[] photos;

    [Tooltip("Single material to display photos on (leave empty to use materials array below)")]
    public Material targetMaterial;

    [Tooltip("Multiple materials to display photos on (used if targetMaterial is empty)")]
    public Material[] slideshowMaterials;

    [Tooltip("How often to change photos (in seconds)")]
    public float changeInterval = 5.0f;

    [Tooltip("Randomize the order of photos at start")]
    public bool randomizeOrder = false;

    [Tooltip("Texture property name in the material (usually '_MainTex' or '_BaseMap')")]
    public string texturePropertyName = "_MainTex";

    [Header("Distribution Settings")]
    [Tooltip("If true, each material gets all photos. If false, photos are split evenly among materials")]
    public bool allPhotosPerMaterial = false;

    [Header("Auto-Load Settings")]
    [Tooltip("Automatically load all images from this folder path (relative to Assets folder)")]
    public string photoFolderPath = "Photos/Memories Slideshow #3";

    [Tooltip("Supported image file extensions (case insensitive)")]
    public string[] supportedExtensions = { ".jpg", ".jpeg", ".png", ".tga", ".bmp" };

    // Private variables for slideshow logic
    private Texture2D[][] materialPhotos; // Photos assigned to each material
    private int[] currentPhotoIndex; // Current photo index for each material
    private float timer;
    private bool isInitialized = false;

    void Start()
    {
        InitializeSlideshow();
    }

    void Update()
    {
        if (!isInitialized) return;

        timer += Time.deltaTime;
        if (timer >= changeInterval)
        {
            timer = 0f;
            AdvanceAllSlides();
        }
    }

    private void InitializeSlideshow()
    {
        // Validate inputs
        if (photos == null || photos.Length == 0)
        {
            Debug.LogError("[PhotoSlideshowUdon] No photos assigned!");
            return;
        }

        // Setup materials - prioritize single material, then array, then auto-detect
        if (targetMaterial != null)
        {
            // Use single target material
            slideshowMaterials = new Material[] { targetMaterial };
            Debug.Log("[PhotoSlideshowUdon] Using single target material");
        }
        else if (slideshowMaterials == null || slideshowMaterials.Length == 0)
        {
            // Try to get materials from this GameObject's renderer
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null && renderer.materials.Length > 0)
            {
                slideshowMaterials = renderer.materials;
                Debug.Log($"[PhotoSlideshowUdon] Auto-assigned {slideshowMaterials.Length} materials from renderer");
            }
            else
            {
                Debug.LogError("[PhotoSlideshowUdon] No materials found! Please assign targetMaterial or slideshowMaterials, or ensure this GameObject has a Renderer with materials.");
                return;
            }
        }

        // Randomize photo order if enabled
        if (randomizeOrder)
        {
            ShufflePhotos();
        }

        // Initialize arrays
        materialPhotos = new Texture2D[slideshowMaterials.Length][];
        currentPhotoIndex = new int[slideshowMaterials.Length];

        // Distribute photos among materials
        DistributePhotos();

        // Set initial photos
        for (int i = 0; i < slideshowMaterials.Length; i++)
        {
            if (materialPhotos[i] != null && materialPhotos[i].Length > 0)
            {
                SetMaterialTexture(i, materialPhotos[i][0]);
            }
        }

        isInitialized = true;
        Debug.Log($"[PhotoSlideshowUdon] Initialized slideshow with {photos.Length} photos across {slideshowMaterials.Length} materials");
    }

    private void DistributePhotos()
    {
        if (allPhotosPerMaterial)
        {
            // Each material gets all photos
            for (int i = 0; i < slideshowMaterials.Length; i++)
            {
                materialPhotos[i] = new Texture2D[photos.Length];
                for (int j = 0; j < photos.Length; j++)
                {
                    materialPhotos[i][j] = photos[j];
                }
            }
        }
        else
        {
            // Split photos evenly among materials
            int photosPerMaterial = Mathf.CeilToInt((float)photos.Length / slideshowMaterials.Length);

            for (int i = 0; i < slideshowMaterials.Length; i++)
            {
                int startIndex = i * photosPerMaterial;
                int endIndex = Mathf.Min(startIndex + photosPerMaterial, photos.Length);
                int photoCount = endIndex - startIndex;

                if (photoCount > 0)
                {
                    materialPhotos[i] = new Texture2D[photoCount];
                    for (int j = 0; j < photoCount; j++)
                    {
                        materialPhotos[i][j] = photos[startIndex + j];
                    }
                }
                else
                {
                    materialPhotos[i] = new Texture2D[0];
                }
            }
        }
    }

    private void ShufflePhotos()
    {
        // Fisher-Yates shuffle algorithm (UdonSharp compatible)
        for (int i = photos.Length - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            Texture2D temp = photos[i];
            photos[i] = photos[randomIndex];
            photos[randomIndex] = temp;
        }
        Debug.Log("[PhotoSlideshowUdon] Photos shuffled randomly");
    }

    private void AdvanceAllSlides()
    {
        for (int i = 0; i < slideshowMaterials.Length; i++)
        {
            if (materialPhotos[i] != null && materialPhotos[i].Length > 0)
            {
                // Advance to next photo
                currentPhotoIndex[i] = (currentPhotoIndex[i] + 1) % materialPhotos[i].Length;
                SetMaterialTexture(i, materialPhotos[i][currentPhotoIndex[i]]);
            }
        }
    }

    private void SetMaterialTexture(int materialIndex, Texture2D texture)
    {
        if (materialIndex >= 0 && materialIndex < slideshowMaterials.Length &&
            slideshowMaterials[materialIndex] != null)
        {
            slideshowMaterials[materialIndex].SetTexture(texturePropertyName, texture);
        }
    }

    // Public methods for external control
    public void NextSlide()
    {
        AdvanceAllSlides();
        timer = 0f; // Reset timer
    }

    public void PauseSlideshow()
    {
        isInitialized = false;
    }

    public void ResumeSlideshow()
    {
        isInitialized = true;
        timer = 0f;
    }

    public void RestartSlideshow()
    {
        for (int i = 0; i < currentPhotoIndex.Length; i++)
        {
            currentPhotoIndex[i] = 0;
        }

        for (int i = 0; i < slideshowMaterials.Length; i++)
        {
            if (materialPhotos[i] != null && materialPhotos[i].Length > 0)
            {
                SetMaterialTexture(i, materialPhotos[i][0]);
            }
        }

        timer = 0f;
    }
}
