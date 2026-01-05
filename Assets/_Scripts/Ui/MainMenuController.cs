using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class MainMenuController : MonoBehaviour
{
    [Header("Panneaux")]
    public GameObject mainPanel;
    public GameObject chaptersPanel;
    public GameObject optionsPanel;
    public GameObject missionListPanel; // NOUVEAU

    [Header("Mission List Components")]
    public TextMeshProUGUI chapterTitleText; // Pour afficher "Chapitre 1"
    public Transform missionContainer;       // L'endroit où on met les boutons
    public GameObject missionButtonPrefab;   // Le modèle de bouton

    [Header("Options Components")]
    public Slider volumeSlider;
    public TMP_Dropdown qualityDropdown;

    void Start()
    {
        ShowMain();
        if (volumeSlider != null) { volumeSlider.value = AudioListener.volume; volumeSlider.onValueChanged.AddListener(SetVolume); }
        if (qualityDropdown != null) { qualityDropdown.value = QualitySettings.GetQualityLevel(); qualityDropdown.onValueChanged.AddListener(SetQuality); }
    }

    // --- NAVIGATION ---

    public void ShowMain()
    {
        CloseAll();
        mainPanel.SetActive(true);
    }

    public void ShowChapters()
    {
        CloseAll();
        chaptersPanel.SetActive(true);
    }

    public void ShowOptions()
    {
        CloseAll();
        optionsPanel.SetActive(true);
    }

    void CloseAll()
    {
        mainPanel.SetActive(false);
        chaptersPanel.SetActive(false);
        optionsPanel.SetActive(false);
        missionListPanel.SetActive(false);
    }

    // --- LOGIQUE CHAPITRE & MISSION (C'est ici que ça change) ---

    // Cette fonction est appelée par le bouton "Chapitre 1"
    public void OpenChapter(ChapterData chapter)
    {
        CloseAll();
        missionListPanel.SetActive(true);

        // Mise à jour du titre
        if(chapterTitleText != null) chapterTitleText.text = chapter.chapterName;

        // Nettoyage de la liste précédente
        foreach (Transform child in missionContainer) Destroy(child.gameObject);

        // Création des boutons
        foreach (var mission in chapter.missions)
        {
            GameObject btnObj = Instantiate(missionButtonPrefab, missionContainer);
            
            // Texte du bouton (ex: "1-1 : L'Attaque")
            btnObj.GetComponentInChildren<TextMeshProUGUI>().text = mission.missionName;

            // Clic sur le bouton
            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => LaunchMissionSetup(mission.sceneName));
        }
    }

    // Quand on clique sur une mission spécifique
    public void LaunchMissionSetup(string sceneName)
    {
        if (GameData.Instance != null)
        {
            GameData.Instance.sceneToLoad = sceneName;
        }
        
        // On part au déploiement
        SceneManager.LoadScene("Deployment");
    }

    public void QuitGame() { Application.Quit(); }
    public void SetVolume(float v) { AudioListener.volume = v; }
    public void SetQuality(int i) { QualitySettings.SetQualityLevel(i); }
}