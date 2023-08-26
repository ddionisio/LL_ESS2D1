using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct GroundPoint {
    public Vector2 position;
    public Vector2 up;
    public bool isWater;

    public static bool GetGroundPoint(Vector2 position, out GroundPoint point) {
        var levelBounds = ColonyController.instance.bounds;

        var checkPoint = new Vector2(position.x, levelBounds.max.y);
        var checkDir = Vector2.down;

        var hit = Physics2D.Raycast(checkPoint, checkDir, levelBounds.size.y, GameData.instance.groundLayerMask);
        if(hit.collider) {
            point = new GroundPoint() { position = hit.point, up = hit.normal };

            return true;
        }
        else {
            point = new GroundPoint() { position = position, up = Vector2.up };

            return false;
        }
    }

    public static bool GetGroundPoint(float x, out GroundPoint point) {
        var levelBounds = ColonyController.instance.bounds;

        var checkPoint = new Vector2(x, levelBounds.max.y);
        var checkDir = Vector2.down;

        var hit = Physics2D.Raycast(checkPoint, checkDir, levelBounds.size.y, GameData.instance.groundLayerMask | GameData.instance.waterLayerMask);
        if(hit.collider) {
            point = new GroundPoint() { position = hit.point, up = hit.normal, isWater = ((1<<hit.collider.gameObject.layer) & GameData.instance.waterLayerMask) != 0 };

            return true;
        }
        else {
            point = new GroundPoint() { position = new Vector2(x, levelBounds.min.y), up = Vector2.up, isWater = false };

            return false;
        }
    }
}
