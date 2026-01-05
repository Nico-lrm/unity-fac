using UnityEngine;
using UnityEditor; // Obligatoire pour toucher à l'éditeur

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Affiche tout le contenu normal (les variables, le fichier Map Data...)
        DrawDefaultInspector();

        MapGenerator myScript = (MapGenerator)target;

        GUILayout.Space(15); // Espace visuel
        GUILayout.Label("Outils de Level Design", EditorStyles.boldLabel);

        // Bouton pour Construire (Appelle ta fonction)
        if (GUILayout.Button("🏗️ Construire la Map (Fichier)"))
        {
            // On s'assure qu'on a bien assigné un fichier
            if (myScript.mapData != null)
            {
                myScript.GenerateMapFromData();
                
                // Force Unity à sauvegarder la scène modifié
                EditorUtility.SetDirty(myScript); 
            }
            else
            {
                Debug.LogError("⚠️ Oups ! Glisse un fichier 'MapDefinition' dans le slot 'Map Data' d'abord.");
            }
        }

        // Bouton pour Nettoyer
        if (GUILayout.Button("🧹 Nettoyer la Map"))
        {
            myScript.ClearMap(); // On va créer cette petite fonction publique juste après
        }
    }
}