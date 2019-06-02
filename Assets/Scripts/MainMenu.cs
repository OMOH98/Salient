using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;

public class MainMenu : MonoBehaviour
{
    //public string battleManagementScene;
    public string creditsScene;

    public List<Battle> availableMaps;
    private List<Button> battleButtons;

    [Header("Battle management UI")]
    

    public GameObject battleListPanel;
    public GameObject blScrollViewContent;
    public GameObject blButtonPrefab;
    public Button blCloseButton;

    public GameObject battlePropertiesPanel;
    public Text bpMapName;
    public Button bpStartBtn;
    public Button bpCloseBtn;
    public GameObject bpScrollViewContent;
    public GameObject bpSidePrefab;
    private const string bpsCountInputField = "Count";
    private const string bpsAiDropdown = "AI";
    private const string bpsCodeInputField = "Code";

    // Start is called before the first frame update
    void Start()
    {
        battleButtons = new List<Button>(availableMaps.Count);

        foreach (var item in availableMaps)
        {
            var btn = Instantiate(blButtonPrefab);
            var txt = btn.transform.GetChild(0).GetComponent<Text>();
            txt.text = item.mapName;
            var btncmp = btn.GetComponent<Button>();
            var inx = battleButtons.Count;
            btncmp.onClick.AddListener(() => {
                OpenBattleProperties(inx);
            });
            btn.transform.SetParent(blScrollViewContent.transform);
            battleButtons.Add(btncmp);
        }

        blCloseButton.onClick.AddListener(CloseBattleList);
        bpStartBtn.onClick.AddListener(StartBattleButtonClick);
        bpCloseBtn.onClick.AddListener(CloseBattlePropertiesWindow);

        battleListPanel.SetActive(false);
        battlePropertiesPanel.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private int currentBattleInx;
    private void OpenBattleProperties(int battleIndex)
    {
        battlePropertiesPanel.SetActive(true);
        currentBattleInx = battleIndex;
        //fill panel content
        var battle = availableMaps[battleIndex];
        bpMapName.text = string.Format("Map: \"{0}\":", battle.mapName);

        //BAD: it is not that smart to destroy all the gameobjects and than instantiate same. Not only is it a performance issue, it also kills all the data user may has entered.
        List<GameObject> finalSolutionCandidates = new List<GameObject>();
        for(int i=0; i<bpScrollViewContent.transform.childCount; i++)
        {
            finalSolutionCandidates.Add(bpScrollViewContent.transform.GetChild(i).gameObject);
        }
        foreach (var item in finalSolutionCandidates)
        {
            Destroy(item);
        }

        foreach (var item in battle.sides)
        {
            var side = Instantiate(bpSidePrefab);
            side.transform.SetParent(bpScrollViewContent.transform);
        }
    }

    public void StartBattleButtonClick()
    {
        var battle = availableMaps[currentBattleInx];
        for (int i = 0; i < battle.sides.Count && i < bpScrollViewContent.transform.childCount; i++)
        {
            var side = bpScrollViewContent.transform.GetChild(i);
            var count = side.transform.Find(bpsCountInputField).GetComponent<InputField>().text;
            var aiInx = side.transform.Find(bpsAiDropdown).GetComponent<Dropdown>().value;
            string code = side.transform.Find(bpsCodeInputField).GetComponent<InputField>().text;

            battle.sides[i].count = int.Parse(count);
            battle.sides[i].aiDifficulty = (Battle.Side.LAID)aiInx;
            battle.sides[i].code = code;
        }

        var prev = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var item in prev)
        {
            if (item.name == BattleInfo.infoGameObjectName)
                Destroy(item);
        }

        var preserver = new GameObject();
        preserver.name = BattleInfo.infoGameObjectName;

        

        var pb = preserver.AddComponent<BattleInfo>();
        pb.battle = battle;
        DontDestroyOnLoad(preserver);

        OpenScene(battle.loadSceneName);
    }
    public void CloseBattlePropertiesWindow()
    {
        battlePropertiesPanel.SetActive(false);
    }

    public void OpenBattleList()
    {
        battleListPanel.SetActive(true);
    }
    public void CloseBattleList()
    {
        battleListPanel.SetActive(false);
    }

    public void OpenAIManagement()
    {
        OpenScene(EditedTank.sceneSimulation);
    }
    public void OpenOptions()
    {
        //TODO: implement options
    }
    public void OpenCredits()
    {
        OpenScene(creditsScene);
    }

    private void OpenScene(string sceneName)
    {
        if(!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.Log($"coming soon!");
        }
    }
    
}

[System.Serializable]
public class Battle
{
    [System.Serializable]
    public class Side
    {
        public int count;
        public enum LAID
        {
            disabled, easy, medium, expert, userDefined
        }
        public LAID aiDifficulty;
        public string code;
        //public Color color;
    }
    public string loadSceneName;
    public string mapName;
    public List<Side> sides;
}

public class BattleInfo: MonoBehaviour
{
    public const string infoGameObjectName = "ChosenBattle";
    public Battle battle;
}