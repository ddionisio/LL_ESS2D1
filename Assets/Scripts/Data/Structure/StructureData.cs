using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "structure", menuName = "Game/Structure")]
public class StructureData : ScriptableObject {
    [Header("Info")]
    [M8.Localize]
    public string nameRef;
    [M8.Localize]
    public string descRef;

    public Sprite icon;

    [Header("Stats")]
    public int hitpoints; //for damageable, set to 0 for invulnerable

    //for buildable
    public float buildTime; //if buildable, set to 0 for spawning building (e.g. house)

    public int workCapacity = 2; //how many can work on this structure

    public bool isReparable;
    public bool isDemolishable;

    [Header("Spawn")]
    public StructureGhost ghostPrefab;
    public Structure spawnPrefab;

    [Header("Placement Info")]
    public LayerMask placementValidLayerMask;

    public bool IsPlacementLayerValid(int layer) {
        return (placementValidLayerMask & (1 << layer)) != 0;
    }

    public bool IsPlacementValid(Vector2 pos, Vector2 size) {
        var checkValid = false;

        var levelBounds = ColonyController.isInstantiated ? ColonyController.instance.bounds : new Bounds(Vector3.zero, Vector3.one);

        var checkPoint = new Vector2(pos.x, levelBounds.max.y);

        var hit = Physics2D.BoxCast(checkPoint, size, 0f, Vector2.down, levelBounds.size.y, GameData.instance.placementCheckLayerMask | placementValidLayerMask);
        var hitColl = hit.collider;
        if(hitColl) {
            checkValid = (placementValidLayerMask & (1 << hitColl.gameObject.layer)) != 0;
        }

        return checkValid;
    }

    /// <summary>
    /// For structures that need specific setup during initialization
    /// </summary>
    public virtual void Setup(ColonyController colonyCtrl, int structureCount) { }
}
