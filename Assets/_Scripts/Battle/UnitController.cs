using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UnitController : MonoBehaviour
{
    [Header("Données")]
    public UnitData data; 
    public bool isPlayerTeam;

    [Header("Stats Dynamiques")]
    public int currentHP;
    public int currentAP;
    public int maxAP = 6;
    public int attackCost = 3; 

    // Pour l'interface et les autres scripts
    public string unitName => data != null ? data.unitName : "Unassigned";
    public int maxHP => data != null ? data.maxHP : 20;
    public int speed => data != null ? data.speed : 0;
    public int attackDamage => data != null ? data.attackDamage : 0;

    [Header("VFX")]
    public Transform damagePopupPrefab; 

    // État du tour
    public Vector2 gridPosition;
    public Dictionary<TileData, int> validMoveTiles = new Dictionary<TileData, int>();
    public bool hasMovedThisTurn = false; // Bloque le mouvement après une action
    private bool isTargetingAttack = false;
    
    private EnemyAI myAI;

    void Awake() { myAI = GetComponent<EnemyAI>(); }

    public void Initialize(UnitData unitData, bool isPlayer)
    {
        data = unitData;
        isPlayerTeam = isPlayer;
        currentHP = maxHP; 
        
        // Bonus de PA selon le type (facultatif, à ajuster selon ton équilibrage)
        maxAP = 6; 
        if(data != null)
        {
            switch (data.pieceType)
            {
                case ChessType.King: maxAP += 2; break;
                case ChessType.Pawn: maxAP = 3; break;
            }
        }
        currentAP = maxAP;
    }
    
    void Update()
    {
        // On n'écoute l'input que si c'est notre tour et qu'on est le joueur
        if (GameManager.Instance.activeUnit == this && isPlayerTeam)
        {
            if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                return;

            HandleInput();
        }
    }
    
    void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                UnitController clickedUnit = hit.collider.GetComponent<UnitController>();
                TileData clickedTile = hit.collider.GetComponent<TileData>();

                //MODE CIBLAGE (ATTAQUE)
                if (isTargetingAttack)
                {
                    if (clickedUnit != null && !clickedUnit.isPlayerTeam)
                    {
                        // Vérif distance
                        if (Vector3.Distance(transform.position, clickedUnit.transform.position) <= 2.0f)
                        {
                            PerformAttack(clickedUnit);
                            isTargetingAttack = false;
                            ClearHighlights(); // On nettoie les cases rouges
                        }
                    }
                    else if (clickedTile != null) // Clic dans le vide annule
                    {
                        isTargetingAttack = false;
                        ClearHighlights();
                    }
                    return;
                }

                // SÉLECTION DE SOI-MÊME
                if (clickedUnit == this)
                {
                    // Si on a déjà bougé, on ne montre PAS les cases bleues
                    if (!hasMovedThisTurn) ShowWalkableTiles();
                    
                    if (UIManager.Instance != null) 
                        UIManager.Instance.ShowActionMenu(true, () => EnterAttackMode(), null);
                    return;
                }

                // DÉPLACEMENT
                // On ne peut bouger que si on n'a pas encore bougé ce tour-ci
                if (!hasMovedThisTurn && clickedTile != null && validMoveTiles.ContainsKey(clickedTile))
                {
                    StartCoroutine(MoveChessPiece(clickedTile));
                    if (UIManager.Instance != null) UIManager.Instance.CloseAllMenus();
                }
            }
        }
        
        if (Input.GetMouseButtonDown(1)) // Clic Droit annule tout
        {
             isTargetingAttack = false;
             ClearHighlights();
             if (UIManager.Instance != null) UIManager.Instance.CloseAllMenus();
        }
    }

    // --- LOGIQUE DE DÉPLACEMENT (ECHECS + PA) ---

    public void CalculateChessMoves()
    {
        validMoveTiles.Clear();
        // Si on a déjà bougé, on ne calcule rien (liste vide)
        if (hasMovedThisTurn || data == null) return;
        
        if (!MapGenerator.mapGrid.ContainsKey(gridPosition)) return;
        TileData startTile = MapGenerator.mapGrid[gridPosition];

        Vector2[] lines = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        Vector2[] diags = { new Vector2(1, 1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(-1, -1) };
        Vector2[] knightMoves = { new Vector2(1, 2), new Vector2(2, 1), new Vector2(2, -1), new Vector2(1, -2), new Vector2(-1, -2), new Vector2(-2, -1), new Vector2(-2, 1), new Vector2(-1, 2) };

        switch (data.pieceType)
        {
            case ChessType.King:   CheckSlideMoves(startTile, lines, 1); CheckSlideMoves(startTile, diags, 1); break;
            case ChessType.Queen:  CheckSlideMoves(startTile, lines, 99); CheckSlideMoves(startTile, diags, 99); break;
            case ChessType.Rook:   CheckSlideMoves(startTile, lines, 99); break;
            case ChessType.Bishop: CheckSlideMoves(startTile, diags, 99); break;
            case ChessType.Pawn:   CheckSlideMoves(startTile, lines, 2); break;
            case ChessType.Knight: CheckJumpMoves(startTile, knightMoves, 3); break; // Cavalier coûte 3PA fixe pour sauter
        }
    }

    void CheckSlideMoves(TileData startNode, Vector2[] directions, int maxDist)
    {
        foreach (Vector2 dir in directions)
        {
            for (int i = 1; i <= maxDist; i++)
            {
                Vector2 targetPos = startNode.gridPosition + (dir * i);
                
                if (!MapGenerator.mapGrid.ContainsKey(targetPos)) break; // Hors map
                TileData tile = MapGenerator.mapGrid[targetPos];
                
                // Obstacles
                if (!tile.isWalkable || tile.currentUnit != null) break; 
                if (Mathf.Abs(tile.height - startNode.height) > 1) break; // Trop haut

                // COÛT : 1 case = 1 PA (donc i cases = i PA)
                int cost = i;

                if (currentAP >= cost)
                {
                    if (!validMoveTiles.ContainsKey(tile)) validMoveTiles.Add(tile, cost);
                }
                else break; // Plus assez de PA pour aller plus loin
            }
        }
    }

    void CheckJumpMoves(TileData startNode, Vector2[] offsets, int fixedCost)
    {
        if (currentAP < fixedCost) return;

        foreach (Vector2 offset in offsets)
        {
            Vector2 targetPos = startNode.gridPosition + offset;
            if (MapGenerator.mapGrid.ContainsKey(targetPos))
            {
                TileData tile = MapGenerator.mapGrid[targetPos];
                // Le cavalier saute, donc il ignore les unités sur le chemin, mais pas sur l'arrivée
                if (tile.isWalkable && tile.currentUnit == null && Mathf.Abs(tile.height - startNode.height) <= 1)
                {
                    validMoveTiles.Add(tile, fixedCost);
                }
            }
        }
    }

    public IEnumerator MoveChessPiece(TileData targetTile)
    {
        int cost = validMoveTiles[targetTile];
        ClearHighlights(); // Efface les cases bleues immédiatement
        
        // Libère l'ancienne case
        if (MapGenerator.mapGrid.ContainsKey(gridPosition)) 
            MapGenerator.mapGrid[gridPosition].currentUnit = null;

        // Animation
        Vector3 startPos = transform.position;
        Vector3 endPos = new Vector3(targetTile.gridPosition.x, targetTile.height + 1.5f, targetTile.gridPosition.y);
        float elapsed = 0f;
        while (elapsed < 0.3f)
        {
            transform.position = Vector3.Lerp(startPos, endPos, elapsed / 0.3f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = endPos;

        // Mise à jour données
        gridPosition = targetTile.gridPosition;
        targetTile.currentUnit = this;
        
        currentAP -= cost;
        hasMovedThisTurn = true; // Empêche de rebouger ce tour

        if (isPlayerTeam && UIManager.Instance != null) UIManager.Instance.UpdateUI(this);
    }

    // --- COMBAT & ACTIONS ---

    public void EnterAttackMode()
    {
        if (currentAP < attackCost) return;
        isTargetingAttack = true;
        ShowAttackRange(); // Affiche cases rouges
        if (UIManager.Instance != null) UIManager.Instance.CloseAllMenus();
    }

    public void PerformAttack(UnitController target)
    {
        if(currentAP < attackCost) return;
        currentAP -= attackCost;
        
        StartCoroutine(AttackAnimation(target.transform.position));
        target.TakeDamage(attackDamage);
        
        if (isPlayerTeam && UIManager.Instance != null) UIManager.Instance.UpdateUI(this);
        CheckEndTurn();
    }

    public void TakeDamage(int amount)
    {
        currentHP -= amount;
        if (damagePopupPrefab != null)
        {
             Transform popup = Instantiate(damagePopupPrefab, transform.position + Vector3.up * 3f, Quaternion.identity);
             popup.GetComponent<DamagePopup>().Setup(amount, false);
        }
        if (currentHP <= 0) Die();
    }

    void Die()
    {
        // On libère la case sur la grille
        if (MapGenerator.mapGrid.ContainsKey(gridPosition)) 
            MapGenerator.mapGrid[gridPosition].currentUnit = null;
            
        gameObject.SetActive(false);

        // On prévient le GameManager spécifiquement qu'une unité est morte
        GameManager.Instance.OnUnitDied(this);
    }

    // --- VISUELS ---

    public void ShowWalkableTiles()
    {
        ClearHighlights();
        CalculateChessMoves(); // On recalcule pour être sûr
        foreach (var tile in validMoveTiles.Keys)
        {
            Renderer r = tile.GetComponent<Renderer>();
            if (r != null) r.material.color = Color.cyan; // BLEU pour déplacement
        }
    }

    public void ShowAttackRange()
    {
        ClearHighlights();
        // Portée simple de 1 case autour
        Collider[] hits = Physics.OverlapSphere(transform.position, 1.5f);
        foreach(var hit in hits)
        {
            TileData t = hit.GetComponent<TileData>();
            if(t != null)
            {
                Renderer r = t.GetComponent<Renderer>();
                if(r != null) r.material.color = Color.red; // ROUGE pour attaque
            }
        }
    }

    public void ClearHighlights()
    {
        // On parcourt toutes les tuiles de la map
        foreach (var tile in MapGenerator.mapGrid.Values)
        {
            Renderer r = tile.GetComponent<Renderer>();
            if (r != null)
            {
                
                int type = tile.height;
                
                if (MapGenerator.Instance.terrainMaterials != null && 
                    type >= 0 && 
                    type < MapGenerator.Instance.terrainMaterials.Length)
                {
                    // sharedMaterial est important pour la performance
                    r.sharedMaterial = MapGenerator.Instance.terrainMaterials[type];
                }
            }
        }
    }

    // --- OUTILS ---
    public void BeginTurn()
    {
        currentAP = maxAP;
        hasMovedThisTurn = false; // Reset du mouvement pour le nouveau tour
        
        if (UIManager.Instance != null) UIManager.Instance.UpdateUI(this);

        if (isPlayerTeam) 
        {
            CalculateChessMoves(); 
            // On ne montre pas les cases tout de suite, on attend le clic du joueur
        }
        else if (myAI != null) 
        {
            myAI.DoTurn();
        }
    }

    public void EndTurnLogic() { ClearHighlights(); }
    void CheckEndTurn() { if (currentAP <= 0 && isPlayerTeam) GameManager.Instance.EndTurn(); }
    
    IEnumerator AttackAnimation(Vector3 targetPos)
    {
        Vector3 originalPos = transform.position;
        Vector3 dir = (targetPos - transform.position).normalized;
        float t = 0;
        while(t < 0.1f) { transform.position += dir * 5f * Time.deltaTime; t+=Time.deltaTime; yield return null; }
        while(t > 0) { transform.position -= dir * 5f * Time.deltaTime; t-=Time.deltaTime; yield return null; }
        transform.position = originalPos;
    }
}