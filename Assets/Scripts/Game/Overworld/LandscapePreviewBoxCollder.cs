using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class LandscapePreviewBoxCollder : MonoBehaviour {
	public BoxCollider2D boxCollider { 
		get {
			if(!mCollider)
				mCollider = GetComponent<BoxCollider2D>();
			return mCollider;
		} 
	}

    private BoxCollider2D mCollider;

	public bool RefreshCollider() {
		var coll = boxCollider;

		bool isChanged = false;

		if(coll.offset != Vector2.zero) {
			coll.offset = Vector2.zero;
			isChanged = true;
		}

		var previewSize = GameData.instance.landscapePreviewSize;

		if(coll.size != previewSize) {
			coll.size = previewSize;
			isChanged = true;
		}

		return isChanged;
	}

	void Awake() {
		RefreshCollider();
	}

	void OnDrawGizmos() {
		var size = GameData.instance.landscapePreviewSize;

		Gizmos.color = GameData.instance.landscapePreviewBoundsColor;

		Gizmos.DrawWireCube(transform.position, new Vector3(size.x, size.y));
	}
}
