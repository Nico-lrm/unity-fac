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
    public GameObject missionListPanel;

    [Header("Mission List Components")]
    public TextMeshProUGUI chapterTitleText; // Pour afficher "Chapitre 1"
    public Transform missionContainer;       // L'endroit où on met les boutons
    public GameObject missionButtonPrefab;   // Le modèle de bouton

    [Header("Options Components")]
    public Slider volumeSlider;
    public TMP_Dropdown qualityDropdown;

	public TextMeshProUGUI previewDescriptionText;

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

    // --- LOGIQUE CHAPITRE & MISSION  ---

    public void OpenChapter(ChapterData chapter)
    {
        CloseAll();
        missionListPanel.SetActive(true);

        // On vide le texte de description au début (pour pas afficher la desc de la mission d'avant)
        if (previewDescriptionText != null) previewDescriptionText.text = "Sélectionnez une mission...";

        if (chapterTitleText != null) chapterTitleText.text = chapter.chapterName;

        // Nettoyage des vieux boutons
        foreach (Transform child in missionContainer) Destroy(child.gameObject);

        foreach (var mission in chapter.missions)
        {
            GameObject btnObj = Instantiate(missionButtonPrefab, missionContainer);

            MissionButtonUI uiScript = btnObj.GetComponent<MissionButtonUI>();
            if (uiScript != null)
            {
                uiScript.SetInfo(mission.missionID, mission.missionName);
            }
            else
            {
                TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = mission.missionName; 
            }

            // 2. Configuration du Clic
            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => LaunchMissionSetup(mission.sceneName));

            // 3. Configuration du HOVER (Hologramme + Description)
            MissionButtonHover hoverScript = btnObj.GetComponent<MissionButtonHover>();
            if (hoverScript != null)
            {
                hoverScript.Setup(mission.mapConfig, mission.description, previewDescriptionText);
            }
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