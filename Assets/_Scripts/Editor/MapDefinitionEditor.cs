using UnityEngine;
using UnityEditor; 

[CustomEditor(typeof(MapDefinition))]
public class MapDefinitionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Affiche tout le contenu normal (les variables, le fichier Map Data...)
        DrawDefaultInspector();

        MapDefinition myScript = (MapDefinition)target;

        GUILayout.Space(15); // Espace visuel
        GUILayout.Label("Outils Pour export", EditorStyles.boldLabel);

        // Bouton pour Construire (Appelle ta fonction)
        if (GUILayout.Button("Exporter les rows"))
        {
            EditorGUIUtility.systemCopyBuffer = myScript.exportRows();
        }

        // Bouton pour Nettoyer
        if (GUILayout.Button("Importer les rows"))
        {
            myScript.importRows(EditorGUIUtility.systemCopyBuffer);
            EditorUtility.SetDirty(myScript);
        }
    }
}