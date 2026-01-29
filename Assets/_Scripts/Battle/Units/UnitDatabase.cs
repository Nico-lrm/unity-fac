using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "DB_Global", menuName = "Tactical/Unit Database")]
public class UnitDatabase : ScriptableObject
{
    public List<UnitData> allUnits;
}