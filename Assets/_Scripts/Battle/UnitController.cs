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

    [Header("UI & Feedback")]
    public GameObject turnIndicatorPrefab; 
    private GameObject myIndicatorInstance;

    // Skill sélectionné (null = Attaque de base)
    public SkillData selectedSkill; 

    // Accesseurs rapides (Pour l'UI et les calculs)
    public string unitName => data != null ? data.unitName : "Inconnu";
    public int maxHP => data != null ? data.maxHP : 20;
    public int speed => data != null ? data.speed : 3;
    public int attackDamage => data != null ? data.attackDamage : 0; 
    
    // Récupère la portée (soit celle du skill, soit celle de l'unité de base)
    public int GetMinRange() => (selectedSkill != null) ? 0 : (data != null ? data.minRange : 1);
    public int GetMaxRange() => (selectedSkill != null) ? selectedSkill.range : (data != null ? data.maxRange : 1);
    public int GetPower() => (selectedSkill != null) ? selectedSkill.power : (data != null ? data.attackDamage : 1);
    public int GetAPCost() => (selectedSkill != null) ? selectedSkill.apCost : 3;

	[Header("Visuel & Animation")]
    public Transform damagePopupPrefab;
    public Transform firePoint;
    public float rotationSpeed = 10f;
    private Animator anim;

    // Logique Grille
    public Vector2 gridPosition;
    public Dictionary<TileData, int> validMoveTiles = new Dictionary<TileData, int>();
    public bool hasMovedThisTurn = false;
    
    // État du combat
    private bool isTargetingMode = false;
    
    private EnemyAI myAI;

    // Fonction publique pour l'UI (TurnPortrait)
    public bool IsInTargetMode() { return isTargetingMode; }

    void Awake() 
    { 
        myAI = GetComponent<EnemyAI>(); 
        anim = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        // On crée le cercle dès le début et on le cache
        if (turnIndicatorPrefab != null)
        {
            myIndicatorInstance = Instantiate(turnIndicatorPrefab, transform);
            // On le place aux pieds (0, 0.05, 0)
            myIndicatorInstance.transform.localPosition = new Vector3(0, 0.05f, 0);

            // On configure la couleur
            TurnIndicator indicatorScript = myIndicatorInstance.GetComponent<TurnIndicator>();
            if (indicatorScript != null) indicatorScript.Setup(isPlayerTeam);

            myIndicatorInstance.SetActive(false); // Caché par défaut
        }
    }

    public void Initialize(UnitData unitData, bool isPlayer)
    {
        data = unitData;
        isPlayerTeam = isPlayer;
        currentHP = maxHP; 
        
        maxAP = 6;
        if(data != null)
        {
            if (data.pieceType == ChessType.King) maxAP = 8;
            if (data.pieceType == ChessType.Pawn) maxAP = 4;
        }
        currentAP = maxAP;

        UpdateTeamColor();
    }

    void UpdateTeamColor()
    {
        // On cherche le Renderer (Le modèle 3D) dans les enfants
        Renderer r = GetComponentInChildren<Renderer>();
        if (r != null)
        {
            // On définit la couleur : Cyan pour Joueur, Rouge Vif pour Ennemi
            Color teamColor = isPlayerTeam ? Color.cyan : new Color(1f, 0.2f, 0.2f); // Rouge un peu orangé
            
            // On applique la couleur au paramètre "_RimColor" du shader
            r.material.SetColor("_RimColor", teamColor);
        }
    }
    
    void Update()
    {
        if (GameManager.Instance.activeUnit == this && isPlayerTeam)
        {
            // Bloque les clics si on est sur l'UI (Boutons, etc.)
            if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                return;
                
            HandleInput();
        }
    }
    
    void HandleInput()
    {
        // --- 1. RACCOURCIS CLAVIER ---
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Ampersand)) EnterCombatMode(null);
        if (Input.GetKeyDown(KeyCode.Alpha2) && data.skills.Count > 0 && UIManager.Instance != null) UIManager.Instance.OpenSkillMenu();
        
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (CameraFollow.Instance != null && !CameraFollow.Instance.isLockedOnUnit) CameraFollow.Instance.ResetCameraOnActiveUnit();
            else { GameManager.Instance.EndTurn(); if (UIManager.Instance != null) UIManager.Instance.CloseAllMenus(); }
        }

        // --- 2. SOURIS ---
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                UnitController clickedUnit = hit.collider.GetComponent<UnitController>();
                TileData clickedTile = hit.collider.GetComponent<TileData>();

                // MODE CIBLAGE (Attaque ou Skill)
                if (isTargetingMode)
                {
                    if (clickedUnit != null)
                    {
                        // --- CALCUL DE DISTANCE ---
                        int distX = Mathf.Abs((int)clickedUnit.gridPosition.x - (int)gridPosition.x);
                        int distY = Mathf.Abs((int)clickedUnit.gridPosition.y - (int)gridPosition.y);
                        int distInt = distX + distY; 

                        if (distInt >= GetMinRange() && distInt <= GetMaxRange())
                        {
                            bool isHeal = selectedSkill != null && selectedSkill.isHeal;
                            // Si soin : cible allié. Si attaque : cible ennemi.
                            bool validTarget = isHeal ? (clickedUnit.isPlayerTeam == isPlayerTeam) : (clickedUnit.isPlayerTeam != isPlayerTeam);

                            if (validTarget) PerformCombatAction(clickedUnit);
                            else if (UIManager.Instance != null) UIManager.Instance.ShowAnnouncement("Cible Invalide", Color.yellow, 1f);
                        }
                        else if (UIManager.Instance != null) UIManager.Instance.ShowAnnouncement("Hors de Portée", Color.yellow, 1f);
                    }
                    else if (clickedTile != null) 
                    {
                        ExitTargetMode(); // Clic dans le vide = Annuler
                    }
                    return;
                }

                // MENU D'ACTION (Clic sur soi-même)
                if (clickedUnit == this)
                {
                    if (!hasMovedThisTurn) ShowWalkableTiles();
                    if (UIManager.Instance != null) UIManager.Instance.ShowActionMenu(true, () => EnterCombatMode(null), null); 
                    return;
                }

                // DÉPLACEMENT
                if (!hasMovedThisTurn && clickedTile != null && validMoveTiles.ContainsKey(clickedTile))
                {
                    StartCoroutine(MoveChessPiece(clickedTile));
                    if (UIManager.Instance != null) UIManager.Instance.CloseAllMenus();
                }
            }
        }
        
        // Clic Droit = Annuler / Retour
        if (Input.GetMouseButtonDown(1))
        {
             ExitTargetMode();
             if (UIManager.Instance != null) UIManager.Instance.CloseAllMenus();
        }
    }

    // --- LOGIQUE DE COMBAT ---

    public void EnterCombatMode(SkillData skill)
    {
        selectedSkill = skill;
        int cost = GetAPCost();

        if (currentAP < cost) 
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowAnnouncement("Pas assez de PA", Color.red, 1f);
            return;
        }

        isTargetingMode = true;
        ShowCombatRange();
        if (UIManager.Instance != null) UIManager.Instance.CloseAllMenus();
    }

    void ExitTargetMode()
    {
        isTargetingMode = false;
        selectedSkill = null;
        ClearHighlights();
    }

    public void ShowCombatRange()
    {
        ClearHighlights();
        
        int minR = GetMinRange();
        int maxR = GetMaxRange();
        bool isHeal = selectedSkill != null && selectedSkill.isHeal;
        Color highlightColor = isHeal ? Color.green : Color.red;

        foreach(var kvp in MapGenerator.mapGrid)
        {
            TileData tile = kvp.Value;
            Vector2 tilePos = tile.gridPosition;

            // --- CALCUL DE DISTANCE ---
            int distX = Mathf.Abs((int)tilePos.x - (int)gridPosition.x);
            int distY = Mathf.Abs((int)tilePos.y - (int)gridPosition.y);
            int distanceEnCases = distX + distY;

            if (distanceEnCases >= minR && distanceEnCases <= maxR)
            {
                Renderer r = tile.GetComponent<Renderer>();
                if(r != null) r.material.color = highlightColor;
            }
        }
    }

    public void PerformCombatAction(UnitController target)
    {
        int cost = GetAPCost();
        if(currentAP < cost) return;
        currentAP -= cost;

        // On capture le skill AVANT de reset
        SkillData skillUsed = selectedSkill; 

        StartCoroutine(CombatSequence(target, skillUsed)); 
        
        isTargetingMode = false;
        selectedSkill = null;
        ClearHighlights();
    }

   IEnumerator CombatSequence(UnitController target, SkillData skillUsed)
    {
        if (skillUsed != null && skillUsed.isLeapAttack)
        {
            TileData jumpDest = GetFreeTileNextTo(target);

            if (jumpDest != null)
            {
                if (UIManager.Instance != null) UIManager.Instance.ShowAnnouncement("Saut !", Color.cyan, 0.5f);

                if (MapGenerator.mapGrid.ContainsKey(gridPosition)) 
                    MapGenerator.mapGrid[gridPosition].currentUnit = null;

                yield return StartCoroutine(JumpAnimation(transform.position, jumpDest.transform.position + Vector3.up * 0.5f, 0.6f));

                gridPosition = jumpDest.gridPosition;
                jumpDest.currentUnit = this;
            }
            else
            {
                if (UIManager.Instance != null) UIManager.Instance.AddToLog("Pas de place pour atterrir !");
            }
        }
        Vector3 targetDir = target.transform.position;
        float turnTime = 0f;
        while(turnTime < 0.2f) { LookAtTarget(targetDir); turnTime += Time.deltaTime; yield return null; }
        
        // UI Annonce
        if (UIManager.Instance != null)
        {
            string attackName = (skillUsed != null) ? skillUsed.skillName : "Attaque";
            Color textColor = (skillUsed != null && skillUsed.isHeal) ? Color.green : Color.white;
            UIManager.Instance.ShowAnnouncement(attackName, textColor, 1.0f);
        }

        // 2. LANCER L'ANIMATION
        string animTrigger = (skillUsed != null && !string.IsNullOrEmpty(skillUsed.animTrigger)) ? skillUsed.animTrigger : "DoAttack";
        if (anim != null) anim.SetTrigger(animTrigger);
        // On calcule combien de temps on doit attendre pour que l'anim soit au bon moment
        float delay = (skillUsed != null) ? skillUsed.castDelay : (data != null ? data.attackAnimDelay : 0.3f);
        
        // On attend que le bras soit levé / l'épée soit en bas
        yield return new WaitForSeconds(delay);

        // 3. SON & VFX 
        AudioClip clipToPlay = (skillUsed != null) ? skillUsed.castSound : (data != null ? data.attackSound : null);
        if (AudioManager.Instance != null && clipToPlay != null) AudioManager.Instance.PlaySFX(clipToPlay);

        // Calcul positions
        Vector3 startPoint = (firePoint != null) ? firePoint.position : transform.position + Vector3.up;
        Vector3 endPoint = target.transform.position + Vector3.up;

        // Instantiation Laser / Projectile
        if (skillUsed != null && skillUsed.castVFX != null)
        {
            GameObject vfxObj = Instantiate(skillUsed.castVFX, Vector3.zero, Quaternion.identity);
            LaserEffect laser = vfxObj.GetComponent<LaserEffect>();
            if (laser != null) laser.Setup(startPoint, endPoint);
            else vfxObj.transform.position = startPoint; 
        }

        // 4. PETIT DÉLAI DE VOL (Pour que le laser ait le temps d'arriver)
        yield return new WaitForSeconds(0.1f); 
        
        // 5. IMPACT & DÉGÂTS
        GameObject hitPrefab = (skillUsed != null) ? skillUsed.hitVFX : (data != null ? data.hitVFX : null);
        if (hitPrefab != null) Instantiate(hitPrefab, endPoint, Quaternion.identity);

        int power = (skillUsed != null) ? skillUsed.power : attackDamage;
        bool isHeal = skillUsed != null && skillUsed.isHeal;

        if (UIManager.Instance != null)
        {
            string colorAttacker = isPlayerTeam ? "#00FFFF" : "#FF5555"; // Cyan ou Rouge
            string colorTarget = target.isPlayerTeam ? "#00FFFF" : "#FF5555";
            string actionName = (skillUsed != null) ? skillUsed.skillName : "Coup simple";
            string sign = isHeal ? "+" : "-";
            string colorResult = isHeal ? "green" : "orange";

            string msg = $"<color={colorAttacker}>{unitName}</color> utilise <b>{actionName}</b> sur <color={colorTarget}>{target.unitName}</color> (<color={colorResult}>{sign}{power} PV</color>)";
            UIManager.Instance.AddToLog(msg);
        }

        if (isHeal) target.Heal(power);
        else
        {
            target.TakeDamage(power);
            
            if (GameManager.Instance != null && GameManager.Instance.currentMission != null)
            {
                var mechanic = GameManager.Instance.currentMission.activeMechanic;
                if (mechanic != null)
                {
                    mechanic.OnAttackHit(this, target);
                }
            }
        }
        
        // On laisse le temps à l'anim de finir tranquillement
        yield return new WaitForSeconds(0.5f);

        if (UIManager.Instance != null) 
        {
            UIManager.Instance.UpdateActiveUnitPanel(this);
        }
        
        CheckEndTurn();
    }

    // --- MOUVEMENTS ---
    
    public void CalculateChessMoves()
    {
        validMoveTiles.Clear();
        if (hasMovedThisTurn || data == null) return;
        if (!MapGenerator.mapGrid.ContainsKey(gridPosition)) return;

        TileData startTile = MapGenerator.mapGrid[gridPosition];
        Vector2[] ortho = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        Vector2[] diags = { new Vector2(1, 1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(-1, -1) };
        Vector2[] knightJumps = { new Vector2(1, 2), new Vector2(2, 1), new Vector2(2, -1), new Vector2(1, -2), new Vector2(-1, -2), new Vector2(-2, -1), new Vector2(-2, 1), new Vector2(-1, 2) };

        switch (data.pieceType)
        {
            case ChessType.King: CheckSlideMoves(startTile, ortho, 1); CheckSlideMoves(startTile, diags, 1); break;
            case ChessType.Queen: CheckSlideMoves(startTile, ortho, 99); CheckSlideMoves(startTile, diags, 99); break;
            case ChessType.Rook: CheckSlideMoves(startTile, ortho, 99); break;
            case ChessType.Bishop: CheckSlideMoves(startTile, diags, 99); break;
            case ChessType.Knight: CheckJumpMoves(startTile, knightJumps, 3); break;
            case ChessType.Pawn: CheckSlideMoves(startTile, ortho, 2); break;
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
                if (!tile.isWalkable || tile.currentUnit != null) break; 
                if (Mathf.Abs(tile.height - startNode.height) > 1) break;

                int cost = i;
                if (currentAP >= cost) { if (!validMoveTiles.ContainsKey(tile)) validMoveTiles.Add(tile, cost); } else break; 
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
                if (tile.isWalkable && tile.currentUnit == null && Mathf.Abs(tile.height - startNode.height) <= 1)
                {
                    validMoveTiles.Add(tile, fixedCost);
                }
            }
        }
    }

    // --- ANIMATIONS & UTILS ---

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
        
        if (MapGenerator.mapGrid.ContainsKey(gridPosition)) MapGenerator.mapGrid[gridPosition].currentUnit = null;

        Vector3 startPos = transform.position;
        Vector3 endPos = new Vector3(targetTile.gridPosition.x, targetTile.height + 0.5f, targetTile.gridPosition.y);
        
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

        if (isPlayerTeam && UIManager.Instance != null) UIManager.Instance.UpdateActiveUnitPanel(this);
    }

    public void Heal(int amount)
    {
        currentHP = Mathf.Min(currentHP + amount, maxHP);

        if (UIManager.Instance != null)
        {
            string cName = isPlayerTeam ? "#00FFFF" : "#FF5555";
            UIManager.Instance.AddToLog($"<color={cName}>{unitName}</color> reçoit du soin : <color=green>+{amount} PV</color>");
        }


        if (damagePopupPrefab != null) 
        { 
            Transform p = Instantiate(damagePopupPrefab, transform.position + Vector3.up * 3f, Quaternion.identity); 
            p.GetComponent<DamagePopup>().Setup(amount, true); // TRUE pour Soin
        }
        if (UIManager.Instance != null) UIManager.Instance.UpdateActiveUnitPanel(this);
    }

    public void TakeDamage(int amount)
    {
        currentHP -= amount;
        if (damagePopupPrefab != null) 
        { 
            Transform p = Instantiate(damagePopupPrefab, transform.position + Vector3.up * 3f, Quaternion.identity); 
            p.GetComponent<DamagePopup>().Setup(amount, false); 
        }
        if (currentHP > 0 && anim != null) anim.SetTrigger("DoHit");
        if (currentHP <= 0)
        {
            if (UIManager.Instance != null)
            {
                string cName = isPlayerTeam ? "#00FFFF" : "#FF5555";
                UIManager.Instance.AddToLog($"<color={cName}>{unitName}</color> a été <color=red>ÉLIMINÉ</color> !");
            }

            StartCoroutine(DieSequence());
        }

        if (GameManager.Instance.activeUnit == this)
            UIManager.Instance.UpdateActiveUnitPanel(this);
        if (UIManager.Instance != null) UIManager.Instance.UpdateActiveUnitPanel(this);
    }

    public void ConsumeAP(int cost)
    {
        currentAP -= cost;

        if (GameManager.Instance.activeUnit == this)
            UIManager.Instance.UpdateActiveUnitPanel(this);
    }

    IEnumerator DieSequence()
    {
        if (anim != null) anim.SetTrigger("DoDie");
        yield return new WaitForSeconds(2.5f);
        if (MapGenerator.mapGrid.ContainsKey(gridPosition)) MapGenerator.mapGrid[gridPosition].currentUnit = null;
        gameObject.SetActive(false);
        GameManager.Instance.OnUnitDied(this);
    }

    public void BeginTurn()
    {
        currentAP = maxAP;
        hasMovedThisTurn = false;
        
        if (GameManager.Instance != null && GameManager.Instance.currentMission != null)
        {
            var mechanic = GameManager.Instance.currentMission.activeMechanic;
            if (mechanic != null)
            {
                mechanic.OnTurnStart(this);
            }
        }

        if (myIndicatorInstance != null) myIndicatorInstance.SetActive(true);

        if (UIManager.Instance != null) UIManager.Instance.UpdateActiveUnitPanel(this);

        if (isPlayerTeam) 
        {
            CalculateChessMoves(); 
        }
        else 
        {
            var ai = GetComponent<EnemyAI>();
            if (myAI != null) myAI.DoTurn();
            else { Debug.LogError($"🛑 ERREUR : {name} n'a pas d'EnemyAI !"); GameManager.Instance.EndTurn(); }
        }
    }

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

    public void ClearHighlights()
    {
        foreach (var tile in MapGenerator.mapGrid.Values)
        {
            Renderer r = tile.GetComponent<Renderer>();
            if (r != null)
            {
                int type = tile.height;
                if (MapGenerator.Instance.terrainMaterials != null && type >= 0 && type < MapGenerator.Instance.terrainMaterials.Length)
                {
                    r.sharedMaterial = MapGenerator.Instance.terrainMaterials[type];
                }
            }
        }
    }
    
    public void ForceMoveTo(TileData targetTile)
    {
        if (MapGenerator.mapGrid.ContainsKey(gridPosition))
            MapGenerator.mapGrid[gridPosition].currentUnit = null;

        gridPosition = targetTile.gridPosition;
        targetTile.currentUnit = this;

        transform.position = new Vector3(targetTile.gridPosition.x, targetTile.height + 0.5f, targetTile.gridPosition.y);
        
        if (UIManager.Instance != null) UIManager.Instance.AddToLog($"{unitName} est repoussé !");
    }
    
    TileData GetFreeTileNextTo(UnitController target)
    {
        Vector2[] offsets = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        
        TileData bestTile = null;
        float minDistance = 999f;

        foreach (Vector2 off in offsets)
        {
            Vector2 checkPos = target.gridPosition + off;
            
            if (MapGenerator.mapGrid.ContainsKey(checkPos))
            {
                TileData t = MapGenerator.mapGrid[checkPos];
                if (t.isWalkable && (t.currentUnit == null || t.currentUnit == this))
                {
                    float dist = Vector2.Distance(transform.position, t.transform.position);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        bestTile = t;
                    }
                }
            }
        }
        return bestTile;
    }

    IEnumerator JumpAnimation(Vector3 startPos, Vector3 endPos, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            Vector3 currentPos = Vector3.Lerp(startPos, endPos, t);
            
            currentPos.y += Mathf.Sin(t * Mathf.PI) * 2.0f; 

            transform.position = currentPos;
            yield return null;
        }
        transform.position = endPos;
    }

    public void EndTurnLogic() 
    {
        if (myIndicatorInstance != null) myIndicatorInstance.SetActive(false);
        ClearHighlights(); 
    }

    void CheckEndTurn() { if (currentAP <= -1 && isPlayerTeam) GameManager.Instance.EndTurn(); }
}