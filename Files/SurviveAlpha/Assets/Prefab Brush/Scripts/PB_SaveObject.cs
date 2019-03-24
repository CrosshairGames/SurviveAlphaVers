using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "[NEW]PB_SaveFile", menuName = "Create/VRG/Prefab Brush Save", order = 0), System.Serializable]
public class PB_SaveObject : ScriptableObject
{
    public List<GameObject> prefabList = new List<GameObject>();

    public float brushSize = 1;
    public float minBrushSize = .1f, maxBrushSize = 20;
    public int minObjectsPerBrush = 1, maxObjectsPerBrush = 100;
    public int prefabsPerStroke = 1;

    public bool checkLayer = false;
    public bool checkTag = false;
    public bool checkSlope = false;

    public int requiredTagMask, requiredLayerMask;
    public float minRequiredSlope, maxRequiredSlope;

    public Vector3 prefabOriginOffset, prefabRotationOffset;

    public PB_ParentingStyle parentingStyle;
    public GameObject parent;

    public bool rotateToMatchSurface = false;
    public PB_Direction rotateSurfaceDirection;

    public bool randomizeRotation;
    public float minXRotation, maxXRotation;
    public float minYRotation, maxYRotation;
    public float minZRotation, maxZRotation;

    public bool randomizeScale = false;
    public float minXScale = 1, maxXScale = 1;
    public float minYScale = 1, maxYScale = 1;
    public float minZScale = 1, maxZScale = 1;

    public List<GameObject> parentList = new List<GameObject>();
}