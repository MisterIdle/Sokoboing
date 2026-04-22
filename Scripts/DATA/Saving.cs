using Godot;
using Godot.Collections;
using System;
using System.Text;

namespace Com.IsartDigital.Sokoban;

public partial class Saving : Node
{
    private static Saving instance;

    public string playerId;
    public string playerName;
    private string playerPassword;

    [ExportGroup("Save Settings")]
    [Export] private int numberOfLevels = 30;
    [Export] private string saveLocation = "user://saveFile.json";

    private Dictionary levelScore = new Dictionary();

    public override void _Ready()
    {
        if (instance == null)
            instance = this;
        else
        {
            QueueFree();
            return;
        }
    }

    public static Saving GetInstance() => instance;

    private void InitLevelData()
    {
        levelScore.Clear();
        for (int i = 0; i < numberOfLevels; i++)
        {
            levelScore[i.ToString()] = 0;
        }
    }

    public int GetLevelScore(int pLevelNumber)
    {
        string lLevel = pLevelNumber.ToString();
        if (levelScore != null && levelScore.ContainsKey(lLevel))
        {
            return levelScore[lLevel].AsInt32();
        }
        return 0;
    }

    public void SaveLevelScore(int pLevelNumber, int pLevelScore)
    {
        Dictionary lDico = Parsing();

        if (!lDico.ContainsKey(playerName)) return;

        Dictionary lDicoPlayer = (Dictionary)lDico[playerName];

        if (!lDicoPlayer.ContainsKey("levelScore"))
        {
            lDicoPlayer["levelScore"] = new Dictionary();
        }

        Dictionary lAllLevel = (Dictionary)lDicoPlayer["levelScore"];
        string lLevel = pLevelNumber.ToString();

        if (!lAllLevel.ContainsKey(lLevel))
        {
            lAllLevel[lLevel] = pLevelScore;
        }
        else
        {
            int currentScore = lAllLevel[lLevel].AsInt32();
            if (currentScore < pLevelScore)
            {
                lAllLevel[lLevel] = pLevelScore;
            }
        }

        levelScore = lAllLevel;

        lDicoPlayer["levelScore"] = lAllLevel;
        lDico[playerName] = lDicoPlayer;

        using var lFile = FileAccess.Open(saveLocation, FileAccess.ModeFlags.Write);
        lFile.StoreString(Json.Stringify(lDico));
    }

    public int CalculateScore(string pName)
    {
        Dictionary lDico = Parsing();

        if (!lDico.ContainsKey(pName)) return 0;

        Dictionary lDicoPlayer = (Dictionary)lDico[pName];
        if (!lDicoPlayer.ContainsKey("levelScore")) return 0;

        Dictionary lAllLevel = (Dictionary)lDicoPlayer["levelScore"];
        int lFinalScore = 0;

        foreach (Variant lScore in lAllLevel.Values)
        {
            lFinalScore += lScore.AsInt32();
        }
        return lFinalScore;
    }

    public void CheckNameInDataBase(string pName, string pPassword)
    {
        playerName = pName.ToUpper();
        playerPassword = pPassword;

        if (!FileAccess.FileExists(saveLocation))
        {
            InitSaveFile();
        }

        using var lFile = FileAccess.Open(saveLocation, FileAccess.ModeFlags.Read);
        string lContent = lFile.GetAsText();

        if (lContent != "{}")
        {
            Dictionary lDico = Parsing();
            if (lDico.ContainsKey(pName))
            {
                if ((string)((Dictionary)lDico[pName])["Password"] == pPassword)
                {
                    Load();
                    Login.GetInstance().Switch();
                    return;
                }
                else
                {
                    Login.GetInstance().ResetLogin();
                    return;
                }
            }
        }

        Save();
        Login.GetInstance().Switch();
    }

    private void InitSaveFile()
    {
        using var lFile = FileAccess.Open(saveLocation, FileAccess.ModeFlags.Write);
        lFile.StoreString("{}");
    }

    public Dictionary Parsing()
    {
        using var lFile = FileAccess.Open(saveLocation, FileAccess.ModeFlags.Read);
        string lContent = lFile.GetAsText();
        Json lJson = new Json();

        Error lError = lJson.Parse(lContent);
        return (Dictionary)lJson.Data;
    }

    public void Save()
    {
        using var lFile = FileAccess.Open(saveLocation, FileAccess.ModeFlags.ReadWrite);
        string lContent = lFile.GetAsText();
        Json json = new Json();

        Error error = json.Parse(lContent);
        Dictionary lDico = (Dictionary)json.Data;

        if (!lDico.ContainsKey(playerName))
        {
            Dictionary lNewSave = new Dictionary();
            lNewSave["Password"] = playerPassword;

            InitLevelData();
            lNewSave["levelScore"] = levelScore;

            lDico[playerName] = lNewSave;
        }

        lFile.StoreString(Json.Stringify(lDico));
    }

    public void Load()
    {
        if (!FileAccess.FileExists(saveLocation)) return;

        using var lFile = FileAccess.Open(saveLocation, FileAccess.ModeFlags.Read);
        string lContent = lFile.GetAsText();

        if (string.IsNullOrEmpty(lContent) || lContent == "{}") return;

        Json lJson = new Json();
        Error lError = lJson.Parse(lContent);
        if (lError != Error.Ok) return;

        Dictionary lDico = (Dictionary)lJson.Data;

        if (lDico.ContainsKey(playerName))
        {
            Dictionary lDicoPlayer = (Dictionary)lDico[playerName];
            if (lDicoPlayer.ContainsKey("levelScore"))
            {
                levelScore = (Dictionary)lDicoPlayer["levelScore"];
            }
        }
    }

    protected override void Dispose(bool pDisposing)
    {
        instance = null;
        base.Dispose(pDisposing);
    }
}
