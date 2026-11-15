using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using JetBrains.Annotations;
using NineSolsAPI.Utils;
using UnityEngine.SceneManagement;

namespace NineSolsAPI;

[PublicAPI]
public class CustomFlagManager
{
    // STATIC

    private static SaveManager SaveManagerInstance => SaveManager.Instance;


    private static readonly string String_InsertCustomFlags_Begin = "Inserting flags...";
    private static readonly string String_InsertCustomFlags_End = "{0} Inserted to flag dictionary.";
    private static readonly string String_CreateNewFlagBase_NewFlagInGameError = "New flags cannot be created during gameplay.";
    private static readonly string String_CreateNewFlagBase_FlagCreationError = "Flag creation failed: {0}";

    internal static SaveSlotModMetaData[] saveModMetaDataList = new SaveSlotModMetaData[5];

    internal static Dictionary<string, CustomFlagManager> allClassInstances = new();

    private static int insertedFlagCount = 0;

    private static bool inGame = false;

    internal static void Update_inGame(Scene scene)
    {
        inGame = (scene.name != "TitleScreenMenu" && scene.name != "Logo");
    }

    private static void RemoveCustomFlags()
    {
        if (insertedFlagCount == 0)
        {
            return;
        }
        Log.Debug("Removing...");

        int size = SaveManagerInstance.allFlags.Flags.Count - 1;
        var flagDict = SaveManagerInstance.allFlags.flagDict;
        var Flags = SaveManagerInstance.allFlags.Flags;
        for (int i = 0; i < insertedFlagCount; i++)
        {
            flagDict.Remove(flagDict.ElementAt(size - i).Key);
            Flags.RemoveAt(size - i);
        }
        insertedFlagCount = 0;
        Log.Debug("Removed.");
    }

    internal static void InsertCustomFlags()
    {
        if (insertedFlagCount > 0)
        {
            RemoveCustomFlags();
        }

        Log.Debug(String_InsertCustomFlags_Begin);

        for (int i = allClassInstances.Count - 1; i >= 0; i--)
        {
            var instance = allClassInstances.ElementAt(i).Value;

            if ((!instance.enabled || instance.flagCount == 0) && (i != 0))
            {
                instance._enabled = false;
                continue;
            }
            foreach (var flag in instance.flags.Values)
            {
                SaveManagerInstance.allFlags.FlagDict.Add(flag.FinalSaveID, flag);
                SaveManagerInstance.allFlags.Flags.Add(flag);
            }
            insertedFlagCount += instance.flagCount;
        }

        Log.Debug(string.Format(String_InsertCustomFlags_End, insertedFlagCount));
    }

    private static GameFlagBase CreateNewFlagBase(BaseUnityPlugin owner, string name, GameFlagBase flag)
    {
        // As far as I know, there isn't a limit to how long a name/id can be.
        string GUID = owner.Info.Metadata.GUID;
        flag.name = name;
        flag.SetSaveID($"{GUID}.{name}.");
        var instanceFlags = allClassInstances[GUID]._flags;
        if (instanceFlags.ContainsKey(flag.FinalSaveID))
        {
            // Mainly for plugins that are loaded with ScriptEngine. Prevents errors when reloading ingame.
            return instanceFlags[flag.FinalSaveID];
        }

        if (inGame)
        {
            Log.Error(String_CreateNewFlagBase_NewFlagInGameError);
            return flag;
        }

        try
        {
            instanceFlags.Add(flag.FinalSaveID, flag);
        }
        catch (ArgumentException e)
        {
            Log.Error(string.Format(String_CreateNewFlagBase_FlagCreationError, e));
        }

        return flag;
    }

    internal static string InsertModListBeforeBinarySerialization(string? meta)
    {
        if (meta == null || allClassInstances.Count <= 1)
        {
            return meta!;
        }
        string output = meta;
        SaveSlotModMetaData modMetaData = new();
        modMetaData.modList = CustomFlagManager.allClassInstances.Values.Select(cfm => cfm.modData).ToList();
        string modMetaDataString = JsonUtils.Serialize(modMetaData);
        modMetaDataString = modMetaDataString.Remove(0, 1);
        output = output.Remove(output.Length - 1, 1);
        output += ",";
        output += modMetaDataString;
        Log.Debug(output);
        return output;
    }

    internal static void ReadModListBeforeJSONDeserialization(string? meta, string? path)
    {
        if (path == null || meta == null)
        {
            return;
        }
        int slotNumber = (int)path[8] - 48;
        if (slotNumber >= 0 && slotNumber <= 4)
        {
            saveModMetaDataList[slotNumber] = JsonUtils.Deserialize<SaveSlotModMetaData>(meta)!;
        }
    }


    // NON-STATIC

    public bool enabled => _enabled;
    public bool _enabled;

    public Dictionary<string, GameFlagBase> flags => _flags;
    internal Dictionary<string, GameFlagBase> _flags = null!;

    private BaseUnityPlugin owner = null!;
    internal ModData modData = null!;

    internal string url => modData.url;
    internal string name => modData.name;
    internal string guid => modData.guid;
    internal string version => modData.version;
    internal int flagCount => flags.Count;

    public CustomFlagManager(BaseUnityPlugin owner, string url = "")
    {
        this._enabled = true;
        this._flags = new();
        this.modData = new(owner, url);
        this.owner = owner;

        if (CustomFlagManager.allClassInstances.ContainsKey(modData.guid))
        {
            CustomFlagManager.allClassInstances.Remove(modData.guid);
        }

        CustomFlagManager.allClassInstances.Add(modData.guid, this);
    }

    public ScriptableDataBool CreateScriptableDataBool(string name, bool defaultValue = false)
    {
        ScriptableDataBool flag = new ScriptableDataBool();
        flag.field = new FlagFieldBool();
        flag.CurrentValue = defaultValue;
        return (ScriptableDataBool)CreateNewFlagBase(owner, name, flag);
    }
    public GameFlagInt CreateGameFlagInt(string name, int defaultValue = 0)
    {
        GameFlagInt flag = new GameFlagInt();
        flag.field = new FlagFieldInt();
        flag.CurrentValue = defaultValue;
        return (GameFlagInt)CreateNewFlagBase(owner, name, flag);
    }
    public ScriptableDataFloat CreateScriptableDataFloat(string name, float defaultValue = 0f)
    {
        ScriptableDataFloat flag = new ScriptableDataFloat();
        flag.field = new FlagFieldFloat();
        flag.CurrentValue = defaultValue;
        return (ScriptableDataFloat)CreateNewFlagBase(owner, name, flag);
    }
    public GameFlagString CreateGameFlagString(string name, string defaultValue = "")
    {
        GameFlagString flag = new GameFlagString();
        flag.field = new FlagFieldString();
        flag.CurrentValue = defaultValue;
        return (GameFlagString)CreateNewFlagBase(owner, name, flag);
    }
}

[Serializable]
internal class ModData
{
    public ModData()
    {
        url = "";
        name = "";
        guid = "";
        version = "";
        flagCount = 0;
    }
    public ModData(BaseUnityPlugin mod, string url = "")
    {
        BepInPlugin meta = mod.Info.Metadata;
        this.name = meta.Name;
        this.guid = meta.GUID;
        this.version = meta.Version.ToString();
        this.url = url;
        this.flagCount = 0;
    }

    public string url;

    public string name;

    public string guid;

    public string version;

    public int flagCount;
}

[Serializable]
internal class SaveSlotModMetaData
{
    public SaveSlotModMetaData()
    {
        modList = new List<ModData>();
    }
    public List<ModData> modList;
}
