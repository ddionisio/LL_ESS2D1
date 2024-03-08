using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LandscapeGridTerrain : MonoBehaviour {
	public GridData.TopographyType topography;

	public GridData.AtmosphereMod mod;

	public bool isTerrain = true; //if true, altitude is used to determine spot's altitude

    public float altitude;
	public GridData.AtmosphereMod altitudeMod;

	public string topographyName { get { return GridData.instance.GetTopographyText(topography); } }

	public Collider2D coll { 
		get {
			if(!mColl)
				mColl = GetComponent<Collider2D>();

			return mColl;
		} 
	}

	private Collider2D mColl;
}
