using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomDirection : MonoBehaviour {
    public Vector2[] doorDirections;
    // remember, rotating an object 90 degrees is rotating it counter clockwise

    public void RotateRoom90Degrees() {
        for(int i = 0; i < doorDirections.Length; i++) {
            if (doorDirections[i] == Vector2.up) {
                doorDirections[i] = Vector2.left;
            } else if (doorDirections[i] == Vector2.left) {
                doorDirections[i] = Vector2.down;
            } else if (doorDirections[i] == Vector2.down) {
                doorDirections[i] = Vector2.right;
            } else if (doorDirections[i] == Vector2.right) {
                doorDirections[i] = Vector2.up;
            }
        }

        // do i do this here?
        transform.Rotate(new Vector3(0, 0, 90));
    }

    public void RotateRandom() {
        Debug.Log("TODO rotate a random amount!");
    }

    public Vector2[] GetDoorDirections() {
        return doorDirections;
    }
}
