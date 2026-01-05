using UnityEngine;

[CreateAssetMenu(fileName = "Nouveau Sort", menuName = "Tactical/Skill")]
public class SkillData : ScriptableObject
{
    public string skillName;
    public int apCost;
    public int range;
    public int power; // Dégâts ou Montant de soin
    public bool isHeal; // Coché = Soin (Vert), Décoché = Dégâts (Rouge)
    
    [TextArea]
    public string description;
}