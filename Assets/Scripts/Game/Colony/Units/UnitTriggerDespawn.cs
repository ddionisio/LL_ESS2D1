using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitTriggerDespawn : MonoBehaviour {
	private void OnTriggerEnter2D(Collider2D collision) {
		var unit = collision.GetComponent<Unit>();
		if(unit)
			unit.Despawn();
	}
}
