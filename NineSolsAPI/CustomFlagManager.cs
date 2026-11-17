using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using JetBrains.Annotations;
using NineSolsAPI.Utils;
using UnityEngine.SceneManagement;
using HarmonyLib;

namespace NineSolsAPI;

[PublicAPI]
public class CustomFlagManager
{
    // STATIC
    
    private static SaveManager SaveManagerInstance => SaveManager.Instance;

    private static readonly string String_InsertCustomFlags_Begin = "Inserting flags...";
    private static readonly string String_InsertCustomFlags_End = "{0} Inserted to flag dictionary.";
    private static readonly string String_CreateNewFlagBase_NewFlagInGameWarning = "New flags cannot be created during gameplay.";
    private static readonly string String_CreateNewFlagBase_FlagCreationError = "Flag creation failed: {0}";
    private static readonly string String_enabled_ToggleDuringGameplayWarning = "CustomFlagManager instance {0} cannot be toggled during gameplay.";
    private static readonly string String_CheckModDesyncsBeforeLoadSave_NotSyncedConfirm = "This save's mod data is not synced with what you currently have. Load anyway?\n({0} missing)\n({1} different version)\n({2} new)";

    private static readonly int saveSlotCount = (int)Traverse.Create(SaveManagerInstance).Field("saveSlotCount").GetValue();

    private static int insertedFlagCount = 0;
    private static bool inGame = false;
    internal static bool startGameConfirmed = false;

    internal static Dictionary<string, ModData>[] saveModMetaDataList = new Dictionary<string, ModData>[saveSlotCount];

    internal static Dictionary<string, CustomFlagManager> allClassInstances = new();

    internal static HashSet<string>[,] slotDesyncTracker = new HashSet<string>[saveSlotCount, 3];
    
    internal enum Desyncs
    {
        Missing,
        DifferentVersion,
        New
    }
    
    static CustomFlagManager()
    {
        for (int i = 0; i < saveSlotCount; i++)
            for (int j = 0; j < slotDesyncTracker.GetLength(1); j++)
                slotDesyncTracker[i,j] = new();
    }

    private static void CheckForDesyncs(int slotIndex)
    {
        slotDesyncTracker[slotIndex, (int)Desyncs.Missing].Clear();
        slotDesyncTracker[slotIndex, (int)Desyncs.DifferentVersion].Clear();
        foreach (ModData modData in saveModMetaDataList[slotIndex].Values)
        {
            // Check if mod is missing
            if (!allClassInstances.ContainsKey(modData.guid))
                slotDesyncTracker[slotIndex, (int)Desyncs.Missing].Add(modData.guid);

            // Check if mod is different version
            else if (allClassInstances[modData.guid].version != modData.version)
                slotDesyncTracker[slotIndex, (int)Desyncs.DifferentVersion].Add(modData.guid);
        }
        // Check for new mods
        slotDesyncTracker[slotIndex, (int)Desyncs.New].Clear();
        foreach (CustomFlagManager instance in allClassInstances.Values)
        {
            if (!saveModMetaDataList[slotIndex].ContainsKey(instance.guid) && instance.enabled && instance.flagCount > 0)
                slotDesyncTracker[slotIndex, (int)Desyncs.New].Add(instance.guid);
        }
    }

    private static void CheckForNewDesyncOnSpecificInstance(int slotIndex, CustomFlagManager instance)
    {
        if (saveModMetaDataList[slotIndex] == null)
            return;
        if (!saveModMetaDataList[slotIndex].ContainsKey(instance.guid) && instance.enabled && instance.flagCount > 0)
            slotDesyncTracker[slotIndex, (int)Desyncs.New].Add(instance.guid);
        else
            slotDesyncTracker[slotIndex, (int)Desyncs.New].Remove(instance.guid);
    }

    internal static bool CheckModDesyncsBeforeLoadSave(SaveSlotUIButton slotButtonInstance)
    {
        if (startGameConfirmed)
        {
            return true;
        }
        int slotIndex = slotButtonInstance.index;

        int Missing = slotDesyncTracker[slotIndex, (int)Desyncs.Missing].Count;
        int DifferentVersion = slotDesyncTracker[slotIndex, (int)Desyncs.DifferentVersion].Count;
        int New = slotDesyncTracker[slotIndex, (int)Desyncs.New].Count;

        if (Missing > 0 || DifferentVersion > 0 || New > 0)
        {
            startGameConfirmed = true;
            SingletonBehaviour<StartMenuLogic>.Instance.confirmPanel.ShowConfirm(string.Format(String_CheckModDesyncsBeforeLoadSave_NotSyncedConfirm, Missing, DifferentVersion, New), () => slotButtonInstance.Submit());
            return false;
        }
        return true;
    }

    internal static void CheckIfInGame(Scene scene)
    {
        inGame = (scene.name != "TitleScreenMenu" && scene.name != "Logo");
    }

    private static void RemoveCustomFlags()
    {
        if (insertedFlagCount == 0)
        {
            return;
        }

        int size = SaveManagerInstance.allFlags.Flags.Count - 1;
        var flagDict = SaveManagerInstance.allFlags.flagDict;
        var Flags = SaveManagerInstance.allFlags.Flags;
        for (int i = 0; i < insertedFlagCount; i++)
        {
            flagDict.Remove(flagDict.ElementAt(size - i).Key);
            Flags.RemoveAt(size - i);
        }
        insertedFlagCount = 0;
    }

    internal static void InsertCustomFlags(int slotIndex)
    {
        inGame = true;
        if (insertedFlagCount > 0)
        {
            RemoveCustomFlags();
        }

        foreach (var modData in saveModMetaDataList[slotIndex].Values)
        {
            if (allClassInstances.TryGetValue(modData.guid, out var instance))
            {
                instance._enabled = true;
            }
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
        if (meta == null) return meta!;
        
        SaveSlotModMetaData modMetaData = new();

        foreach (var instance in allClassInstances.Values)
        {
            if (instance.enabled)
            {
                modMetaData.modList.Add(instance.modData);
            }
        }

        if (modMetaData.modList.Count <= 1)
        {
            Log.Debug(meta);
            return meta;
        }
        
        string modMetaDataString = JsonUtils.Serialize(modMetaData);

        modMetaDataString = modMetaDataString.Remove(0, 1);
        meta = meta.Remove(meta.Length - 1, 1);
        meta += ",";
        meta += modMetaDataString;

        Log.Debug(meta);
        return meta;
    }

    internal static void ReadModListBeforeJSONDeserialization(string? meta, string? path)
    {
        if (path == null || meta == null)
        {
            return;
        }
        int slotIndex = (int)path[8] - 48;
        if (slotIndex >= 0 && slotIndex <= 4)
        {
            saveModMetaDataList[slotIndex] = JsonUtils.Deserialize<SaveSlotModMetaData>(meta)!.modList.ToDictionary(Moddata => Moddata.guid);
            CheckForDesyncs(slotIndex);
        }
    }


    // NON-STATIC

    public bool enabled
    {
        get
        {
            return _enabled;
        }
        set
        {
            if (!inGame)
            {
                _enabled = value;
                for (int i = 0; i < saveSlotCount; i++)
                    CheckForNewDesyncOnSpecificInstance(i, this);
            }
            else
                Log.Warning(string.Format(String_enabled_ToggleDuringGameplayWarning, guid));
        }
    }

    private bool _enabled;

    public Dictionary<string, GameFlagBase> flags => _flags;
    internal Dictionary<string, GameFlagBase> _flags = null!;

    private BaseUnityPlugin owner = null!;
    internal ModData modData = null!;

    internal string url => modData.url;
    internal string name => modData.name;
    internal string guid => modData.guid;
    internal string version => modData.version;
    internal int flagCount => flags.Count;

    public CustomFlagManager(BaseUnityPlugin owner, bool enabled = true, string url = "")
    {
        this._enabled = enabled;
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
        modList = new();
    }
    public List<ModData> modList;
}
