﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LoLExt {
    public class GameBounds2D : MonoBehaviour {
        public Rect rect = new Rect(-1f, -1f, 2f, 2f);

        //editor info
        public Vector2 editRectSteps = Vector2.one;
        public Color editRectColor = Color.cyan;
        public bool editSyncBoxCollider = false; //sync bound position and size to box collider

        public Vector2 Clamp(Vector2 center, Vector2 ext) {
            Vector2 min = (Vector2)rect.min + ext;
            Vector2 max = (Vector2)rect.max - ext;

            float extX = rect.width * 0.5f;
            float extY = rect.height * 0.5f;

            if(extX > ext.x)
                center.x = Mathf.Clamp(center.x, min.x, max.x);
            else
                center.x = rect.center.x;

            if(extY > ext.y)
                center.y = Mathf.Clamp(center.y, min.y, max.y);
            else
                center.y = rect.center.y;

            return center;
        }

        public float ClampX(float centerX, float extX) {
            var minX = rect.min.x + extX;
            var maxX = rect.max.x - extX;

            var rExtX = rect.width * 0.5f;

            if(rExtX > extX)
                centerX = Mathf.Clamp(centerX, minX, maxX);
            else
                centerX = rect.center.x;

            return centerX;
        }

        public float ClampY(float centerY, float extY) {
            var minY = rect.min.y + extY;
            var maxY = rect.max.y - extY;

            var rExtY = rect.height * 0.5f;

            if(rExtY > extY)
                centerY = Mathf.Clamp(centerY, minY, maxY);
            else
                centerY = rect.center.y;

            return centerY;
        }

        void OnDrawGizmos() {
            Gizmos.color = editRectColor;
            Gizmos.DrawWireCube(rect.center, rect.size);
        }
    }
}