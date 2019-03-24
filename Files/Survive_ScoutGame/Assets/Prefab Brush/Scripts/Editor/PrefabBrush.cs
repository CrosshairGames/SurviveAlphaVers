
/*
 * 		Prefab Brush+ 
 * 		Version 1.3.1
 *		Author: Archie Andrews
 *		www.archieandrews.games
 */

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[ExecuteInEditMode]  
public class PrefabBrush : EditorWindow
{
    private PB_ActiveTab activeTab = PB_ActiveTab.PrefabPaint;
    private PB_SaveOptions activeSaveOption = PB_SaveOptions.New;

    [SerializeField]
    private PB_SaveObject activeSave;
    public PB_SaveObject loadedSave;

    //Foldout bools
    private bool showBrushSettings = true;
    private bool showObjectSettings = true;
    private bool showDebug = false;

    //Scrolls
    private Vector2 scrollPos;
    private Vector2 prefabViewScrollPos;

    //Settings variables
    private Color placeBrush = new Color(0,1,0,0.1f);
    private Color selectedTab = Color.green;

    //On Off variables
    private bool isOn = true;
	private Texture2D onButton;
	private Texture2D offButton;
    private Texture2D buttonIcon;
    private Texture2D icon;
    private GUIStyle buttonSkin;

    //Styles
    private GUIStyle style;
    private GUIStyle styleBold;
    private GUIStyle styleFold;

    //Scale
    private const float deleteButtonSize = 20;
    private const int prefabIconMinSize = 64;
    private float prefabIconScaleFactor = 1;

    //Prefab mod variables
    private PB_PrefabDisplayType prefabDisplayType;
    private GameObject newObjectForPrefabList, newObjectForParentList;
    private int roundRobinCount = 0;
    private float rotationSet = 0, scaleSet = 1;

    //SaveSettings
    private string saveName = "[NEW]PrefabBrush Save";
    private int activeSaveID;
    private List<PB_SaveObject> saves = new List<PB_SaveObject>();
    private string savePath = "";

    //Comfiration
    int comfirmationId = -1;
    string comfirmationName = "";

    //Rects
    Rect dropRect;

    //Display the window.
    [MenuItem ("Window/Prefab Brush+")]
	public static void  ShowWindow () 
	{
		GetWindow(typeof(PrefabBrush));
	}

    // Window has been selected
    void OnFocus()
    {
        // Remove delegate listener if it has previously been assigned.
        SceneView.onSceneGUIDelegate -= OnSceneGUI;

        // Add (or re-add) the delegate.
        SceneView.onSceneGUIDelegate += OnSceneGUI;
    }

    void Awake()
    {
        LoadResources();
        RefreshSaves();
    }

    #region SetUp
    private void LoadResources()
    {
        //Load textures for use in UI.
        onButton = Resources.Load("Button_On") as Texture2D;
        offButton = Resources.Load("Button_Off") as Texture2D;
        buttonIcon = Resources.Load("Button_On") as Texture2D;

        icon = Resources.Load("Icon") as Texture2D;
    }
    #endregion

    #region GUI
    void OnGUI()
    {
        SetStyles();
        EditorGUILayout.BeginVertical();

        EditorGUILayout.BeginHorizontal();
        DrawHeader();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        SetTabColour(PB_ActiveTab.PrefabPaint);
        if (GUILayout.Button("Prefab Paint Brush"))
            SetActiveTab(PB_ActiveTab.PrefabPaint);

        SetTabColour(PB_ActiveTab.Saves);
        if (GUILayout.Button("Saved Brushes"))
        {
            SetActiveTab(PB_ActiveTab.Saves);
            SetSaveOption(PB_SaveOptions.Open);
        }

        SetTabColour(PB_ActiveTab.Settings);
        if (GUILayout.Button("Settings"))
            SetActiveTab(PB_ActiveTab.Settings);

        GUI.color = Color.white;

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginVertical();
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(position.height - 90));

        //Draw core UI
        switch (activeTab)
        {
            case PB_ActiveTab.PrefabPaint:
                DrawPrefabPaintTab();
                break;
            case PB_ActiveTab.Settings:
                DrawSettingsTab();
                break;
            case PB_ActiveTab.About:
                DrawAboutTab();
                break;
            case PB_ActiveTab.Saves:
                DrawSavesTab();
                break;
        }

        //End the scroll window.
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndVertical();

        //Keep the active data saved over play mode
        if(activeSave != null)
            EditorUtility.SetDirty(activeSave);

        if(loadedSave != null)
            EditorUtility.SetDirty(loadedSave);
    }

    private void SetStyles()
    {
        style = EditorStyles.label;
        styleBold = EditorStyles.boldLabel;
        styleFold = EditorStyles.foldout;
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(icon, GUILayout.Width(30), GUILayout.Height(30));
        GUILayout.Label("Prefab Brush+", styleBold);
        EditorGUILayout.EndHorizontal();

        SetTabColour(PB_ActiveTab.About);
        if (GUILayout.Button("About"))
            SetActiveTab(PB_ActiveTab.About);
    }

    //Draw tabs
    private void DrawPrefabPaintTab()
    {
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(buttonIcon, GUI.skin.label))
        {
            isOn = !isOn;
            buttonIcon = (isOn) ? onButton : offButton;
        }
        EditorGUILayout.EndHorizontal();

        Repaint();

        EditorGUILayout.BeginHorizontal("box");
        DrawNewButton();
        DrawOpenButton();
        DrawSaveButton(activeSaveID);
        DrawSaveAsButton();
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        if (isOn)
        {
            EditorGUILayout.Space();
            showBrushSettings = EditorGUILayout.Foldout(showBrushSettings, "Brush Settings", styleFold);

            if (showBrushSettings)
            {
                EditorGUILayout.BeginVertical("box");

                DrawPrefabDisplay();

                DrawBrushSizeSlider();
                DrawPrefabPerStrokeSlider();

                DrawLayerToBrush();
                DrawTagToBrush();
                DrawSlopAngleToBrush();

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space();

            showObjectSettings = EditorGUILayout.Foldout(showObjectSettings, "Object Settings", styleFold);

            if (showObjectSettings)
            {
                EditorGUILayout.BeginVertical("box");
                DrawOffsetCenter();
                DrawOffsetRotation();
                DrawParentOptions();
                DrawMatchSurface();
                DrawCustomRotation();
                DrawCustomScale();
                EditorGUILayout.EndVertical();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Prefab Brush Is Off", MessageType.Warning);
        }
    }

    private void DrawAboutTab()
    {
        GUILayout.Label("About the Prefab Brush+", styleBold);
        EditorGUILayout.HelpBox("Created by Archie Andrews www.archieandrews.games", MessageType.Info);
        EditorGUILayout.HelpBox("Feel free to contact support@archieandrews.games with support inquiries.", MessageType.Info);
    }

    private void DrawSavesTab()
    {
        EditorGUILayout.BeginHorizontal("box");
        DrawNewButton();
        DrawOpenButton();
        DrawSaveButton(activeSaveID);
        DrawSaveAsButton();
        DrawRefreshButton();
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        switch (activeSaveOption)
        {
            case PB_SaveOptions.New:
                DrawNewSaveFile();
                break;
            case PB_SaveOptions.Open:
                DrawSaveList();
                break;
            case PB_SaveOptions.Save:
                break;
            case PB_SaveOptions.SaveAs:
                DrawSaveAsWindow();
                break;
            case PB_SaveOptions.ComfirationOverwrite:
                DrawSaveAsComfirmationWindow();
                break;
            case PB_SaveOptions.ComfirmationDelete:
                DrawComfirationDeleteWindow();
                break;
            case PB_SaveOptions.ComfirmationOpen:
                DrawComfirationOpenWindow();
                break;
        }
    }

    private void DrawSettingsTab()
    {
        showDebug = EditorGUILayout.Foldout(showDebug, "Debug");

        if (showDebug)
        {
            GUILayout.Label(string.Format("Active Save ID: {0}", activeSaveID.ToString()));

            if(activeSaveID != -1)
                GUILayout.Label(string.Format("Active Save Name: {0}", saves[activeSaveID].name));

            GUILayout.Label(string.Format("Tag Mask: {0}", activeSave.requiredTagMask));
            GUILayout.Label(string.Format("Layer Mask: {0}", activeSave.requiredLayerMask));

            if (GUILayout.Button("Open Active Save"))
                Selection.activeObject = activeSave;
        }
    }

    //Draw Prefab Display
    private void DrawPrefabDisplay()
    {
        if (activeSave == null)
            CreateEmptySave();

        EditorGUILayout.LabelField("Prefab Display", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginVertical();
        GUILayout.Label("Prefab Display Settings");

        prefabDisplayType = (PB_PrefabDisplayType)EditorGUILayout.EnumPopup("Prefab Display Type", prefabDisplayType);

        if (prefabDisplayType == PB_PrefabDisplayType.Icon)
            prefabIconScaleFactor = EditorGUILayout.Slider("Prefab Scale", prefabIconScaleFactor, 1, 2);

        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginHorizontal();

        switch (prefabDisplayType)
        {
            case PB_PrefabDisplayType.Icon:
                if (DragAndDrop.paths.Length > 0 || activeSave.prefabList.Count == 0)
                    DrawDragWindow();
                else
                    DrawPrefabIconWindow();
                break;
            case PB_PrefabDisplayType.List:
                DrawPrefabList();
                break;
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        if (Event.current.type == EventType.DragUpdated && dropRect.Contains(Event.current.mousePosition))
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            Event.current.Use();
        }

        if (Event.current.type == EventType.DragPerform && dropRect.Contains(Event.current.mousePosition))
            AddPrefab(DragAndDrop.objectReferences);
    }

    private void DrawPrefabList()
    {
        EditorGUILayout.BeginVertical();
        for (int i = 0; i < activeSave.prefabList.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            DrawPrefabListItem(i);
            EditorGUILayout.EndHorizontal();
        }

        newObjectForPrefabList = EditorGUILayout.ObjectField(newObjectForPrefabList, typeof(GameObject), false) as GameObject;

        if(newObjectForPrefabList != null)
        {
            activeSave.prefabList.Add(newObjectForPrefabList);
            newObjectForPrefabList = null;
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawPrefabListItem(int i)
    {
        activeSave.prefabList[i] = EditorGUILayout.ObjectField(activeSave.prefabList[i], typeof(GameObject), false) as GameObject;
        GUI.color = Color.red;
        if (GUILayout.Button("X"))
            activeSave.prefabList.RemoveAt(i);
        GUI.color = Color.white;
    }

    private void DrawPrefabIconWindow()
    {
        int coloumnCount = Mathf.FloorToInt((position.width - GetPrefabIconSize()) / GetPrefabIconSize());
        int rowCount = Mathf.CeilToInt(activeSave.prefabList.Count / coloumnCount);

        EditorGUILayout.BeginVertical(); //Begin the window with all the prefabs in it
        prefabViewScrollPos = EditorGUILayout.BeginScrollView(prefabViewScrollPos, GUILayout.Height(GetPrefabIconSize() * 1.5f)); //Start the scroll view 
        int id = 0; //This counts how many prefab icons have been built
        for (int y = 0; y <= rowCount; y++)
        {
            EditorGUILayout.BeginHorizontal();//Start a new row
            for (int x = 0; x < coloumnCount; x++)
            {
                if (id >= activeSave.prefabList.Count) //If there are no more prefabs to add icons for then break
                    break;

                if (activeSave.prefabList[id] != null)
                    DrawPrefabWindow(id);
                else
                    activeSave.prefabList.RemoveAt(id);

                id++;
            }
            GUILayout.FlexibleSpace();//Push all of the prefab icons to the left
            EditorGUILayout.EndHorizontal();//End the row
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawDragWindow()
    {
        GUI.color = Color.green;
        dropRect = EditorGUILayout.BeginVertical("box", GUILayout.Height(GetPrefabIconSize() * 1.5f));
        GUI.color = Color.white;
        GUILayout.FlexibleSpace();
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label("Drag And Drop Here To Add Prefabs To The List", styleBold);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndVertical();
    }

    private void DrawPrefabWindow(int id)
    {
        if (id < activeSave.prefabList.Count) //Null check for when deleting prefabs.
        {
            GameObject prefab = activeSave.prefabList[id];
            EditorGUILayout.BeginVertical();
            GUILayout.Box(AssetPreview.GetAssetPreview(prefab) as Texture2D, GUILayout.Width(GetPrefabIconSize()), GUILayout.Height(GetPrefabIconSize()));

            Rect prefabIconRect = GUILayoutUtility.GetLastRect();

            prefabIconRect.x = prefabIconRect.x + (prefabIconRect.width - deleteButtonSize);
            prefabIconRect.height = deleteButtonSize;
            prefabIconRect.width = deleteButtonSize;

            GUI.color = Color.red;
            if (GUI.Button(prefabIconRect, "X"))
                activeSave.prefabList.Remove(prefab);

            GUI.color = Color.white;

            EditorGUILayout.EndVertical();
        }
    }

    //Draw Brush Settings Display
    private void DrawBrushSizeSlider()
    {
        EditorGUILayout.Space();
        //Define radius of the brush.
        GUILayout.Label("Brush Size", style);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();
        //activeSave.brushSize = GUILayout.HorizontalSlider(activeSave.brushSize, 1, activeSave.maxBrushSize);
        activeSave.brushSize = EditorGUILayout.Slider(activeSave.brushSize, activeSave.minBrushSize, activeSave.maxBrushSize);
        EditorGUILayout.EndHorizontal();
        GUILayout.Label("Max Slider Size");
        activeSave.maxBrushSize = EditorGUILayout.FloatField(activeSave.maxBrushSize);
        EditorGUILayout.EndVertical();
    }

    private void DrawPrefabPerStrokeSlider()
    {
        EditorGUILayout.Space();
        //Define how dense the objects are brushed.
        GUILayout.Label("Prefabs Per Stroke", style);
        EditorGUILayout.BeginHorizontal("box");
        activeSave.prefabsPerStroke = EditorGUILayout.IntSlider(activeSave.prefabsPerStroke, activeSave.minObjectsPerBrush, activeSave.maxObjectsPerBrush);
        EditorGUILayout.EndHorizontal();
    }

    //Draw Checks Display
    private void DrawLayerToBrush()
    {
        EditorGUILayout.Space();
        //Define the layer that the brush will brush objects on.
        GUILayout.Label("Layer To Brush", style);

        EditorGUILayout.BeginHorizontal("box");

        EditorGUILayout.BeginVertical();
        activeSave.checkLayer = EditorGUILayout.Toggle(activeSave.checkLayer);

        if (activeSave.checkLayer)
        {
            EditorGUILayout.Space();
            activeSave.requiredLayerMask = EditorGUILayout.MaskField(activeSave.requiredLayerMask, UnityEditorInternal.InternalEditorUtility.layers);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawTagToBrush()
    {
        EditorGUILayout.Space();
        //Define the object tag that the brush will brush objects on.
        GUILayout.Label("Tag To Brush", style);

        EditorGUILayout.BeginHorizontal("box");

        EditorGUILayout.BeginVertical();
        activeSave.checkTag = EditorGUILayout.Toggle(activeSave.checkTag);

        if (activeSave.checkTag)
        {
            EditorGUILayout.Space();
            activeSave.requiredTagMask = EditorGUILayout.MaskField(activeSave.requiredTagMask, UnityEditorInternal.InternalEditorUtility.tags);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawSlopAngleToBrush()
    {
        EditorGUILayout.Space();
        GUILayout.Label("Slope Angle To Brush", style);

        EditorGUILayout.BeginHorizontal("box");

        EditorGUILayout.BeginVertical();
        activeSave.checkSlope = EditorGUILayout.Toggle(activeSave.checkSlope);

        if (activeSave.checkSlope)
        {
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal("box");
            GUILayout.Label(string.Format("Min Angle = {0} | Max Angle = {1}", Mathf.Round(activeSave.minRequiredSlope * 100f) / 100f, Mathf.Round(activeSave.maxRequiredSlope * 100f) / 100f), style);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.MinMaxSlider(ref activeSave.minRequiredSlope, ref activeSave.maxRequiredSlope, 0, 90);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    //Object Settings Display
    private void DrawOffsetCenter()
    {
        GUILayout.Label("Offset Center Of Prefab", style);
        EditorGUILayout.BeginVertical("box");
        activeSave.prefabOriginOffset = EditorGUILayout.Vector3Field("", activeSave.prefabOriginOffset);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }

    private void DrawOffsetRotation()
    {
        GUILayout.Label("Offset Rotation Of Prefab", style);
        EditorGUILayout.BeginVertical("box");
        activeSave.prefabRotationOffset = EditorGUILayout.Vector3Field("", activeSave.prefabRotationOffset);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }

    private void DrawParentOptions()
    {
        GUILayout.Label("Brushed Objects Parent Settings", style);
        EditorGUILayout.BeginVertical("box");
        activeSave.parentingStyle = (PB_ParentingStyle)EditorGUILayout.EnumPopup(activeSave.parentingStyle);

        switch (activeSave.parentingStyle)
        {
            case PB_ParentingStyle.Surface:
                EditorGUILayout.HelpBox("Prefabs painted will now parent them selfs to the surface they are painted on.", MessageType.Info);
                break;
            case PB_ParentingStyle.SingleParent:
                activeSave.parent = EditorGUILayout.ObjectField(activeSave.parent, typeof(GameObject), true) as GameObject;
                break;
            case PB_ParentingStyle.ClosestFromList:
                DrawParentList();
                break;
            case PB_ParentingStyle.RoundRobin:
                DrawParentList();
                break;
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawMatchSurface()
    {
        GUILayout.Label("Rotate GameObjects to Match Surface", style);
        EditorGUILayout.BeginVertical("box");
        activeSave.rotateToMatchSurface = EditorGUILayout.Toggle(activeSave.rotateToMatchSurface);
        if (activeSave.rotateToMatchSurface)
        {
            activeSave.rotateSurfaceDirection = (PB_Direction)EditorGUILayout.EnumPopup(activeSave.rotateSurfaceDirection);
            EditorGUILayout.HelpBox("If your GameObjects aren't facing the correct direction Try changing the direction listed above.", MessageType.Info);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawCustomRotation()
    {
        GUILayout.Label("Customize Rotation", style);
        EditorGUILayout.BeginVertical("box");
        activeSave.randomizeRotation = EditorGUILayout.Toggle(activeSave.randomizeRotation);

        if (activeSave.randomizeRotation)
        {
            EditorGUILayout.BeginHorizontal(); 

            EditorGUILayout.BeginVertical("box"); 
            GUILayout.Label("Axis");
            GUILayout.Label("X");
            GUILayout.Label("Y");
            GUILayout.Label("Z");
            EditorGUILayout.EndVertical();

            //Min
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Minimum Rotation");
            activeSave.minXRotation = Mathf.Clamp(EditorGUILayout.FloatField(activeSave.minXRotation), 0, 360);
            activeSave.minYRotation = Mathf.Clamp(EditorGUILayout.FloatField(activeSave.minYRotation), 0, 360);
            activeSave.minZRotation = Mathf.Clamp(EditorGUILayout.FloatField(activeSave.minZRotation), 0, 360);
            EditorGUILayout.EndVertical(); 

            //Min
            EditorGUILayout.BeginVertical("box"); 
            GUILayout.Label("Maximum Rotation");
            activeSave.maxXRotation = Mathf.Clamp(EditorGUILayout.FloatField(activeSave.maxXRotation), 0, 360);
            activeSave.maxYRotation = Mathf.Clamp(EditorGUILayout.FloatField(activeSave.maxYRotation), 0, 360);
            activeSave.maxZRotation = Mathf.Clamp(EditorGUILayout.FloatField(activeSave.maxZRotation), 0, 360);
            EditorGUILayout.EndVertical(); 

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal("box");
            if (GUILayout.Button("Set all to: "))
            {
                activeSave.minXRotation = rotationSet;
                activeSave.minYRotation = rotationSet;
                activeSave.minZRotation = rotationSet;
                activeSave.maxXRotation = rotationSet;
                activeSave.maxYRotation = rotationSet;
                activeSave.maxZRotation = rotationSet;
            }

            rotationSet = EditorGUILayout.FloatField(rotationSet);
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawCustomScale()
    {
        GUILayout.Label("Customize Scale", style);
        EditorGUILayout.BeginVertical("box");
        activeSave.randomizeScale = EditorGUILayout.Toggle(activeSave.randomizeScale);

        if (activeSave.randomizeScale)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Axis");
            GUILayout.Label("X");
            GUILayout.Label("Y");
            GUILayout.Label("Z");
            EditorGUILayout.EndVertical();

            //Min
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Minimum Scale");
            activeSave.minXScale = EditorGUILayout.FloatField(activeSave.minXScale);
            activeSave.minYScale = EditorGUILayout.FloatField(activeSave.minYScale);
            activeSave.minZScale = EditorGUILayout.FloatField(activeSave.minZScale);
            EditorGUILayout.EndVertical();

            //Min
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Maximum Scale");
            activeSave.maxXScale = EditorGUILayout.FloatField(activeSave.maxXScale);
            activeSave.maxYScale = EditorGUILayout.FloatField(activeSave.maxYScale);
            activeSave.maxZScale = EditorGUILayout.FloatField(activeSave.maxZScale);
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal("box");
            if (GUILayout.Button("Set all to: "))
            {
                activeSave.minXScale = scaleSet;
                activeSave.minYScale = scaleSet;
                activeSave.minZScale = scaleSet;
                activeSave.maxXScale = scaleSet;
                activeSave.maxYScale = scaleSet;
                activeSave.maxZScale = scaleSet;
            }

            scaleSet = EditorGUILayout.FloatField(scaleSet);

            EditorGUILayout.EndHorizontal();
            DrawScaleError();

        }
        EditorGUILayout.EndVertical();
    }

    private void DrawScaleError()
    {
        if (activeSave.minXScale == 0)
            EditorGUILayout.HelpBox("Minimum Scale X is equal to 0 this can cause issues with the prefab", MessageType.Error);

        if (activeSave.minYScale == 0)
            EditorGUILayout.HelpBox("Minimum Scale Y is equal to 0 this can cause issues with the prefab", MessageType.Error);

        if (activeSave.minZScale == 0)
            EditorGUILayout.HelpBox("Minimum Scale Z is equal to 0 this can cause issues with the prefab", MessageType.Error);

        if (activeSave.maxXScale == 0)
            EditorGUILayout.HelpBox("Maximum Scale X is equal to 0 this can cause issues with the prefab", MessageType.Error);

        if (activeSave.maxYScale == 0)
            EditorGUILayout.HelpBox("Maximum Scale Y is equal to 0 this can cause issues with the prefab", MessageType.Error);

        if (activeSave.maxZScale == 0)
            EditorGUILayout.HelpBox("Maximum Scale Z is equal to 0 this can cause issues with the prefab", MessageType.Error);
    }

    private void DrawParentList()
    {
        EditorGUILayout.BeginVertical();
        for (int i = 0; i < activeSave.parentList.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            DrawParentListItem(i);
            EditorGUILayout.EndHorizontal();
        }

        newObjectForParentList = EditorGUILayout.ObjectField(newObjectForParentList, typeof(GameObject), true) as GameObject;

        if (newObjectForParentList != null)
        {
            activeSave.parentList.Add(newObjectForParentList);
            newObjectForParentList = null;
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawParentListItem(int i)
    {
        activeSave.parentList[i] = EditorGUILayout.ObjectField(activeSave.parentList[i], typeof(GameObject), true) as GameObject;
        GUI.color = Color.red;
        if (GUILayout.Button("X"))
            activeSave.parentList.RemoveAt(i);
        GUI.color = Color.white;
    }

    //Draw Save Buttons
    private void DrawNewButton()
    {
        if (GUILayout.Button("New"))
        {
            SetActiveTab(PB_ActiveTab.Saves);
            SetSaveOption(PB_SaveOptions.New);
        }
    }

    private void DrawOpenButton()
    {
        if (GUILayout.Button("Open"))
        {
            SetActiveTab(PB_ActiveTab.Saves);
            SetSaveOption(PB_SaveOptions.Open);
        }
    }

    private void DrawSaveButton(int i)
    {
        if (GUILayout.Button("Save"))
        {
            if (activeSaveID != -1)
                SaveAs(saves[activeSaveID].name, true, false);
            else
            {
                SetActiveTab(PB_ActiveTab.Saves);
                SetSaveOption(PB_SaveOptions.SaveAs);
            }
        }
    }

    private void DrawSaveAsButton()
    {
        if (GUILayout.Button("Save As"))
        {
            SetActiveTab(PB_ActiveTab.Saves);
            SetSaveOption(PB_SaveOptions.SaveAs);
        }
    }

    private void DrawRefreshButton()
    {
        if (GUILayout.Button("Refresh Save List"))
            RefreshSaves();
    }

    private void DrawDeleteSaveButton(int i)
    {
        GUI.color = Color.red;
        if (GUILayout.Button("X"))
        {
            StoreOpenAndDeleteComfirationInfo(saves[i].name, i);
            SetSaveOption(PB_SaveOptions.ComfirmationDelete);
        }

        GUI.color = Color.white;
    }

    //Draw Save Brush Stuff
    private void DrawSaveList()
    {
        EditorGUILayout.BeginVertical();
        for (int i = 0; i < saves.Count; i++)
        {
            DrawSaveItem(i);
            EditorGUILayout.Space();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawSaveItem(int i)
    {
        EditorGUILayout.BeginVertical("box");

        //Banner Start
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(string.Format("{0} {1}", saves[i].name, (CheckIfSaveHasChanged() ? "*" : "")));
        GUILayout.FlexibleSpace();
        DrawDeleteSaveButton(i);
        EditorGUILayout.EndHorizontal();
        //Banner End

        EditorGUILayout.BeginHorizontal();
        GUI.color = (activeSaveID == i) ? Color.green : Color.white;
        if (GUILayout.Button("Load File"))
        {
            StoreOpenAndDeleteComfirationInfo(saves[i].name, i);
            SetActiveTab(PB_ActiveTab.Saves);
            SetSaveOption(PB_SaveOptions.ComfirmationOpen);
        }

        GUI.color = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawNewSaveFile()
    {
        GUILayout.Label("New Save Name");
        saveName = EditorGUILayout.TextField(saveName);

        EditorGUILayout.Space();

        if (GUILayout.Button(string.Format("Create New Save Called {0}", saveName), GUILayout.Height(50)))
        {
            SaveAs(saveName, false, true);
            SetActiveTab(PB_ActiveTab.PrefabPaint);
        }
    }

    //Draw Save Windows
    private void DrawSaveAsWindow()
    {
        GUILayout.Label(string.Format("Save As ({0})", saveName));
        saveName = EditorGUILayout.TextField(saveName);

        EditorGUILayout.Space();

        if (GUILayout.Button(string.Format("Save {0} To New File", saveName), GUILayout.Height(50)))
        {
            if (GetSaveNames().Contains(saveName))
            {
                StoreSaveAsComfirationInfo(saveName);
                SetActiveTab(PB_ActiveTab.Saves);
                SetSaveOption(PB_SaveOptions.ComfirationOverwrite);
            }
            else
            {
                SaveAs(saveName, true, true);
                SetActiveTab(PB_ActiveTab.Saves);
                SetSaveOption(PB_SaveOptions.Open);
            }
        }
    }

    private void DrawSaveAsComfirmationWindow()
    {
        EditorGUILayout.HelpBox(string.Format("Are you sure you want to overwrite {0}. The all data saved to this file will be lost and replaced with the new data", comfirmationName), MessageType.Info);

        EditorGUILayout.BeginHorizontal();

        GUI.color = Color.red;
        if (GUILayout.Button("Yes Overwrite"))
        {
            SaveAs(comfirmationName, true, true);
            SetActiveTab(PB_ActiveTab.Saves);
            SetSaveOption(PB_SaveOptions.Open);
        }

        GUI.color = Color.white;

        if (GUILayout.Button("No Make A New File"))
        {
            SaveAs(comfirmationName, false, true);
            SetActiveTab(PB_ActiveTab.Saves);
            SetSaveOption(PB_SaveOptions.Open);
        }

        if (GUILayout.Button("No Go Back"))
        {
            SetActiveTab(PB_ActiveTab.Saves);
            SetSaveOption(PB_SaveOptions.Open);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawComfirationDeleteWindow()
    {
        EditorGUILayout.HelpBox(string.Format("Are you sure you want to delete {0}. This can't be undone.", comfirmationName), MessageType.Info);

        EditorGUILayout.BeginHorizontal();

        GUI.color = Color.red;
        if (GUILayout.Button("Yes Delete"))
        {
            DeleteSave(comfirmationId);
            SetActiveTab(PB_ActiveTab.Saves);
            SetSaveOption(PB_SaveOptions.Open);
        }

        GUI.color = Color.white;

        if (GUILayout.Button("No Keep Save"))
        {
            SetActiveTab(PB_ActiveTab.Saves);
            SetSaveOption(PB_SaveOptions.Open);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawComfirationOpenWindow()
    {
        EditorGUILayout.HelpBox(string.Format("Are you sure you want to open {0}. Any unsaved data will be lost", comfirmationName), MessageType.Info);

        EditorGUILayout.BeginHorizontal();

        GUI.color = Color.green;
        if (GUILayout.Button("Yes Open"))
        {
            LoadSave(comfirmationId);
            SetActiveTab(PB_ActiveTab.Saves);
            SetSaveOption(PB_SaveOptions.Open);
        }

        GUI.color = Color.white;

        if (GUILayout.Button("No Don't Open"))
        {
            SetActiveTab(PB_ActiveTab.Saves);
            SetSaveOption(PB_SaveOptions.Open);
        }

        EditorGUILayout.EndHorizontal();
    }

    //Draw other
    private void DrawPaintCircle()
    {
        Ray drawPointRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        RaycastHit drawPointHit;

        if (Physics.Raycast(drawPointRay, out drawPointHit))
        {
            Handles.color = placeBrush;
            Handles.DrawSolidDisc(drawPointHit.point, Vector3.up, activeSave.brushSize / 2);
            SceneView.RepaintAll();
        }
    }
    #endregion

    #region Methods
    private void AddPrefab(Object[] objectsToAdd)
    {
        for (int i = 0; i < objectsToAdd.Length; i++)
        {
            if (objectsToAdd[i].GetType() == typeof(GameObject))
                activeSave.prefabList.Add(objectsToAdd[i] as GameObject);
        }
    }

    private void RunPrefabPaint()
    {
        //If the placment brush is selected and the mouse is being dragged across the scene view.
        bool isMouseEventCorrect = ((Event.current.type == EventType.MouseDrag || Event.current.type == EventType.MouseDown) && Event.current.button == 0);
        if (isMouseEventCorrect && IsTabActive(PB_ActiveTab.PrefabPaint))
        {
            GameObject selectedObject = GetRandomObject(); //Assign random object

            if (selectedObject != null)
            {
                //Run the placment multiple times per frame to provide a opacity.
                for (int i = 0; i < activeSave.prefabsPerStroke; i++)
                {
                    //Calculate the radius of the brush size.
                    float newBrushSize = activeSave.brushSize * .5f;

                    //Create a raycast that will come from the top of the world down onto a random point within the brush size raduis calculated above.
                    Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                    RaycastHit hit;

                    if (Physics.Raycast(new Vector3(ray.origin.x + Random.insideUnitSphere.x * newBrushSize, ray.origin.y, ray.origin.z + Random.insideUnitSphere.z * newBrushSize), ray.direction, out hit))
                    {
                        if (CanBrush(hit)) //If the brush result come back as true then start brushing
                        {
                            GameObject clone = PrefabUtility.InstantiatePrefab(selectedObject) as GameObject;

                            if (clone != null)
                            {
                                ApplyModifications(clone, hit);     //Apply prefabs mods
                                Undo.RegisterCreatedObjectUndo(clone, "brush stroke: " + clone.name);       //Store the undo variables.
                            }
                        }
                    }
                }
            }
            else
                Debug.LogError("There is no object selected in the Level tool window, please drag a prefab into the area to use the placment brush.");
        }
    }

    private bool CanBrush(RaycastHit surfaceHit)
    {
        if (activeSave.checkTag)
        {
            string[] tags = GetTagsFromMask(activeSave.requiredTagMask);

            bool foundTag = false;
            for (int i = 0; i < tags.Length; i++)
            {
                if (tags[i] == surfaceHit.collider.tag)
                {
                    foundTag = true;
                    break;
                }
            }

            if (foundTag == false)
                return false;
        }

        if (activeSave.checkLayer)
        {
            string[] layers = GetTagsFromLayer(activeSave.requiredLayerMask);

            bool foundLayer = false;
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i] == LayerMask.LayerToName(surfaceHit.collider.gameObject.layer))
                {
                    foundLayer = true;
                    break;
                }
            }

            if (foundLayer == false)
                return false;
        }

        if (activeSave.checkSlope)
        {
            float angle = (Vector3.Angle(surfaceHit.normal, Vector3.up));
            if (angle > activeSave.maxRequiredSlope || angle < activeSave.minRequiredSlope)
                return false;
        }

        return true;
    }

    private void ApplyModifications(GameObject objectToMod, RaycastHit hitRef)
    {
        objectToMod.transform.position = new Vector3(hitRef.point.x + activeSave.prefabOriginOffset.x, hitRef.point.y + activeSave.prefabOriginOffset.y, hitRef.point.z + activeSave.prefabOriginOffset.z);
        objectToMod.transform.eulerAngles += activeSave.prefabRotationOffset;

        switch (activeSave.parentingStyle)
        {
            case PB_ParentingStyle.Surface:

                objectToMod.transform.parent = hitRef.collider.transform;
                break;

            case PB_ParentingStyle.SingleParent:

                if (activeSave.parent != null)
                    objectToMod.transform.parent = activeSave.parent.transform;
                else
                    Debug.LogWarning("Prefab Brush is trying to set the objects parent to null. Please check that you have defined a gameobject in the Prefab Brush window.");
                break;
            case PB_ParentingStyle.ClosestFromList:

                float dist = Mathf.Infinity;
                Transform newParent = null;
                for (int i = 0; i < activeSave.parentList.Count; i++)
                {
                    float curDist = Vector3.Distance(activeSave.parentList[i].transform.position, objectToMod.transform.position);
                    if (curDist < dist)
                    {
                        newParent = activeSave.parentList[i].transform;
                        dist = curDist;
                    }
                }

                objectToMod.transform.parent = newParent;
                break;
            case PB_ParentingStyle.RoundRobin:
                roundRobinCount = GetId(activeSave.parentList.Count, roundRobinCount, 1);
                objectToMod.transform.parent = activeSave.parentList[roundRobinCount].transform;
                break;
        }

        if (activeSave.rotateToMatchSurface)    //If rotate to surface has been selected then set the spawn objects rotation to the bases normal.
            objectToMod.transform.rotation = Quaternion.FromToRotation(GetDirection(activeSave.rotateSurfaceDirection), hitRef.normal);

        if (activeSave.randomizeRotation)     //If random rotation has been selected then apply a random rotation define by the values in the window.
            objectToMod.transform.rotation *= Quaternion.Euler(new Vector3(Random.Range(activeSave.minXRotation, activeSave.maxXRotation), Random.Range(activeSave.minYRotation, activeSave.maxYRotation), Random.Range(activeSave.minZRotation, activeSave.maxZRotation)));

        //If random scale has been selected then apply a new scale transform to each object based on a random range.
        if (activeSave.randomizeScale)
        {
            float xRnd = Random.Range(activeSave.minXScale, activeSave.maxXScale);   //Create a random number between the min and max scale values.  
            float yRnd = Random.Range(activeSave.minYScale, activeSave.maxYScale);   //Create a random number between the min and max scale values.  
            float zRnd = Random.Range(activeSave.minZScale, activeSave.maxZScale);   //Create a random number between the min and max scale values.  
            objectToMod.transform.localScale = new Vector3(xRnd, yRnd, zRnd);    //Set the objects scale to the random number. (Based on imported scale)
        }
    }

    private GameObject GetRandomObject()
    {
        if (activeSave.prefabList.Count <= 0)
            return null;

        int rnd = Random.Range(0, activeSave.prefabList.Count);
        return activeSave.prefabList[rnd];
    }
    #endregion

    #region Tools
    private void SetActiveTab(PB_ActiveTab newTab)
    {
        activeTab = newTab;
    }

    private void SetTabColour(PB_ActiveTab tabToCheck)
    {
        GUI.color = (IsTabActive(tabToCheck)) ? selectedTab : Color.white;
    }

    private bool IsTabActive(PB_ActiveTab tabToCheck)
    {
        return activeTab == tabToCheck;
    }

    private void SetSaveOption(PB_SaveOptions newOption)
    {
        activeSaveOption = newOption;
    }

    private void SetTabColour(PB_SaveOptions optionToCheck)
    {
        GUI.color = (IsSaveOptionActive(optionToCheck)) ? selectedTab : Color.white;
    }

    private bool IsSaveOptionActive(PB_SaveOptions optionToCheck)
    {
        return activeSaveOption == optionToCheck;
    }

    private float GetPrefabIconSize()
    {
        return prefabIconMinSize * prefabIconScaleFactor;
    }

    private int GetId(int listSize, int curPointInList, int direction)
    {
        if ((curPointInList + direction) >= listSize)
            return 0;

        if ((curPointInList + direction) < 0)
            return listSize - 1;

        return curPointInList + direction;
    }

    private Vector3 GetDirection(PB_Direction direction)
    {
        switch (direction)
        {
            case PB_Direction.Up:
                return Vector3.up;
            case PB_Direction.Down:
                return -Vector3.up;
            case PB_Direction.Left:
                return -Vector3.right;
            case PB_Direction.Right:
                return Vector3.right;
            case PB_Direction.Forward:
                return Vector3.forward;
            case PB_Direction.Backward:
                return -Vector3.forward;
        }

        return Vector3.zero;
    }

    private string[] GetTagsFromMask(int original)
    {
        List<string> output = new List<string>();

        for (int i = 0; i < UnityEditorInternal.InternalEditorUtility.tags.Length; ++i)
        {
            int shifted = 1 << i;
            if ((original & shifted) == shifted)
            {
                string variableName = UnityEditorInternal.InternalEditorUtility.tags[i];
                if (!string.IsNullOrEmpty(variableName))
                {
                    output.Add(variableName);
                }
            }
        }
        return output.ToArray();
    }

    private string[] GetTagsFromLayer(int original)
    {
        List<string> output = new List<string>();

        for (int i = 0; i < UnityEditorInternal.InternalEditorUtility.layers.Length; ++i)
        {
            int shifted = 1 << i;
            if ((original & shifted) == shifted)
            {
                string variableName = UnityEditorInternal.InternalEditorUtility.layers[i];
                if (!string.IsNullOrEmpty(variableName))
                {
                    output.Add(variableName);
                }
            }
        }
        return output.ToArray();
    }
    #endregion

    #region SaveAndLoad
    private void CreateEmptySave()
    {
        MountSave();
        activeSaveID = -1;
    }

    private string SaveAs(string newName, bool overwrite = false, bool loadOnSave = false)
    {
        string finalName = newName;
        PB_SaveObject asset = Instantiate(activeSave);
        List<string> saveDataNames = GetSaveNames();

        if(overwrite == false)
        {
            int count = 0;
            while (saveDataNames.Contains(finalName))
            {
                finalName = string.Format("{0}({1})", newName, count);
                count++;
            }
        }

        AssetDatabase.CreateAsset(asset, savePath + "/" + finalName + ".asset");
        AssetDatabase.SaveAssets();
        RefreshSaves();

        if (loadOnSave)
        {
            for (int i = 0; i < saves.Count; i++)
            {
                if (finalName == saves[i].name)
                    LoadSave(i);
            }
        }

        return finalName;
    }

    private void LoadSave(int id)
    {
        MountSave(saves[id]);
        activeSaveID = id;
    }

    private void MountSave(PB_SaveObject objectToLoad = null)
    {
        PB_SaveObject asset = (objectToLoad == null) ? ScriptableObject.CreateInstance<PB_SaveObject>() : Instantiate(objectToLoad);
        string assetName = string.Format("{0}/activeSave.asset", GetActiveSavePath());

        AssetDatabase.CreateAsset(asset, assetName);
        AssetDatabase.SaveAssets();

        activeSave = (PB_SaveObject)AssetDatabase.LoadAssetAtPath(assetName, typeof(PB_SaveObject));
        loadedSave = objectToLoad;
    }

    private void DeleteSave(int id)
    {
        string deletePath = savePath + "/" + saves[id].name + ".asset";
        Object o = AssetDatabase.LoadAssetAtPath(deletePath, typeof(Object));

        if (o != null)
        {
            AssetDatabase.DeleteAsset(deletePath);
            saves.RemoveAt(id);

            if (id == activeSaveID)
                CreateEmptySave();
        }
    }

    private void SetUpSavePath()
    {
        if (savePath == "")
            savePath = GetDefualtSavePath();
    }

    private string GetDefualtSavePath()
    {
        string[] guid = AssetDatabase.FindAssets("PB_SaveObject");

        if (guid.Length <= 0)
            return "Assets/";

        string startingPath = AssetDatabase.GUIDToAssetPath(guid[0]);
        string currentPath = startingPath.Replace("/Scripts/PB_SaveObject.cs", "").Trim() + "/SaveFiles";

        return currentPath;
    }

    private string GetActiveSavePath()
    {
        string newPath = GetDefualtSavePath().Replace("/SaveFiles", "").Trim();
        return newPath;
    }

    private List<PB_SaveObject> FindAllSaves()
    {
        List<PB_SaveObject> allSaves = new List<PB_SaveObject>();
        string path = savePath;
        string[] fileEntries = System.IO.Directory.GetFiles(Application.dataPath.Replace("/Assets", "") + "/" + path);

        foreach (string fileName in fileEntries)
        {
            int index = fileName.LastIndexOf("/");
            string localPath = path;

            if (index > 0)

                localPath += fileName.Substring(index).Replace('\\', '/').Replace("/SaveFiles", "");
            PB_SaveObject t = (PB_SaveObject)AssetDatabase.LoadAssetAtPath(localPath, typeof(PB_SaveObject));
            if (t != null)
                allSaves.Add(t);
        }

        return allSaves;
    }

    private List<string> GetSaveNames()
    {
        UpdateSaves();

        List<string> saveNames = new List<string>();

        for (int i = 0; i < saves.Count; i++)
        {
            saveNames.Add(saves[i].name);
        }

        return saveNames;
    }

    private void RefreshSaves()
    {
        SetUpSavePath();
        UpdateSaves();
    }

    private void UpdateSaves()
    {
        saves = FindAllSaves();
    }

    private bool CheckIfSaveHasChanged()
    { 
        return ScriptableObject.Equals(loadedSave, activeSave);
    }

    private void StoreSaveAsComfirationInfo(string nameToOverwrite)
    {
        comfirmationName = nameToOverwrite;
    }

    private void StoreOpenAndDeleteComfirationInfo(string nameToDelete, int idToDelete)
    {
        comfirmationName = nameToDelete;
        comfirmationId = idToDelete;
    }
    #endregion

    void OnSceneGUI(SceneView sceneView)
    {
        //Hide gizmos when brushing
        Tools.hidden = (IsTabActive(PB_ActiveTab.PrefabPaint) && isOn);

        if (IsTabActive(PB_ActiveTab.PrefabPaint) && isOn)
        {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            DrawPaintCircle();
            RunPrefabPaint();
        }
    }
	
	void OnDestroy() 
	{
		// When the window is destroyed, remove the delegate so that it will no longer do any drawing.
		SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
	}
}