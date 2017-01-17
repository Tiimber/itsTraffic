using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingRoofLayer : MonoBehaviour {

    void Awake() {
        gameObject.layer = LayerMask.NameToLayer("BuildingRoof");
    }

}
