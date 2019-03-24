using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class FirstPersonClipping : NetworkBehaviour
{
    [Header("Components")]
    public PlayerLook look;
    public PlayerEquipment equipment;

    [Header("Mesh Hiding")]
    public Renderer[] hideRenderers;
    public Material transparentMaterial;
    Dictionary<Renderer, Material> materialBackups = new Dictionary<Renderer, Material>();

    [Header("Disable Depth Check (to avoid clipping)")]
    public bool DisableWeaponDepthCheck = true;
    public string noDepthLayer = "NoDepthInFirstPerson";
    public Renderer[] disableArmsDepthCheck;
    Camera weaponCamera;

    public override void OnStartLocalPlayer()
    {
        // backup materials
        foreach (Renderer renderer in hideRenderers)
            materialBackups[renderer] = renderer.material;

        // find weapon camera
        foreach (Transform t in Camera.main.transform)
            if (t.tag == "WeaponCamera")
                weaponCamera = t.GetComponent<Camera>();
    }

    void HideMeshes(bool firstPerson)
    {
        // hide body etc. if needed, so that we don't see ourself when looking
        // downwards
        // -> we have to do it in Update because proximity checker may overwrite
        //    it
        // -> we don't just destroy it, because that won't work for the textmesh
        // -> do it continously to overwrite proximitychecker changes
        // -> disabling renderer causes ik to stop working, so we need to
        // swap out the material with something transparent instead.
        foreach (Renderer renderer in hideRenderers)
            renderer.material = firstPerson ? transparentMaterial : materialBackups[renderer];
    }

    void DisableDepthCheck(bool firstPerson)
    {
        // enable weapon camera only in first person
        // (to draw arms and weapon without depth check to avoid clipping
        //  through walls)
        if (weaponCamera != null)
            weaponCamera.enabled = firstPerson;

        // set weapon layer to NoDepth (only for localplayer so we don't see
        // others without depth checks)
        // -> do for arms etc.
        foreach (Renderer renderer in disableArmsDepthCheck)
            renderer.gameObject.layer = LayerMask.NameToLayer(noDepthLayer);

        // -> do for weapon
        foreach (Renderer renderer in equipment.weaponMount.GetComponentsInChildren<Renderer>())
            renderer.gameObject.layer = LayerMask.NameToLayer(noDepthLayer);
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        // only hide while in first person mode
        bool firstPerson = look.InFirstPerson();
        HideMeshes(firstPerson);
        DisableDepthCheck(firstPerson);
    }
}
