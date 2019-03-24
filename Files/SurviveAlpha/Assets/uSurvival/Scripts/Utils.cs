// This class contains some helper functions.
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.AI;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Security.Cryptography;

public class Utils
{
    // ScaleFloatToUShort( -1f, -1f, 1f, ushort.MinValue, ushort.MaxValue) => 0
    // ScaleFloatToUShort(  0f, -1f, 1f, ushort.MinValue, ushort.MaxValue) => 32767
    // ScaleFloatToUShort(0.5f, -1f, 1f, ushort.MinValue, ushort.MaxValue) => 49151
    // ScaleFloatToUShort(  1f, -1f, 1f, ushort.MinValue, ushort.MaxValue) => 65535
    public static ushort ScaleFloatToUShort(float value, float minValue, float maxValue, ushort minTarget, ushort maxTarget)
    {
        // note: C# ushort - ushort => int, hence so many casts
        int targetRange = maxTarget - minTarget; // max ushort - min ushort > max ushort. needs bigger type.
        float valueRange = maxValue - minValue;
        float valueRelative = value - minValue;
        return (ushort)(minTarget + (ushort)(valueRelative/valueRange * (float)targetRange));
    }

    // ScaleFloatToByte( -1f, -1f, 1f, byte.MinValue, byte.MaxValue) => 0
    // ScaleFloatToByte(  0f, -1f, 1f, byte.MinValue, byte.MaxValue) => 127
    // ScaleFloatToByte(0.5f, -1f, 1f, byte.MinValue, byte.MaxValue) => 191
    // ScaleFloatToByte(  1f, -1f, 1f, byte.MinValue, byte.MaxValue) => 255
    public static byte ScaleFloatToByte(float value, float minValue, float maxValue, byte minTarget, byte maxTarget)
    {
        // note: C# byte - byte => int, hence so many casts
        int targetRange = maxTarget - minTarget; // max byte - min byte only fits into something bigger
        float valueRange = maxValue - minValue;
        float valueRelative = value - minValue;
        return (byte)(minTarget + (byte)(valueRelative/valueRange * (float)targetRange));
    }

    // ScaleUShortToFloat(    0, ushort.MinValue, ushort.MaxValue, -1, 1) => -1
    // ScaleUShortToFloat(32767, ushort.MinValue, ushort.MaxValue, -1, 1) => 0
    // ScaleUShortToFloat(49151, ushort.MinValue, ushort.MaxValue, -1, 1) => 0.4999924
    // ScaleUShortToFloat(65535, ushort.MinValue, ushort.MaxValue, -1, 1) => 1
    public static float ScaleUShortToFloat(ushort value, ushort minValue, ushort maxValue, float minTarget, float maxTarget)
    {
        // note: C# ushort - ushort => int, hence so many casts
        float targetRange = maxTarget - minTarget;
        ushort valueRange = (ushort)(maxValue - minValue);
        ushort valueRelative = (ushort)(value - minValue);
        return minTarget + (float)((float)valueRelative/(float)valueRange * targetRange);
    }

    // ScaleByteToFloat(  0, byte.MinValue, byte.MaxValue, -1, 1) => -1
    // ScaleByteToFloat(127, byte.MinValue, byte.MaxValue, -1, 1) => -0.003921569
    // ScaleByteToFloat(191, byte.MinValue, byte.MaxValue, -1, 1) => 0.4980392
    // ScaleByteToFloat(255, byte.MinValue, byte.MaxValue, -1, 1) => 1
    public static float ScaleByteToFloat(byte value, byte minValue, byte maxValue, float minTarget, float maxTarget)
    {
        // note: C# byte - byte => int, hence so many casts
        float targetRange = maxTarget - minTarget;
        byte valueRange = (byte)(maxValue - minValue);
        byte valueRelative = (byte)(value - minValue);
        return minTarget + (float)((float)valueRelative/(float)valueRange * targetRange);
    }

    // eulerAngles have 3 floats, putting them into 2 bytes of [x,y],[z,0]
    // would be a waste. instead we compress into 5 bits each => 15 bits.
    // so a ushort.
    public static ushort PackThreeFloatsIntoUShort(float u, float v, float w, float minValue, float maxValue)
    {
        // 5 bits max value = 1+2+4+8+16 = 31 = 0x1F
        byte lower = ScaleFloatToByte(u, minValue, maxValue, 0x00, 0x1F);
        byte middle = ScaleFloatToByte(v, minValue, maxValue, 0x00, 0x1F);
        byte upper = ScaleFloatToByte(w, minValue, maxValue, 0x00, 0x1F);
        ushort combined = (ushort)(upper << 10 | middle << 5 | lower);
        return combined;
    }

    // see PackThreeFloatsIntoUShort for explanation
    public static float[] UnpackUShortIntoThreeFloats(ushort combined, float minTarget, float maxTarget)
    {
        byte lower = (byte)(combined & 0x1F);
        byte middle = (byte)((combined >> 5) & 0x1F);
        byte upper = (byte)(combined >> 10); // nothing on the left, no & needed

        // note: we have to use 4 bits per float, so between 0x00 and 0x0F
        float u = ScaleByteToFloat(lower, 0x00, 0x1F, minTarget, maxTarget);
        float v = ScaleByteToFloat(middle, 0x00, 0x1F, minTarget, maxTarget);
        float w = ScaleByteToFloat(upper, 0x00, 0x1F, minTarget, maxTarget);
        return new float[]{u, v, w};
    }

    // is any of the keys UP?
    public static bool AnyKeyUp(KeyCode[] keys)
    {
        return keys.Any(k => Input.GetKeyUp(k));
    }

    // is any of the keys DOWN?
    public static bool AnyKeyDown(KeyCode[] keys)
    {
        return keys.Any(k => Input.GetKeyDown(k));
    }

    // is any of the keys PRESSED?
    public static bool AnyKeyPressed(KeyCode[] keys)
    {
        return keys.Any(k => Input.GetKey(k));
    }

    // Distance between two ClosestPoints
    // this is needed in cases where entites are really big. in those cases,
    // we can't just move to entity.transform.position, because it will be
    // unreachable. instead we have to go the closest point on the boundary.
    //
    // Vector3.Distance(a.transform.position, b.transform.position):
    //    _____        _____
    //   |     |      |     |
    //   |  x==|======|==x  |
    //   |_____|      |_____|
    //
    //
    // Utils.ClosestDistance(a.collider, b.collider):
    //    _____        _____
    //   |     |      |     |
    //   |     |x====x|     |
    //   |_____|      |_____|
    //
    public static float ClosestDistance(Collider a, Collider b)
    {
        // return 0 if both intersect or if one is inside another.
        // ClosestPoint distance wouldn't be > 0 in those cases otherwise.
        if (a.bounds.Intersects(b.bounds))
            return 0;

        // Unity offers ClosestPointOnBounds and ClosestPoint.
        // ClosestPoint is more accurate. OnBounds often doesn't get <1 because
        // it uses a point at the top of the player collider, not in the center.
        // (use Debug.DrawLine here to see the difference)
        return Vector3.Distance(a.ClosestPoint(b.transform.position),
                                b.ClosestPoint(a.transform.position));
    }

    // raycast while ignoring self (by setting layer to "Ignore Raycasts" first)
    // => setting layer to IgnoreRaycasts before casting is the easiest way to do it
    // => raycast + !=this check would still cause hit.point to be on player
    // => raycastall is not sorted and child objects might have different layers etc.
    public static bool RaycastWithout(Vector3 origin, Vector3 direction, out RaycastHit hit, float maxDistance, GameObject ignore, int layerMask=Physics.DefaultRaycastLayers)
    {
        // remember layers
        Dictionary<Transform, int> backups = new Dictionary<Transform, int>();

        // set all to ignore raycast
        foreach (Transform tf in ignore.GetComponentsInChildren<Transform>(true)) {
            backups[tf] = tf.gameObject.layer;
            tf.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        }

        // raycast
        bool result = Physics.Raycast(origin, direction, out hit, maxDistance, layerMask);

        // restore layers
        foreach (KeyValuePair<Transform, int> kvp in backups)
            kvp.Key.gameObject.layer = kvp.Value;

        return result;
    }

    public static bool LinecastWithout(Vector3 start, Vector3 end, out RaycastHit hit, GameObject ignore, int layerMask=Physics.DefaultRaycastLayers)
    {
        // remember layers
        Dictionary<Transform, int> backups = new Dictionary<Transform, int>();

        // set all to ignore raycast
        foreach (Transform tf in ignore.GetComponentsInChildren<Transform>(true))
        {
            backups[tf] = tf.gameObject.layer;
            tf.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        }

        // raycast
        bool result = Physics.Linecast(start, end, out hit, layerMask);

        // restore layers
        foreach (KeyValuePair<Transform, int> kvp in backups)
            kvp.Key.gameObject.layer = kvp.Value;

        return result;
    }

    public static bool SphereCastWithout(Vector3 origin, float sphereRadius, Vector3 direction, out RaycastHit hit, float maxDistance, GameObject ignore, int layerMask=Physics.DefaultRaycastLayers)
    {
        // remember layers
        Dictionary<Transform, int> backups = new Dictionary<Transform, int>();

        // set all to ignore raycast
        foreach (Transform tf in ignore.GetComponentsInChildren<Transform>(true)) {
            backups[tf] = tf.gameObject.layer;
            tf.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        }

        // raycast
        bool result = Physics.SphereCast(origin, sphereRadius, direction, out hit, maxDistance, layerMask);

        // restore layers
        foreach (KeyValuePair<Transform, int> kvp in backups)
            kvp.Key.gameObject.layer = kvp.Value;

        return result;
    }

    // hard mouse scrolling that is consistent between all platforms
    //   Input.GetAxis("Mouse ScrollWheel") and
    //   Input.GetAxisRaw("Mouse ScrollWheel")
    //   both return values like 0.01 on standalone and 0.5 on WebGL, which
    //   causes too fast zooming on WebGL etc.
    // normally GetAxisRaw should return -1,0,1, but it doesn't for scrolling
    public static float GetAxisRawScrollUniversal()
    {
        float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
        if (scroll < 0) return -1;
        if (scroll > 0) return  1;
        return 0;
    }

    // two finger pinch detection
    // source: https://docs.unity3d.com/Manual/PlatformDependentCompilation.html
    public static float GetPinch()
    {
        if (Input.touchCount == 2)
        {
            // Store both touches.
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            // Find the position in the previous frame of each touch.
            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            // Find the magnitude of the vector (the distance) between the touches in each frame.
            float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

            // Find the difference in the distances between each frame.
            return touchDeltaMag - prevTouchDeltaMag;
        }
        return 0;
    }

    // universal zoom: mouse scroll if mouse, two finger pinching otherwise
    public static float GetZoomUniversal()
    {
        if (Input.mousePresent)
            return GetAxisRawScrollUniversal();
        else if (Input.touchSupported)
            return GetPinch();
        return 0;
    }

    // parse last upper cased noun from a string, e.g.
    //   EquipmentWeaponBow => Bow
    //   EquipmentShield => Shield
    public static string ParseLastNoun(string text)
    {
        MatchCollection matches = new Regex(@"([A-Z][a-z]*)").Matches(text);
        return matches.Count > 0 ? matches[matches.Count-1].Value : "";
    }

    // check if the cursor is over a UI or OnGUI element right now
    // note: for UI, this only works if the UI's CanvasGroup blocks Raycasts
    // note: for OnGUI: hotControl is only set while clicking, not while zooming
    public static bool IsCursorOverUserInterface()
    {
        // IsPointerOverGameObject check for left mouse (default)
        if (EventSystem.current.IsPointerOverGameObject())
            return true;

        // IsPointerOverGameObject check for touches
        for (int i = 0; i < Input.touchCount; ++i)
            if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(i).fingerId))
                return true;

        // OnGUI check
        return GUIUtility.hotControl != 0;
    }

    // PBKDF2 hashing recommended by NIST:
    // http://nvlpubs.nist.gov/nistpubs/Legacy/SP/nistspecialpublication800-132.pdf
    // salt should be at least 128 bits = 16 bytes
    public static string PBKDF2Hash(string text, string salt)
    {
        byte[] saltBytes = Encoding.UTF8.GetBytes(salt);
        Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(text, saltBytes, 10000);
        byte[] hash = pbkdf2.GetBytes(20);
        return BitConverter.ToString(hash).Replace("-", string.Empty);
    }

    // random point on NavMesh for item drops, etc.
    public static Vector3 RandomUnitCircleOnNavMesh(Vector3 position, float radiusMultiplier)
    {
        // random circle point
        Vector2 r = UnityEngine.Random.insideUnitCircle * radiusMultiplier;

        // convert to 3d
        Vector3 randomPosition = new Vector3(position.x + r.x, position.y, position.z + r.y);

        // raycast to find valid point on NavMesh. otherwise return original one
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPosition, out hit, radiusMultiplier * 2, NavMesh.AllAreas))
            return hit.position;
        return position;
    }

    // random point on NavMesh that has no obstacles (walls) between point and center
    // -> useful because items shouldn't be dropped behind walls, etc.
    public static Vector3 ReachableRandomUnitCircleOnNavMesh(Vector3 position, float radiusMultiplier, int solverAttempts)
    {
        for (int i = 0; i < solverAttempts; ++i)
        {
            // get random point on navmesh around position
            Vector3 candidate = RandomUnitCircleOnNavMesh(position, radiusMultiplier);

            // check if anything obstructs the way (walls etc.)
            NavMeshHit hit;
            if (!NavMesh.Raycast(position, candidate, out hit, NavMesh.AllAreas))
                return candidate;
        }

        // otherwise return original position if we can't find any good point.
        // in that case it's best to just drop it where the entity stands.
        return position;
    }

    // can Collider A 'reach' Collider B?
    // e.g. can zombie reach player to attack?
    //      can player reach item to pick up?
    // => NOTE: we only try to reach the center vertical line of the collider.
    //    this is not a perfect 'is collider reachable' function that checks
    //    any point on the collider. it is perfect for monsters and players
    //    though, because they are rather vertical
    public static bool IsReachableVertically(Collider origin, Collider other)
    {
        // raycast from origin to other to decide if reachable
        // -> we cast from origin center/top to all center/top/bottom of other
        //    aka 'can origin attack any part of other with head or hands?'
        Vector3 otherCenter = other.bounds.center;
        Vector3 otherTop    = otherCenter + Vector3.up * other.bounds.extents.y;
        Vector3 otherBottom = otherCenter + Vector3.down * other.bounds.extents.y;

        Vector3 originCenter = origin.bounds.center;
        Vector3 originTop    = origin.bounds.center + Vector3.up * origin.bounds.extents.y;

        // draw lines for debugging if needed
        Debug.DrawLine(originCenter, otherCenter, Color.white);
        Debug.DrawLine(originCenter, otherTop,    Color.white);
        Debug.DrawLine(originCenter, otherBottom, Color.white);
        Debug.DrawLine(originTop,    otherCenter, Color.white);
        Debug.DrawLine(originTop,    otherTop,    Color.white);
        Debug.DrawLine(originTop,    otherBottom, Color.white);

        // reachable if any point hits other collider directly
        // (if there is nothing between us and target at one of the lines)
        RaycastHit hitInfo;
        return LinecastWithout(originCenter, otherCenter, out hitInfo, origin.gameObject) && hitInfo.collider == other ||
               LinecastWithout(originCenter, otherTop,    out hitInfo, origin.gameObject) && hitInfo.collider == other ||
               LinecastWithout(originCenter, otherBottom, out hitInfo, origin.gameObject) && hitInfo.collider == other ||
               LinecastWithout(originTop,    otherCenter, out hitInfo, origin.gameObject) && hitInfo.collider == other ||
               LinecastWithout(originTop,    otherTop,    out hitInfo, origin.gameObject) && hitInfo.collider == other ||
               LinecastWithout(originTop,    otherBottom, out hitInfo, origin.gameObject) && hitInfo.collider == other;
    }

    // clamp a rotation around x axis
    // (e.g. camera up/down rotation so we can't look below character's pants etc.)
    // original source: Unity's standard assets MouseLook.cs
    public static Quaternion ClampRotationAroundXAxis(Quaternion q, float min, float max)
    {
        q.x /= q.w;
        q.y /= q.w;
        q.z /= q.w;
        q.w = 1.0f;

        float angleX = 2.0f * Mathf.Rad2Deg * Mathf.Atan (q.x);
        angleX = Mathf.Clamp (angleX, min, max);
        q.x = Mathf.Tan (0.5f * Mathf.Deg2Rad * angleX);

        return q;
    }
}
