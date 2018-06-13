using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridHelper : MonoBehaviour {
}

public static class GlobalGrid {
    public static Grid GetGrid() {
        return GameObject.FindGameObjectWithTag("Grid").GetComponent<Grid>();
    }
}
