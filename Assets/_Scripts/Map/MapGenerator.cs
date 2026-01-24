using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MapGenerator : MonoBehaviour
{
    public static MapGenerator Instance;
    public static Dictionary<Vector2, TileData> mapGrid = new Dictionary<Vector2, TileData>();

    [Header("Configuration")]
    public MapDefinition mapData;
    public GameObject tilePrefab; 
    public Transform mapHolder;
    public Material[] terrainMaterials;

    // On stocke les positions trouvées
    private List<Vector2> foundPlayerSpawns = new List<Vector2>();
    private List<Vector2> foundEnemySpawns = new List<Vector2>();

    void Awake()
    {
        Instance = this;
        if (mapGrid != null) mapGrid.Clear(); else mapGrid = new Dictionary<Vector2, TileData>();
        if (mapHolder == null) { GameObject h = new GameObject("MapHolder"); mapHolder = h.transform; }
    }
    
    void Start()
    {
        if (mapData == null) { Debug.LogError("❌ Pas de MapData !"); return; }

        GenerateMapFromData(); 

        // Placement des Joueurs (Aléatoire sur les '+')
        if (GameData.Instance != null && GameData.Instance.selectedRoster.Count > 0)
        {
            SpawnPlayerTeamRandomly();
        }
        
        // Placement des Ennemis (Aléatoire sur les '-') et Lancement
        StartGame(); 
    }

    public void ClearMap()
    {
        mapGrid.Clear();
        foundPlayerSpawns.Clear();
        foundEnemySpawns.Clear();

        if (mapHolder != null)
        {
            while (mapHolder.childCount > 0) DestroyImmediate(mapHolder.GetChild(0).gameObject);
        }
    }

    public void GenerateMapFromData()
    {
        ClearMap(); 

        int rowsCount = mapData.mapRows.Length;

        for (int z = 0; z < rowsCount; z++)
        {
            string rowString = mapData.mapRows[z];
            string[] cells = rowString.Split(',');

            int worldZ = rowsCount - 1 - z;

            for (int x = 0; x < cells.Length; x++)
            {
                string cellContent = cells[x].Trim(); 
                if (string.IsNullOrEmpty(cellContent)) continue;

                // DÉTECTION DES MARQUEURS
                bool isPlayerSpawn = cellContent.Contains("+");
                bool isEnemySpawn = cellContent.Contains("-");

                // Nettoyage pour récupérer le type de terrain (le chiffre)
                string numberOnly = cellContent.Replace("+", "").Replace("-", "");
                
                int tileType = 0;
                int.TryParse(numberOnly, out tileType); 

                CreateTile(x, worldZ, tileType);

                // STOCKAGE DES POSITIONS
                if (isPlayerSpawn) foundPlayerSpawns.Add(new Vector2(x, worldZ));
                if (isEnemySpawn) foundEnemySpawns.Add(new Vector2(x, worldZ));
            }
        }
    }

    void CreateTile(int x, int z, int type)
    {
        int y = type; 
        Vector3 position = new Vector3(x, y, z);
        GameObject tile = Instantiate(tilePrefab, position, Quaternion.identity);
        tile.transform.parent = mapHolder;
        tile.name = $"Tile_{x}_{z}";
        tile.transform.localScale = Vector3.one * 0.95f;

        TileData data = tile.GetComponent<TileData>();
        if(data == null) data = tile.AddComponent<TileData>();

        data.gridPosition = new Vector2(x, z);
        data.height = y;

        Renderer rend = tile.GetComponent<Renderer>();

        // Application du Matériau (SharedMaterial est crucial pour éviter le bug de brillance)
        if (terrainMaterials != null && type < terrainMaterials.Length && terrainMaterials[type] != null)
        {
            rend.sharedMaterial = terrainMaterials[type];
        }

        switch (type)
        {
            case 0: data.isWalkable = false; break; // EAU
            case 1: data.isWalkable = true; data.movementCost = 1; break; // PLAGE
            case 2: data.isWalkable = true; data.movementCost = 1; break; // HERBE
            case 3: data.isWalkable = true; data.movementCost = 2; break; // ROCHE
            case 4: data.isWalkable = false; break; // PIC
            default: data.isWalkable = false; break;
        }

        if (!mapGrid.ContainsKey(data.gridPosition)) mapGrid.Add(data.gridPosition, data);
    }

    void SpawnPlayerTeamRandomly()
    {
        List<UnitData> roster = GameData.Instance.selectedRoster;
        
        // MÉLANGE des positions '+' disponibles
        System.Random rnd = new System.Random();
        var shuffledSpawns = foundPlayerSpawns.OrderBy(x => rnd.Next()).ToList();

        if (shuffledSpawns.Count < roster.Count)
        {
            Debug.LogWarning($"⚠️ Manque de places '+' ! Héros: {roster.Count}, Places: {shuffledSpawns.Count}");
        }

        for (int i = 0; i < roster.Count; i++)
        {
            if (i >= shuffledSpawns.Count) break; // Plus de place

            UnitData data = roster[i];
            Vector2 spawnPos2D = shuffledSpawns[i];

            if (mapGrid.ContainsKey(spawnPos2D))
            {
                TileData spawnTile = mapGrid[spawnPos2D];
                
                Vector3 finalPos = new Vector3(spawnTile.gridPosition.x, spawnTile.height + 1.5f, spawnTile.gridPosition.y);
                GameObject unitObj = Instantiate(data.unitPrefab, finalPos, Quaternion.identity);
                
                UnitController controller = unitObj.GetComponent<UnitController>();
                controller.Initialize(data, true); 
                controller.gridPosition = spawnTile.gridPosition;
                spawnTile.currentUnit = controller;
            }
        }
    }

    void StartGame()
    {
        UnitController[] allUnitsArray = FindObjectsOfType<UnitController>();
        List<UnitController> combatList = new List<UnitController>(allUnitsArray);

        // Récupérer uniquement les ennemis (ceux déjà présents dans la scène)
        List<UnitController> enemies = combatList.Where(u => !u.isPlayerTeam).ToList();

        // MÉLANGE des positions '-' disponibles
        System.Random rnd = new System.Random();
        var shuffledEnemySpawns = foundEnemySpawns.OrderBy(x => rnd.Next()).ToList();

        int spawnIndex = 0;

        foreach (var enemy in enemies)
        {
            Vector2 targetPos;

            // Il reste des places '-'
            if (spawnIndex < shuffledEnemySpawns.Count)
            {
                targetPos = shuffledEnemySpawns[spawnIndex];
                spawnIndex++;
            }
            // Plus de place '-' (ou pas définies), l'ennemi reste là où il est posé
            else
            {
                targetPos = new Vector2(Mathf.RoundToInt(enemy.transform.position.x), Mathf.RoundToInt(enemy.transform.position.z));
                Debug.LogWarning($"L'ennemi {enemy.name} n'a pas trouvé de marqueur '-' libre, il reste en {targetPos}.");
            }

            // Déplacement Physique et Logique
            if (mapGrid.ContainsKey(targetPos))
            {
                TileData tile = mapGrid[targetPos];
                
                // On téléporte l'ennemi
                enemy.transform.position = new Vector3(targetPos.x, tile.height + 0.5f, targetPos.y);
                
                // On met à jour la grille
                enemy.gridPosition = targetPos;
                tile.currentUnit = enemy;
            }
            
            // Init Data
            if(enemy.data != null) enemy.Initialize(enemy.data, false);
        }

        GameManager.Instance.StartCombat(combatList);
    }
    
    // Gizmos pour voir les zones dans l'éditeur
    void OnDrawGizmos()
    {
        // Joueurs (+) en VERT
        Gizmos.color = Color.green;
        foreach(var pos in foundPlayerSpawns)
        {
            Gizmos.DrawSphere(new Vector3(pos.x, 2, pos.y), 0.3f);
        }

        // Ennemis (-) en ROUGE
        Gizmos.color = Color.red;
        foreach(var pos in foundEnemySpawns)
        {
            Gizmos.DrawSphere(new Vector3(pos.x, 2, pos.y), 0.3f);
        }
    }
}