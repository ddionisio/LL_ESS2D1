using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LandscapeGridTerrain : MonoBehaviour {
	public GridData.AtmosphereMod mod;

	public bool isTerrain = true; //if true, altitude is used to determine spot's altitude

    public float altitude;
	public GridData.AtmosphereMod altitudeMod;

	public Collider2D coll { 
		get {
			if(!mColl)
				mColl = GetComponent<Collider2D>();

			return mColl;
		} 
	}

	private Collider2D mColl;
}
