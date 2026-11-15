using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using JetBrains.Annotations;
using NineSolsAPI.Utils;

namespace NineSolsAPI;

[PublicAPI]
public class CustomFlagManager
{
    // STATIC

    private static readonly string String_InsertCustomFlags_Begin = "Inserting flags...";
    private static readonly string String_InsertCustomFlags_End = "{0} Inserted to flag dictionary.";
    private static readonly string String_CreateNewFlagBase_NewFlagInGameWarning = "New flags cannot be created during gameplay.";
    private static readonly string String_CreateNewFlagBase_FlagCreationError = "Flag creation failed: {0}";
    
    internal static SaveSlotModMetaData[] saveModMetaDataList = new SaveSlotModMetaData[5];

    internal static Dictionary<string, CustomFlagManager> allClassInstances = new();

    private static bool flagsInserted = false;
    
    internal static void InsertCustomFlags()
    {
        if (flagsInserted)
        {
            return;
        }

        Log.Debug(String_InsertCustomFlags_Begin);

        for (int i = allClassInstances.Count - 1; i >= 0; i--)
        {
            var GUID = allClassInstances.ElementAt(i).Key;
            var instanceFlags = allClassInstances[GUID]._flags;
            
            if (instanceFlags.Count == 0 && i != 0)
            {
                allClassInstances.Remove(GUID);
                continue;
            }
            foreach (var flag in instanceFlags.Values)
            {
                SaveManager.Instance.allFlags.FlagDict.Add(flag.FinalSaveID, flag);
                SaveManager.Instance.allFlags.Flags.Add(flag);
            }
        }

        int count = 0;
        foreach (var instance in allClassInstances.Values)
        {
            count += instance.flags.Count;
        }

        flagsInserted = true;
        Log.Debug(string.Format(String_InsertCustomFlags_End, count));
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
        
        if (flagsInserted)
        {
            Log.Warning(String_CreateNewFlagBase_NewFlagInGameWarning);
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
