using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.C_
{
    //  This script is intended to be used in the Unity Editor only, stored in an Editor folder to ensure it is not included in builds.
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.U2D.Sprites; // this script requires com.unity.2d.sprite package be imported to work.

/// <summary>
/// Batch Sprite Slicer
/// Use this tool to slice multiple sprites from selected textures in the project.
/// I personally like to run a search for all textures in the project
/// and then slice the ones I want to slice. I work in smallish batches of twenty or so as to not overwhelm the editor.
/// I wanted to thread the tasks, but I am understanding that the Unity API is not thread-safe,
/// so I had to use the ISpriteEditorDataProvider interface instead.
/// see https://docs.unity3d.com/Packages/com.unity.2d.sprite@latest/manual/SpriteEditorDataProvider.html
/// </summary>
public class BatchSpriteSlicer : EditorWindow
{
    /// <summary>
    /// Whether to use cell size for slicing or fixed columns/rows.
    /// * If true, cellSize will be used to determine the size of each sprite slice.
    /// * If false, columns and rows will be used instead.
    /// </summary>
    private bool useCellSize = false;

    /// <summary>
    /// Size of each cell for slicing when useCellSize is true.
    /// </summary>
    private Vector2 cellSize = new(64, 64);

    /// <summary>
    /// Number of columns and rows for slicing when useCellSize is false.
    /// </summary>
    private int columns = 12;

    /// <summary>
    /// Number of rows for slicing when useCellSize is false.
    /// </summary>
    private int rows = 8;

    /// <summary>
    /// Alignment for the pivot of the sliced sprites.
    /// </summary>
    private SpriteAlignment pivotAlignment = SpriteAlignment.BottomCenter;

    /// <summary>
    /// Whether to ignore empty rectangles when slicing.
    /// </summary>
    private bool ignoreEmptyRects = true;

    /// <summary>
    /// List of copied sprite rectangles from the last copy operation.
    /// We're now including custom physics shapes (outlines) as well
    /// </summary>
    private static List<SpriteRect> copiedRects = null;
    private static readonly Dictionary<string, List<Vector2[]>> copiedOutlines = new();

    /// <summary>
    /// Width and height of the texture from which the last copy operation was performed.
    /// </summary>
    private static int copiedTexWidth = 0;
    private static int copiedTexHeight = 0;

    /// <summary>
    /// Opens the Batch Sprite Slicer window in the Unity Editor.
    /// </summary>
    [MenuItem("Tools/Batch Sprite Slicer")] // Adds a menu item to open the window
    public static void OpenWindow()
    {
        GetWindow<BatchSpriteSlicer>("Batch Sprite Slicer"); // Opens the window with a title
    }

    /// <summary>
    /// Called when the window is drawn in the Unity Editor.
    /// </summary>
    private void OnGUI()
    {
        GUILayout.Label("Batch Sprite Slicer", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Slice multiple sprites from selected textures in the project. " +
            "You can copy/paste slice layouts, adjust pivots, or grid-slice textures in batches. " +
            "Requires the 2D Sprite package (com.unity.2d.sprite).", MessageType.Info);

        GUILayout.Space(10);
        useCellSize = EditorGUILayout.Toggle("Use Cell Size", useCellSize);

        if (useCellSize)
        {
            cellSize = EditorGUILayout.Vector2Field("Cell Size", cellSize);
            EditorGUILayout.HelpBox(
                "Set the width and height (in pixels) for each sprite cell. " +
                "The slicer will automatically determine the number of columns and rows based on the texture size.",
                MessageType.None);
        }
        else
        {
            GUILayout.Label("Grid Settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Specify the number of columns and rows to divide the texture into. " +
                "Each cell will be sized to fit the grid.", MessageType.None);
            columns = EditorGUILayout.IntField("Columns", columns);
            rows = EditorGUILayout.IntField("Rows", rows);
        }

        EditorGUILayout.Space();

        pivotAlignment = (SpriteAlignment)EditorGUILayout.EnumPopup("Pivot Alignment", pivotAlignment);
        ignoreEmptyRects = EditorGUILayout.Toggle("Ignore Empty Rects", ignoreEmptyRects);

        EditorGUILayout.Space();

        GUILayout.Label("Slice Layout Operations", EditorStyles.boldLabel);

        if (GUILayout.Button(new GUIContent(
            "Copy Rect Layout",
            "Copy the current sprite slice rectangles from the first selected texture. " +
            "You can paste this layout onto other textures of similar proportions.")))
        {
            CopySlicesFromSelected();
        }
        EditorGUILayout.HelpBox(
            "Copy the slice layout from the first selected texture. " +
            "Useful for applying the same slicing to multiple textures.", MessageType.None);

        using (new EditorGUI.DisabledScope(copiedRects == null))
        {
            if (GUILayout.Button(new GUIContent(
                "Paste Rect Layout",
                "Paste the previously copied slice layout onto all selected textures. " +
                "The layout will be scaled to fit each texture's size.")))
            {
                if (Selection.objects.Length == 0)
                {
                    Debug.LogWarning("No textures selected. Please select textures to paste slices.");
                    return;
                }
                PasteSlicesToSelected();
            }
            EditorGUILayout.HelpBox(
                "Paste the copied slice layout onto all selected textures. " +
                "The layout is automatically scaled to fit each texture.", MessageType.None);
        }

        EditorGUILayout.Space();

        GUILayout.Label("Pivot Adjustment", EditorStyles.boldLabel);

        if (GUILayout.Button(new GUIContent(
            "Adjust Pivot Of Selected Slices",
            "Set the pivot alignment for all slices in the selected textures to the value chosen above. " +
            "Warning: This will overwrite any custom pivots.")))
        {
            if (Selection.objects.Length == 0)
            {
                Debug.LogWarning("No textures selected. Please select textures to adjust pivots.");
                return;
            }
            AdjustPivotOfSelectedSlices();
        }
        EditorGUILayout.HelpBox(
            "Change the pivot alignment for all slices in the selected textures. " +
            "This will overwrite any custom pivots.", MessageType.Warning);

        EditorGUILayout.Space();

        GUILayout.Label("Batch Slicing", EditorStyles.boldLabel);

        if (GUILayout.Button(new GUIContent(
            "Slice Selected Sprites (Grid)",
            "Slice all selected textures into a grid using the settings above. " +
            "Empty cells (fully transparent) can be ignored if enabled.")))
        {
            if (Selection.objects.Length == 0)
            {
                Debug.LogWarning("No textures selected. Please select textures to slice.");
                return;
            }

            SliceSelectedSprites();
        }
        EditorGUILayout.HelpBox(
            "Slice all selected textures into a grid based on the current settings. " +
            "If 'Ignore Empty Rects' is enabled, fully transparent cells will be skipped.",
            MessageType.None);
    }

    /// <summary>
    /// Copies the sprite rectangles and custom physics outlines from the selected texture(s) in the project.    
    /// </summary>
    private void CopySlicesFromSelected()
    {
        Object[] selectedTextures = Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets);
        if (selectedTextures.Length == 0)
        {
            Debug.LogWarning("No texture selected to copy slices from.");
            return;
        }

        string path = AssetDatabase.GetAssetPath(selectedTextures[0]);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning("Texture importer not found.");
            return;
        }

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null)
        {
            Debug.LogWarning("Could not load texture asset.");
            return;
        }

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
        dataProvider.InitSpriteEditorDataProvider();

        var rects = new List<SpriteRect>(dataProvider.GetSpriteRects());
        if (rects.Count == 0)
        {
            Debug.LogWarning("No custom slices found on selected texture.");
            return;
        }

        // Store outlines using GUID as key
        copiedOutlines.Clear();
        var outlineProvider = dataProvider.GetDataProvider<ISpritePhysicsOutlineDataProvider>();
        foreach (var rect in rects)
        {
            var outlines = outlineProvider.GetOutlines(rect.spriteID);
            // Deep copy the outlines to avoid reference issues
            copiedOutlines[rect.spriteID.ToString()] = outlines != null
                ? outlines.Select(arr => arr.ToArray()).ToList()
                : new List<Vector2[]>();
        }

        copiedRects = rects;
        copiedTexWidth = texture.width;
        copiedTexHeight = texture.height;
        Debug.Log($"Copied {rects.Count} sprite rects (and outlines) from '{texture.name}' ({copiedTexWidth}x{copiedTexHeight}).");
    }

    /// <summary>
    /// Pastes the copied sprite rectangles and custom physics outlines to the selected texture(s) in the project.
    /// </summary>
    private void PasteSlicesToSelected()
    {
        if (copiedRects == null || copiedRects.Count == 0)
        {
            Debug.LogWarning("No copied slices to paste.");
            return;
        }

        Object[] selectedTextures = Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets);

        foreach (Object obj in selectedTextures)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"Skipping '{obj.name}', texture importer not found.");
                continue;
            }

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
            {
                Debug.LogWarning($"Skipping '{obj.name}', could not load texture asset.");
                continue;
            }

            int texWidth = texture.width;
            int texHeight = texture.height;

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
            dataProvider.InitSpriteEditorDataProvider();

            // Scale rects to fit new texture size
            List<SpriteRect> newRects = new();
            float scaleX = (float)texWidth / copiedTexWidth;
            float scaleY = (float)texHeight / copiedTexHeight;

            // Map from old rect GUID to new rect for outline assignment
            Dictionary<string, SpriteRect> guidToNewRect = new();

            foreach (var srcRect in copiedRects)
            {
                var r = srcRect.rect;
                var scaledRect = new Rect(
                    Mathf.RoundToInt(r.x * scaleX),
                    Mathf.RoundToInt(r.y * scaleY),
                    Mathf.RoundToInt(r.width * scaleX),
                    Mathf.RoundToInt(r.height * scaleY)
                );

                if (scaledRect.width <= 0 || scaledRect.height <= 0)
                    continue;

                SpriteRect newRect = new()
                {
                    name = srcRect.name,
                    rect = scaledRect,
                    alignment = srcRect.alignment,
                    pivot = srcRect.pivot
                };
                newRects.Add(newRect);
                guidToNewRect[srcRect.spriteID.ToString()] = newRect;
            }

            // Clear all previous rects before setting new ones
            dataProvider.SetSpriteRects(System.Array.Empty<SpriteRect>());
            dataProvider.Apply();

            dataProvider.SetSpriteRects(newRects.ToArray());
            dataProvider.Apply();

            // Set outlines (physics shapes)
            var outlineProvider = dataProvider.GetDataProvider<ISpritePhysicsOutlineDataProvider>();
            foreach (var srcRect in copiedRects)
            {
                if (!guidToNewRect.TryGetValue(srcRect.spriteID.ToString(), out var newRect))
                    continue;

                if (copiedOutlines.TryGetValue(srcRect.spriteID.ToString(), out var outlines) && outlines != null && outlines.Count > 0)
                {
                    var srcRectRect = srcRect.rect;
                    var newRectRect = newRect.rect;
                    float outlineScaleX = newRectRect.width / srcRectRect.width;
                    float outlineScaleY = newRectRect.height / srcRectRect.height;

                    List<Vector2[]> scaledOutlines = new();
                    foreach (var outline in outlines)
                    {
                        Vector2[] scaled = new Vector2[outline.Length];
                        for (int i = 0; i < outline.Length; i++)
                        {
                            scaled[i] = new Vector2(
                                outline[i].x * outlineScaleX,
                                outline[i].y * outlineScaleY
                            );
                        }
                        scaledOutlines.Add(scaled);
                    }
                    outlineProvider.SetOutlines(newRect.spriteID, scaledOutlines);
                }
            }

            dataProvider.Apply();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            Debug.Log($"Pasted {newRects.Count} slices (and outlines) to '{texture.name}' ({texWidth}x{texHeight}).");
        }
    }

    /// <summary>
    /// Adjusts the pivot of all selected sprite slices to the specified pivot alignment.
    /// </summary>
    private void AdjustPivotOfSelectedSlices()
    {
        Object[] selectedTextures = Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets); // Get all selected textures in the project

        // Check if any textures are selected to adjust the pivot
        foreach (Object obj in selectedTextures)
        {
            string path = AssetDatabase.GetAssetPath(obj); // Get the path of the selected texture
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter; // Get the TextureImporter for the selected texture

            // Check if the importer is null, which means the texture is not a valid sprite texture
            if (importer == null)
            {
                Debug.LogWarning($"Skipping '{obj.name}', texture importer not found.");
                continue;
            }

            var factory = new SpriteDataProviderFactories(); // Create a new instance of SpriteDataProviderFactories to access sprite data providers
            factory.Init(); // Initialize the factory to ensure it can provide data providers
            var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer); // Get the sprite editor data provider for the texture importer
            dataProvider.InitSpriteEditorDataProvider(); // Initialize the sprite editor data provider to access sprite data

            var rects = new List<SpriteRect>(dataProvider.GetSpriteRects()); // Get all sprite rectangles from the data provider

            // Check if there are any sprite rectangles to adjust
            if (rects.Count == 0)
            {
                Debug.LogWarning($"No slices found on '{obj.name}'.");
                continue;
            }

            // Update alignment and pivot for each rect
            for (int i = 0; i < rects.Count; i++)
            {
                rects[i].alignment = pivotAlignment;
                rects[i].pivot = GetPivotForAlignment(pivotAlignment);
            }

            // Clear all previous rects before setting new ones
            dataProvider.SetSpriteRects(rects.ToArray());
            dataProvider.Apply();

            // Import the asset to apply changes
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            // Log the number of slices adjusted and the texture details
            Debug.Log($"Adjusted pivot for {rects.Count} slices on '{obj.name}'.");
        }
    }

    /// <summary>
    /// Slices the selected textures into multiple sprites based on the specified cell size or grid settings.
    /// </summary>
    private void SliceSelectedSprites()
    {
        Object[] selectedTextures = Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets); // Get all selected textures in the project

        // Check if any textures are selected to slice
        foreach (Object obj in selectedTextures)
        {
            string path = AssetDatabase.GetAssetPath(obj); // Get the path of the selected texture
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter; // Get the TextureImporter for the selected texture

            // Check if the importer is null, which means the texture is not a valid sprite texture
            if (importer == null)
            {
                Debug.LogWarning($"Skipping '{obj.name}', texture importer not found.");
                continue;
            }

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path); // Load the texture asset from the path

            // Check if the texture is null, which means it could not be loaded
            if (texture == null)
            {
                Debug.LogWarning($"Skipping '{obj.name}', could not load texture asset.");
                continue;
            }

            // Validate texture dimensions
            int texWidth = texture.width;
            int texHeight = texture.height;

            // Check if the texture dimensions are valid for slicing
            int actualColumns, actualRows, spriteWidth, spriteHeight;

            if (useCellSize)
            {
                spriteWidth = Mathf.Max(1, Mathf.RoundToInt(cellSize.x)); // Calculate sprite width based on cell size
                spriteHeight = Mathf.Max(1, Mathf.RoundToInt(cellSize.y)); // Calculate sprite height based on cell size
                actualColumns = Mathf.Max(1, texWidth / spriteWidth); // Calculate actual columns based on texture width and sprite width
                actualRows = Mathf.Max(1, texHeight / spriteHeight); // Calculate actual rows based on texture height and sprite height
            }
            else
            {
                actualColumns = Mathf.Max(1, columns); // Use specified columns, ensuring at least 1 column
                actualRows = Mathf.Max(1, rows); // Use specified rows, ensuring at least 1 row
                spriteWidth = texWidth / actualColumns; // Calculate sprite width based on texture width and actual columns
                spriteHeight = texHeight / actualRows; // Calculate sprite height based on texture height and actual rows
            }

            var factory = new SpriteDataProviderFactories(); // Create a new instance of SpriteDataProviderFactories to access sprite data providers
            factory.Init(); // Initialize the factory to ensure it can provide data providers
            var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer); // Get the sprite editor data provider for the texture importer
            dataProvider.InitSpriteEditorDataProvider(); // Initialize the sprite editor data provider to access sprite data

            List<SpriteRect> spriteRects = new(); // Create a list to hold the sprite rectangles
                        
            string assetPath = AssetDatabase.GetAssetPath(texture); // Get the asset path of the texture
            TextureImporter texImporter = (TextureImporter)TextureImporter.GetAtPath(assetPath); // Get the TextureImporter for the texture
            bool wasReadable = texImporter.isReadable; // Check if the texture was readable
            if (!wasReadable)
            {
                texImporter.isReadable = true; // Set the texture to be readable if it wasn't already
                AssetDatabase.ImportAsset(assetPath); // Import the asset to apply changes
            }

            // Iterate through the grid to create sprite rectangles
            for (int y = 0; y < actualRows; y++)
            {
                // iterate through each column in the current row
                for (int x = 0; x < actualColumns; x++)
                {
                    int rectX = x * spriteWidth;
                    int rectY = y * spriteHeight;
                    int rectW = spriteWidth;
                    int rectH = spriteHeight;

                    // Last column/row may be smaller if texture size is not a perfect multiple
                    if (x == actualColumns - 1)
                        rectW = texWidth - rectX;
                    if (y == actualRows - 1)
                        rectH = texHeight - rectY;

                    // Clamp the rectangle dimensions to ensure they don't exceed texture bounds
                    rectW = Mathf.Clamp(rectW, 0, texWidth - rectX);
                    rectH = Mathf.Clamp(rectH, 0, texHeight - rectY);

                    // Skip empty rectangles
                    if (rectW <= 0 || rectH <= 0)
                        continue;

                    // Flip Y so row 0 is at the top
                    int flippedY = texHeight - (rectY + rectH);

                    // Create the rectangle for the sprite slice
                    Rect cellRect = new(rectX, flippedY, rectW, rectH);

                    bool isEmpty = false;

                    // Check if the rectangle is empty if ignoreEmptyRects is true
                    if (ignoreEmptyRects)
                    {
                        // Get the pixels in the rectangle area
                        // Note: This can be slow for large textures, consider optimizing if needed
                        Color[] pixels = texture.GetPixels(
                            Mathf.RoundToInt(cellRect.x),
                            Mathf.RoundToInt(cellRect.y),
                            Mathf.RoundToInt(cellRect.width),
                            Mathf.RoundToInt(cellRect.height)
                        );

                        // Check if any pixel in the rectangle has an alpha value greater than 0
                        isEmpty = true;

                        // Iterate through the pixels to check if any pixel is not fully transparent
                        foreach (var pixel in pixels)
                        {
                            if (pixel.a > 0f)
                            {
                                isEmpty = false;
                                break;
                            }
                        }
                    }

                    // If ignoreEmptyRects is true and the rectangle is empty, skip adding it
                    if (ignoreEmptyRects && isEmpty)
                        continue;

                    // Create a new SpriteRect with the calculated rectangle and specified pivot alignment
                    SpriteRect rect = new()
                    {
                        name = $"{obj.name}_{x}_{y}",
                        rect = cellRect,
                        alignment = pivotAlignment,
                        pivot = GetPivotForAlignment(pivotAlignment)
                    };

                    spriteRects.Add(rect); // Add the created rectangle to the list of sprite rectangles
                }
            }

            dataProvider.SetSpriteRects(spriteRects.ToArray()); // Set the created sprite rectangles to the data provider
            dataProvider.Apply(); // Apply the changes to the data provider

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate); // Import the asset to apply changes
        }

        // Log the completion of the batch slicing operation
        Debug.Log("Batch slicing completed using ISpriteEditorDataProvider!");
    }

    /// <summary>
    /// Gets the pivot vector for the specified sprite alignment.
    /// </summary>
    /// <param name="alignment"></param>
    /// <returns></returns>
    private Vector2 GetPivotForAlignment(SpriteAlignment alignment)
    {
        // Return the pivot vector based on the specified sprite alignment
        // The pivot vector is normalized to the range [0, 1] where (0, 0) is the bottom-left corner and (1, 1) is the top-right corner
        return alignment switch
        {
            SpriteAlignment.BottomCenter => new Vector2(0.5f, 0f),
            SpriteAlignment.Center => new Vector2(0.5f, 0.5f),
            SpriteAlignment.TopLeft => new Vector2(0f, 1f),
            SpriteAlignment.TopCenter => new Vector2(0.5f, 1f),
            SpriteAlignment.TopRight => new Vector2(1f, 1f),
            SpriteAlignment.LeftCenter => new Vector2(0f, 0.5f),
            SpriteAlignment.RightCenter => new Vector2(1f, 0.5f),
            SpriteAlignment.BottomLeft => new Vector2(0f, 0f),
            SpriteAlignment.BottomRight => new Vector2(1f, 0f),
            SpriteAlignment.Custom => new Vector2(0.5f, 0f),
            _ => new Vector2(0.5f, 0.5f),
        };
    }
}
#endif
}
