using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(ExportToGtiff))]
public class ExportToGtiffEditor : Editor
{

    // Use this for initialization
    public override void OnInspectorGUI()
    {
         var obj = target as ExportToGtiff;

//        base.OnInspectorGUI();

        GUILayout.Label("Reference Path");
        GUILayout.TextField(obj.ref_path);

        obj.overwrite_ref = GUILayout.Toggle(obj.overwrite_ref, "Overwrite Reference");

        GUILayout.Label("Destination Path");

        if (obj.overwrite_ref)
        {
            obj.dst_path = obj.ref_path;
            EditorGUILayout.SelectableLabel(obj.dst_path, 
                                      EditorStyles.textField, 
                                      GUILayout.Height(EditorGUIUtility.singleLineHeight));
        }
        else
        {
            obj.dst_path = GUILayout.TextField(obj.dst_path);
        }

        if (GUILayout.Button("Export to Gtiff"))
        {
            obj.Export();
        }

    }

}