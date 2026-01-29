using UnityEngine;
using System.Collections.Generic;

public class MapPreviewManager : MonoBehaviour
{
    public static MapPreviewManager Instance;

    [Header("Réglages Généraux")]
    public Transform contentParent;
    public GameObject cubePrefab;
    public float rotationSpeed = 20f;

    [Header("Matériaux Holographiques")]
    public Material[] terrainMaterials; // 0=Fond, 1=Sol, 2=Haut...

    [Header("Couleurs Spéciales")]
    public Material playerSpawnMat; // Créer un matériau Bleu/Vert brillant
    public Material enemySpawnMat;  // Créer un matériau Rouge brillant

    private void Awake() { Instance = this; }

    void Update()
    {
        if (contentParent != null)
            contentParent.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }

    public void ShowMapPreview(MapDefinition mapData)
    {
        // 1. Nettoyage
        foreach (Transform child in contentParent) Destroy(child.gameObject);
        contentParent.rotation = Quaternion.identity;

        if (mapData == null) return;

        GenerateVisuals(mapData);
    }

    void GenerateVisuals(MapDefinition data)
    {
        float offsetX = data.mapRows[0].Split(',').Length / 2f;
        float offsetZ = data.mapRows.Length / 2f;

        for (int z = 0; z < data.mapRows.Length; z++)
        {
            string[] rawCells = data.mapRows[z].Split(',');

            for (int x = 0; x < rawCells.Length; x++)
            {
                // --- 1. ANALYSE DU TEXTE (Parsing) ---
                string cell = rawCells[x].Trim(); // Enlève les espaces

                bool isPlayer = cell.Contains("+");
                bool isEnemy = cell.Contains("-");

                // On enlève les symboles pour récupérer juste le chiffre
                string cleanNumber = cell.Replace("+", "").Replace("-", "");

                if (int.TryParse(cleanNumber, out int heightCode))
                {
                    // --- 2. GESTION DU HAUTEUR 0 ---
                    // Si c'est 0, on veut quand même le voir (comme un sol plat)
                    // On force la hauteur visuelle à 1 cube si c'est 0, sinon on prend la hauteur réelle.
                    int visualHeight = (heightCode == 0) ? 1 : heightCode;

                    // --- 3. CONSTRUCTION ---
                    for (int h = 0; h < visualHeight; h++)
                    {
                        Vector3 pos = new Vector3(x - offsetX, h, (data.mapRows.Length - 1 - z) - offsetZ);
                        GameObject cube = Instantiate(cubePrefab, contentParent);
                        cube.transform.localPosition = pos;
                        cube.layer = LayerMask.NameToLayer("MiniMap");

                        Renderer r = cube.GetComponent<Renderer>();
                        if (r != null)
                        {
                            // Si c'est un bloc 0, on met le materiau 0 (Sombre/Eau)
                            // Sinon on met le matériau correspondant à la hauteur
                            int matIndex = (heightCode == 0) ? 0 : Mathf.Clamp(heightCode, 0, terrainMaterials.Length - 1);

                            if (terrainMaterials != null && terrainMaterials.Length > matIndex)
                                r.material = terrainMaterials[matIndex];
                        }
                    }

                    // --- 4. INDICATEURS DE SPAWN ---
                    // On ajoute un petit cube au dessus de la dernière case pour montrer le spawn
                    if (isPlayer || isEnemy)
                    {
                        // Position au dessus du bloc le plus haut
                        Vector3 markerPos = new Vector3(x - offsetX, visualHeight, (data.mapRows.Length - 1 - z) - offsetZ);

                        GameObject marker = Instantiate(cubePrefab, contentParent);
                        marker.transform.localPosition = markerPos;
                        marker.transform.localScale = Vector3.one * 0.5f; // Plus petit (50% de la taille)
                        marker.layer = LayerMask.NameToLayer("MiniMap");

                        Renderer rMarker = marker.GetComponent<Renderer>();
                        if (rMarker != null)
                        {
                            rMarker.material = isPlayer ? playerSpawnMat : enemySpawnMat;
                        }
                    }
                }
            }
        }
    }
}