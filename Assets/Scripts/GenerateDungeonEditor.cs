using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GenerateDungeon))]
public class GenerateDungeonEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        GenerateDungeon script = (GenerateDungeon)target;
        if(GUILayout.Button("Generate Dungeon")) {
            script.GenerateNewDungeon();
        }
    }
}
