using System.Collections.Generic;
using System.Linq;
using BepInEx.Bootstrap;
using I2.Loc;
using NineSolsAPI.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NineSolsAPI.Menu;

internal class TitlescreenModifications {
    private UIControlGroup? group;
    private UIControlButton? button;
    internal TitleScreenModList modList = new();

    private const bool Enable = true;

    public void Load() {
        MaybeExtendMainMenu(SceneManager.GetActiveScene());
    }

    public void Unload() {
        modList.Unload();
        if (button != null) Object.Destroy(button.gameObject);
        if (group != null) Object.Destroy(group.gameObject);
    }


    public void MaybeExtendMainMenu(Scene scene) {
        if (!Enable) return;

        if (scene.name != "TitleScreenMenu") return;
        if (button) return;

        
        modList.Load();
        
        /*
        group = CreateUiControlGroup();
        button = CreateOptionsButton();
        button.clickToShowGroup = group;
        */
    }


    private UIControlButton CreateOptionsButton() {
        var optionsButton = GameObject.Find("MainMenuButton_Option");

        var modOptions = ObjectUtils.InstantiateAutoReference(optionsButton, optionsButton.transform.parent, false);
        Object.Destroy(modOptions.GetComponentInChildren<Localize>());
        modOptions.GetComponentInChildren<TMP_Text>().text = "Mod Options";
        modOptions.gameObject.transform.SetSiblingIndex(optionsButton.transform.GetSiblingIndex() + 1);
        var modOptionButton = modOptions.GetComponentInChildren<UIControlButton>();

        return modOptionButton;
    }

    private UIControlGroup CreateUiControlGroup() {
        var providers = StartMenuLogic.Instance.gameObject.GetComponentInChildren<UICursorProvider>();

        var obj = new GameObject("ModOptions Panel");
        obj.transform.SetParent(providers.transform, false);

        var rectTransform = obj.AddComponent<RectTransform>();
        var uiControlGroup = obj.AddComponent<UIControlGroup>();
        var rcgUiPanel = obj.GetComponent<RCGUIPanel>();
        obj.AddComponent<CanvasRenderer>();
        obj.AddComponent<SelectableNavigationProvider>();
        obj.AddComponent<Animator>();
        AutoAttributeManager.AutoReference(obj);

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;

        rcgUiPanel.OnShowInit = new UnityEvent();
        rcgUiPanel.OnHideInit = new UnityEvent();
        rcgUiPanel.OnShowComplete = new UnityEvent();
        rcgUiPanel.OnHideComplete = new UnityEvent();

        var layout = new GameObject();
        var layoutTransform = layout.AddComponent<RectTransform>();
        layoutTransform.anchorMin = new Vector2(0.5f, 0.5f);
        layoutTransform.anchorMax = new Vector2(0.5f, 0.5f);
        layoutTransform.sizeDelta = new Vector2(600, 800);
        layout.AddComponent<VerticalLayoutGroup>().spacing = 20;
        layout.transform.SetParent(uiControlGroup.transform, false);

        var title = new GameObject();
        title.AddComponent<TextMeshProUGUI>().text = "Mod options";
        title.AddComponent<LayoutElement>();
        title.transform.SetParent(layout.transform, false);

        var padding = new GameObject();
        padding.AddComponent<CanvasRenderer>();
        padding.AddComponent<RectTransform>();
        padding.AddComponent<LayoutElement>().minHeight = 0;
        padding.transform.SetParent(layout.transform, false);

        var buttonOrig = ObjectUtils.FindDisabledByName<Button>("Show HUD")!;

        foreach (var plugin in Chainloader.PluginInfos) {
            var c = ObjectUtils.InstantiateAutoReference(buttonOrig.gameObject, layout.transform);
            c.GetComponentInChildren<TMP_Text>().text = $"{plugin.Key} {plugin.Value.Metadata.Version}";
            var flag = new FlagFieldEntryInt();
            var intFlag = ScriptableObject.CreateInstance<GameFlagInt>();
            intFlag.field = new FlagFieldInt();
            flag.flagBase = intFlag;
            flag.fieldName = "field";
        }


        uiControlGroup.defaultSelectable = layout.GetComponentInChildren<Selectable>();

        return uiControlGroup;
    }
}

internal class TitleScreenModList
{
    private static Dictionary<string, ModData>[] saveModMetaDataList => CustomFlagManager.saveModMetaDataList;
    private static Dictionary<string, CustomFlagManager> modDict => CustomFlagManager.allClassInstances;

    private static readonly string modList_GameObject_Label_Text_String = "Mod List";
    private static readonly string URLConfirm_String = "Open the download page of {0}?\n({1})";
    private static readonly string notSyncedConfirm_String = "This save's mods are not synced with your mods. Load anyway?\n({0} missing)\n({1} different version)\n({2} new)";
    private static readonly string transpilerFailedWarningLabel_Txt_String = "An error occured where the save file mod list metadata can't be read/written. Check the console for more details. Proceed at your own risk!";

    private static readonly Color RED = new Vector4(1f, 0f, 0f, 0.25f);
    private static readonly Color ORANGE = new Vector4(1f, 0.5f, 0f, 0.25f);

    private enum Desyncs
    {
        Missing,
        DifferentVersion,
        New
    }
    private int[][] desyncTracker = new int[][]
    {
        new int[5],
        new int[5],
        new int[5]
    };

    private bool modListLoaded = false;
    private bool startGameConfirmed = false;
    internal bool transpilerFailed = false;

    private Dictionary<string, GameObject>[] modButtons = new Dictionary<string, GameObject>[5]
    {
        new Dictionary<string, GameObject>(),
        new Dictionary<string, GameObject>(),
        new Dictionary<string, GameObject>(),
        new Dictionary<string, GameObject>(),
        new Dictionary<string, GameObject>()
    };

    private GameObject modList_GameObject = null!;
    private GameObject buttonClone = null!;
    private GameObject modList_GameObject_saveIndexLabel = null!;
    private GameObject transpilerFailedWarningLabel = null!;

    internal bool CheckModDesyncsBeforeLoadSave(SaveSlotUIButton slotButtonInstance)
    {
        if (startGameConfirmed)
        {
            return true;
        }
        int slotNumber = (int)slotButtonInstance.name[8] - 48;

        int Missing = desyncTracker[(int) Desyncs.Missing][slotNumber];
        int DifferentVersion = desyncTracker[(int) Desyncs.DifferentVersion][slotNumber];
        int New = desyncTracker[(int) Desyncs.New][slotNumber];

        if (Missing > 0 || DifferentVersion > 0 || New > 0)
        {
            startGameConfirmed = true;
            SingletonBehaviour<StartMenuLogic>.Instance.confirmPanel.ShowConfirm(string.Format(notSyncedConfirm_String, Missing, DifferentVersion, New), () => slotButtonInstance.Submit());
            return false;
        }
        return true;
    }
    
    private void LoadModList()
    {
        var SavePanel_BackButton = GameObject.Find("MenuLogic/MainMenuLogic/Providers/StartGame SaveSlotPanel/SavePanel_BackButton");
        buttonClone = ObjectUtils.InstantiateAutoReference(SavePanel_BackButton, modList_GameObject.transform.Find("Viewport/Content"), false);
        buttonClone.transform.Find("Text (TMP)").GetComponent<Localize>().enabled = false;
        GameObject.Destroy(buttonClone.transform.Find("BackgroundImage/Frame").gameObject);

        var buttonCloneText = buttonClone.transform.Find("Text (TMP)").gameObject.GetComponent<TextMeshProUGUI>();
        buttonCloneText.autoSizeTextContainer = true;
        buttonCloneText.text = "";
        buttonCloneText.characterSpacing = -5;
        buttonCloneText.fontSizeMax = 32;
        buttonCloneText.fontSizeMin = 14;

        var buttonCloneText_RectTransform = buttonCloneText.GetComponent<RectTransform>();
        buttonCloneText_RectTransform.anchoredPosition = Vector2.zero;
        buttonCloneText_RectTransform.anchoredPosition += new Vector2(-200, 0);
        buttonCloneText_RectTransform.sizeDelta = new Vector2(400, 0);
        buttonCloneText_RectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        buttonCloneText_RectTransform.anchorMin = new Vector2(0.5f, 0.5f);

        buttonClone.transform.localPosition = new Vector3(220f, -50f, 0f);
        buttonClone.transform.Find("Outline").gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(300, -5);
        buttonClone.transform.Find("BackgroundImage").gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(295, -9);
        buttonClone.GetComponent<UIControlButton>().onSubmit = new UnityEvent();

        var buttonCloneColorOverlay = ObjectUtils.InstantiateAutoReference(buttonClone.transform.Find("BackgroundImage").gameObject, buttonClone.transform, false);
        buttonCloneColorOverlay.name = "overlay";
        buttonCloneColorOverlay.GetComponent<Image>().color = new Vector4(0, 0, 0, 0);

        buttonClone.SetActive(false);
        buttonClone.name = "buttonClone";

        for (int i = 0; i < 5; i++)
        {
            Dictionary<string, ModData> meta = saveModMetaDataList[i];

            for (int j = 0; j < meta?.Count; j++)
            {
                ModData data = meta.ElementAt(j).Value;

                GameObject newButton = ObjectUtils.InstantiateAutoReference(buttonClone, buttonClone.transform.parent, false);
                modButtons[i].Add(data.guid, newButton);
                newButton.name = data.guid;
                newButton.GetComponent<RectTransform>().anchoredPosition += new Vector2(0, (-50 * j));
                var newButtonText = newButton.transform.Find("Text (TMP)").gameObject.GetComponent<TextMeshProUGUI>();
                newButtonText.text = $"{data.name} - {data.version}";

                var newButtonColorOverlay = newButton.transform.Find("overlay").GetComponent<Image>();
                // Check if mod is missing
                if (!modDict.ContainsKey(data.guid))
                {
                    newButtonColorOverlay.color = RED;
                    desyncTracker[(int)Desyncs.Missing][i]++;
                }
                // Check if mod is different version
                else if (modDict[data.guid].version != data.version)
                {
                    newButtonColorOverlay.color = ORANGE;
                    desyncTracker[(int)Desyncs.DifferentVersion][i]++;
                }
                
                if (data.url != "")
                {
                    newButton.GetComponent<Button>().onClick.AddListener(() =>
                    SingletonBehaviour<StartMenuLogic>.Instance.confirmPanel.ShowConfirm(string.Format(URLConfirm_String, data.name, data.url), () =>
                    Application.OpenURL(data.url)));
                }
            }
            
            // Check for new mod(s)
            foreach (CustomFlagManager data in modDict.Values)
            {
                if (meta != null && !meta.ContainsKey(data.guid) && data.flagCount > 0)
                {
                    desyncTracker[(int)Desyncs.New][i]++;
                }
            }
        }
        modListLoaded = true;
    }

    private void DisplayModListForSelectedFile(UIControlButton button)
    {
        startGameConfirmed = false;
        if (!modListLoaded)
        {
            LoadModList();
        }

        char slotNumber_CHAR = button.name[8];
        modList_GameObject_saveIndexLabel.GetComponent<TextMeshProUGUI>().text = ((char)(slotNumber_CHAR + 1)).ToString();

        int slotNumber_INT = slotNumber_CHAR - 48;
        for (int i = 0; i < 5; i++)
        {
            foreach (var buttonoff in modButtons[i])
            {
                buttonoff.Value.SetActive(slotNumber_INT == i);
            }
        }
    }

    public void Unload()
    {
        modListLoaded = false;
        startGameConfirmed = false;
        GameObject.Destroy(modList_GameObject);
        GameObject.Find("MenuLogic/MainMenuLogic/Providers/StartGame SaveSlotPanel/BG").transform.GetChild(2).GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -12);
        GameObject.Find("MenuLogic/MainMenuLogic/Providers/StartGame SaveSlotPanel/BG/Frame/UselessUI").transform.localPosition = new Vector2(0, -215);
        foreach (var save in modButtons)
        {
            save.Clear();
        }
        for (int i = 0; i < 4; i++)
        {
            string slotpath = "/MenuLogic/MainMenuLogic/Providers/StartGame SaveSlotPanel/SlotGroup/SlotGroup H/";
            GameObject slot = GameObject.Find($"{slotpath}SaveSlotContainer_Slot{i}/SaveSlot{i}");
            slot.GetComponent<SaveSlotUIButton>().OnSelectedEvent.RemoveListener(DisplayModListForSelectedFile);
        }
        for (int i = 0; i < desyncTracker.Length; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                desyncTracker[i][j] = 0;
            }
        }
    }

    public void Load()
    {
        Unload();
        
        // Mod list window
        GameObject ScrollView = GameObject.Find("/MenuLogic").transform.Find("Scroll View").gameObject;

        modList_GameObject = ObjectUtils.InstantiateAutoReference(ScrollView, GameObject.Find("/MenuLogic/MainMenuLogic/Providers/StartGame SaveSlotPanel").transform, false);
        modList_GameObject.SetActive(true);
        modList_GameObject.name = "modList_GameObject";
        
        GameObject.Destroy(modList_GameObject.transform.Find("Viewport/Content/ButtonCandidate1").gameObject);
        GameObject.Destroy(modList_GameObject.transform.Find("Viewport/Content/ButtonCandidate2").gameObject);
        var modList_GameObject_LayoutElement = modList_GameObject.AddComponent<LayoutElement>();
        modList_GameObject_LayoutElement.ignoreLayout = true;
        modList_GameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(120, -137);
        modList_GameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(440, 610);
        modList_GameObject.GetComponent<Image>().color = new Vector4(0f, 0f, 0f, 0.15f);

        // Mod list label
        var modList_GameObject_Label = new GameObject("Label");
        modList_GameObject_Label.transform.SetParent(modList_GameObject.transform);

        var modList_GameObject_Label_Text = modList_GameObject_Label.AddComponent<TextMeshProUGUI>();
        modList_GameObject_Label_Text.text = modList_GameObject_Label_Text_String;
        modList_GameObject_Label_Text.font = GameObject.Find("/MenuLogic/MainMenuLogic/Providers/StartGame SaveSlotPanel/Title Text").GetComponent<TextMeshProUGUI>().font;
        modList_GameObject_Label_Text.alignment = TextAlignmentOptions.Center;
        modList_GameObject_Label_Text.fontSize = 32;
        
        var modList_GameObject_Label_RectTransform = modList_GameObject_Label.GetComponent<RectTransform>();
        modList_GameObject_Label_RectTransform.localScale = new Vector3(1, 1, 1);
        modList_GameObject_Label_RectTransform.anchoredPosition = new Vector2(0, 340);

        // Mod list save index label
        var index = GameObject.Find("MenuLogic/MainMenuLogic/Providers/StartGame SaveSlotPanel/SlotGroup/SlotGroup H/SaveSlotContainer_Slot0/SaveSlot0/AnimationRoot/IndexNumberViewer");
        modList_GameObject_saveIndexLabel = ObjectUtils.InstantiateAutoReference(index, modList_GameObject.transform, false);
        modList_GameObject_saveIndexLabel.GetComponent<RectTransform>().anchoredPosition = new Vector2(-422, -2);
        modList_GameObject_saveIndexLabel.GetComponent<TextMeshProUGUI>().text = "";

        // Adjusting positions of cosmetic UI elements to make it look a little nicer.
        GameObject.Find("MenuLogic/MainMenuLogic/Providers/StartGame SaveSlotPanel/BG").transform.GetChild(2).GetComponent<RectTransform>().anchoredPosition = new Vector2(-20, -12);
        GameObject.Find("MenuLogic/MainMenuLogic/Providers/StartGame SaveSlotPanel/BG/Frame/UselessUI").transform.localPosition = new Vector2(-20, -215);

        // Adding listeners to save slot buttons to update modlist when selected
        for (int i = 0; i < 4; i++)
        {
            string slotpath = "/MenuLogic/MainMenuLogic/Providers/StartGame SaveSlotPanel/SlotGroup/SlotGroup H/";
            GameObject slot = GameObject.Find($"{slotpath}SaveSlotContainer_Slot{i}/SaveSlot{i}");
            slot.GetComponent<SaveSlotUIButton>().OnSelectedEvent.AddListener(DisplayModListForSelectedFile);
        }

        // Warning label for incase the transpilers fail
        transpilerFailedWarningLabel = ObjectUtils.InstantiateAutoReference(modList_GameObject_Label, modList_GameObject_Label.transform.parent, false);
        transpilerFailedWarningLabel.name = "transpilerFailedWarningLabel";

        RectTransform transpilerFailedWarningLabel_Rect = transpilerFailedWarningLabel.GetComponent<RectTransform>();
        transpilerFailedWarningLabel_Rect.anchoredPosition = new Vector2(0, 0);
        transpilerFailedWarningLabel_Rect.sizeDelta = new Vector2(440, 200);

        TextMeshProUGUI transpilerFailedWarningLabel_Txt = transpilerFailedWarningLabel.GetComponent<TextMeshProUGUI>();
        transpilerFailedWarningLabel_Txt.enableWordWrapping = true;
        transpilerFailedWarningLabel_Txt.text = transpilerFailedWarningLabel_Txt_String;
        transpilerFailedWarningLabel_Txt.color = Color.red;

        transpilerFailedWarningLabel.SetActive(transpilerFailed);
    }
}
