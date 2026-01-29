using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Chapter_X", menuName = "Tactical/Chapter Data")]
public class ChapterData : ScriptableObject
{
    public string chapterName; // Ex: "Chapitre 1"
    public List<MissionData> missions; // La liste des missions dedans
}