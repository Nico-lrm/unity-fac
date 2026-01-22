using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UnitController : MonoBehaviour
{
    [Header("Données")]
    public UnitData data; 
    public bool isPlayerTeam;

    [Header("Stats")]
    public int currentHP;
    public int currentAP;
    public int maxAP = 6;
    public int attackCost = 3; 

    // Accesseurs
    public string unitName => data != null ? data.unitName : "Unassigned";
    public int maxHP => data != null ? data.maxHP : 20;
    public int speed => data != null ? data.speed : 0;
    public int attackDamage => data != null ? data.attackDamage : 0;

    [Header("Visuel & Animation")]
    public Transform damagePopupPrefab;
    public float rotationSpeed = 10f;
    private Animator anim; 

    // Logique Grille
    public Vector2 gridPosition;
    public Dictionary<TileData, int> validMoveTiles = new Dictionary<TileData, int>();
    public bool hasMovedThisTurn = false;
    private bool isTargetingAttack = false;
    
    private EnemyAI myAI;

    void Awake() 
    { 
        myAI = GetComponent<EnemyAI>(); 
        anim = GetComponentInChildren<Animator>();
    }

    public void Initialize(UnitData unitData, bool isPlayer)
    {
        data = unitData;
        isPlayerTeam = isPlayer;
        currentHP = maxHP; 
        
        // Ajustement des PA selon la classe
        maxAP = 6;
        if(data != null)
        {
            if (data.pieceType == ChessType.King) maxAP = 8;
            if (data.pieceType == ChessType.Pawn) maxAP = 4;
        }
        currentAP = maxAP;
    }
    
    void Update()
    {
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

                // 1. Mode Attaque
                if (isTargetingAttack)
                {
                    if (clickedUnit != null && !clickedUnit.isPlayerTeam)
                    {
                        if (Vector3.Distance(transform.position, clickedUnit.transform.position) <= 2.0f)
                        {
                            PerformAttack(clickedUnit);
                            isTargetingAttack = false;
                            ClearHighlights();
                        }
                    }
                    else if (clickedTile != null) 
                    {
                        isTargetingAttack = false;
                        ClearHighlights();
                    }
                    return;
                }

                // 2. Sélection de soi-même (Menu)
                if (clickedUnit == this)
                {
                    if (!hasMovedThisTurn) ShowWalkableTiles();
                    if (UIManager.Instance != null) 
                        UIManager.Instance.ShowActionMenu(true, () => EnterAttackMode(), null);
                    return;
                }

                // 3. Déplacement
                if (!hasMovedThisTurn && clickedTile != null && validMoveTiles.ContainsKey(clickedTile))
                {
                    StartCoroutine(MoveChessPiece(clickedTile));
                    if (UIManager.Instance != null) UIManager.Instance.CloseAllMenus();
                }
            }
        }
        
        // Clic Droit pour annuler
        if (Input.GetMouseButtonDown(1))
        {
             isTargetingAttack = false;
             ClearHighlights();
             if (UIManager.Instance != null) UIManager.Instance.CloseAllMenus();
        }
    }

    public void EnterAttackMode()
    {
        if (currentAP < attackCost) return;
        isTargetingAttack = true;
        ShowAttackRange();
        if (UIManager.Instance != null) UIManager.Instance.CloseAllMenus();
    }

    // --- LOGIQUE DE DÉPLACEMENT RESTAURÉE ---
    public void CalculateChessMoves()
    {
        validMoveTiles.Clear();
        if (hasMovedThisTurn || data == null) return;
        if (!MapGenerator.mapGrid.ContainsKey(gridPosition)) return;

        TileData startTile = MapGenerator.mapGrid[gridPosition];

        // Vecteurs de direction
        Vector2[] ortho = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        Vector2[] diags = { new Vector2(1, 1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(-1, -1) };
        Vector2[] knightJumps = { new Vector2(1, 2), new Vector2(2, 1), new Vector2(2, -1), new Vector2(1, -2), new Vector2(-1, -2), new Vector2(-2, -1), new Vector2(-2, 1), new Vector2(-1, 2) };

        // C'est ICI que la magie opère pour chaque pièce
        switch (data.pieceType)
        {
            case ChessType.King:
                CheckSlideMoves(startTile, ortho, 1);
                CheckSlideMoves(startTile, diags, 1);
                break;
            case ChessType.Queen:
                CheckSlideMoves(startTile, ortho, 99);
                CheckSlideMoves(startTile, diags, 99);
                break;
            case ChessType.Rook:
                CheckSlideMoves(startTile, ortho, 99);
                break;
            case ChessType.Bishop:
                CheckSlideMoves(startTile, diags, 99);
                break;
            case ChessType.Knight: // Le Cavalier SAUTE
                CheckJumpMoves(startTile, knightJumps, 3);
                break;
            case ChessType.Pawn:
                CheckSlideMoves(startTile, ortho, 2); // Le pion bouge un peu moins loin
                break;
        }
    }

    void CheckSlideMoves(TileData startNode, Vector2[] directions, int maxDist)
    {
        foreach (Vector2 dir in directions)
        {
            for (int i = 1; i <= maxDist; i++)
            {
                Vector2 targetPos = startNode.gridPosition + (dir * i);
                if (!MapGenerator.mapGrid.ContainsKey(targetPos)) break;
                TileData tile = MapGenerator.mapGrid[targetPos];
                
                // Obstacle ou unité ? On arrête
                if (!tile.isWalkable || tile.currentUnit != null) break; 
                // Trop haut ? On arrête
                if (Mathf.Abs(tile.height - startNode.height) > 1) break;

                int cost = i;
                if (currentAP >= cost) 
                { 
                    if (!validMoveTiles.ContainsKey(tile)) validMoveTiles.Add(tile, cost); 
                } 
                else break; 
            }
        }
    }

    // Fonction indispensable pour le Cavalier (Knight)
    void CheckJumpMoves(TileData startNode, Vector2[] offsets, int fixedCost)
    {
        if (currentAP < fixedCost) return;

        foreach (Vector2 offset in offsets)
        {
            Vector2 targetPos = startNode.gridPosition + offset;
            
            if (MapGenerator.mapGrid.ContainsKey(targetPos))
            {
                TileData tile = MapGenerator.mapGrid[targetPos];

                // Le cavalier saute, donc il s'en fiche des obstacles sur le chemin
                // Il regarde juste la case d'arrivée : est-elle libre et accessible ?
                if (tile.isWalkable && tile.currentUnit == null)
                {
                    // On vérifie quand même la hauteur d'arrivée
                    if (Mathf.Abs(tile.height - startNode.height) <= 1)
                    {
                        validMoveTiles.Add(tile, fixedCost);
                    }
                }
            }
        }
    }

    // --- ANIMATIONS & DÉPLACEMENTS ---

    void LookAtTarget(Vector3 targetPoint)
    {
        Vector3 direction = (targetPoint - transform.position).normalized;
        direction.y = 0; 
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            if (anim != null) 
                anim.transform.rotation = Quaternion.Slerp(anim.transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        }
    }

    public IEnumerator MoveChessPiece(TileData targetTile)
    {
        int cost = validMoveTiles[targetTile];
        ClearHighlights();
        
        if (MapGenerator.mapGrid.ContainsKey(gridPosition)) 
            MapGenerator.mapGrid[gridPosition].currentUnit = null;

        Vector3 startPos = transform.position;
        Vector3 endPos = new Vector3(targetTile.gridPosition.x, targetTile.height + 1.5f, targetTile.gridPosition.y);
        
        if (anim != null) anim.SetTrigger("DoWalk");

        float elapsed = 0f;
        while (elapsed < 0.5f)
        {
            LookAtTarget(endPos);
            transform.position = Vector3.Lerp(startPos, endPos, elapsed / 0.5f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = endPos;

        if (anim != null) anim.SetTrigger("DoIdle");

        gridPosition = targetTile.gridPosition;
        targetTile.currentUnit = this;
        currentAP -= cost;
        hasMovedThisTurn = true;

        if (isPlayerTeam && UIManager.Instance != null) UIManager.Instance.UpdateUI(this);
    }

    public void PerformAttack(UnitController target)
    {
        if(currentAP < attackCost) return;
        currentAP -= attackCost;
        StartCoroutine(AttackSequence(target));
    }

    IEnumerator AttackSequence(UnitController target)
    {
        Vector3 targetDir = target.transform.position;
        float turnTime = 0f;
        while(turnTime < 0.2f) 
        { 
            LookAtTarget(targetDir); 
            turnTime += Time.deltaTime; 
            yield return null; 
        }
        
        if (anim != null) anim.SetTrigger("DoAttack");

        yield return new WaitForSeconds(0.5f);
        
        target.TakeDamage(attackDamage);
        
        yield return new WaitForSeconds(0.5f);

        if (isPlayerTeam && UIManager.Instance != null) UIManager.Instance.UpdateUI(this);
        CheckEndTurn();
    }

    // --- OUTILS & FIN DE TOUR ---

    public void BeginTurn()
    {
        currentAP = maxAP;
        hasMovedThisTurn = false;
        
        if (UIManager.Instance != null) UIManager.Instance.UpdateUI(this);

        if (isPlayerTeam) 
        {
            CalculateChessMoves(); 
        }
        else 
        {
            if (myAI != null) myAI.DoTurn();
            else
            {
                Debug.LogError($"🛑 ERREUR : {name} n'a pas d'EnemyAI ! Fin de tour forcée.");
                GameManager.Instance.EndTurn();
            }
        }
    }

    public void TakeDamage(int amount)
    {
        currentHP -= amount;
        if (damagePopupPrefab != null) { Transform p = Instantiate(damagePopupPrefab, transform.position + Vector3.up * 3f, Quaternion.identity); p.GetComponent<DamagePopup>().Setup(amount, false); }
        if (currentHP <= 0) Die();
    }

    void Die()
    {
        if (MapGenerator.mapGrid.ContainsKey(gridPosition)) MapGenerator.mapGrid[gridPosition].currentUnit = null;
        gameObject.SetActive(false);
        GameManager.Instance.OnUnitDied(this);
    }
    
    // --- GESTION AFFICHAGE ---
    public void ShowWalkableTiles()
    {
        ClearHighlights();
        CalculateChessMoves();
        foreach (var tile in validMoveTiles.Keys)
        {
            Renderer r = tile.GetComponent<Renderer>();
            if (r != null) r.material.color = Color.cyan;
        }
    }
    
    public void ShowAttackRange()
    {
        ClearHighlights();
        Collider[] hits = Physics.OverlapSphere(transform.position, 1.5f);
        foreach(var hit in hits) {
            TileData t = hit.GetComponent<TileData>();
            if(t != null) t.GetComponent<Renderer>().material.color = Color.red;
        }
    }

    public void ClearHighlights()
    {
        foreach (var tile in MapGenerator.mapGrid.Values)
        {
            Renderer r = tile.GetComponent<Renderer>();
            if (r != null)
            {
                // Remet le bon matériau selon la hauteur
                int type = tile.height;
                if (MapGenerator.Instance.terrainMaterials != null && type >= 0 && type < MapGenerator.Instance.terrainMaterials.Length)
                {
                    r.sharedMaterial = MapGenerator.Instance.terrainMaterials[type];
                }
            }
        }
    }

    public void EndTurnLogic() { ClearHighlights(); }
    void CheckEndTurn() { if (currentAP <= 0 && isPlayerTeam) GameManager.Instance.EndTurn(); }
}