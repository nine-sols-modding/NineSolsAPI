using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using JetBrains.Annotations;
using UnityEngine;

namespace NineSolsAPI;

internal class KeyBind {
    public required MonoBehaviour Owner;
    public required Func<KeyboardShortcut> Shortcut;
    public required Action Action;
}

[PublicAPI]
public class KeybindManager {
    private List<KeyBind> keybindings = [];

    internal void Unload() {
        keybindings.Clear();
    }

    public static void Add(MonoBehaviour owner, Action action, params KeyCode[] keys) {
        var mainKey = keys[^1];
        var modifiers = keys[..^1];

        var shortcut = new KeyboardShortcut(mainKey, modifiers);
        Add(owner, action, shortcut);
    }

    public static void Add(MonoBehaviour owner, Action action, KeyboardShortcut shortcut) {
        Add(owner, action, () => shortcut);
    }

    public static void Add(MonoBehaviour owner, Action action, Func<KeyboardShortcut> shortcut) {
        NineSolsAPICore.Instance.KeybindManager.AddKeybind(owner, action, shortcut);
    }

    public static void Add(MonoBehaviour owner, Action action, ConfigEntry<KeyboardShortcut> shortcut) {
        Add(owner, action, () => shortcut.Value);
    }

    private void AddKeybind(MonoBehaviour owner, Action action, Func<KeyboardShortcut> shortcut) {
        keybindings.Add(new KeyBind { Owner = owner, Action = action, Shortcut = shortcut });
    }

    internal void Update() {
        var someOutdated = false;
        foreach (var keybind in keybindings) {
            if (!keybind.Owner) {
                someOutdated = true;
                continue;
            }

            if (!CheckShortcutOnly(keybind.Shortcut.Invoke())) continue;

            try {
                keybind.Action.Invoke();
            } catch (Exception e) {
                Log.Error($"Failed to run action: {e}");
            }
        }

        if (someOutdated) keybindings.RemoveAll(keybinding => !keybinding.Owner);
    }

    /**
     * When you hold A + S + F1 and check KeyboardShortcut(F1).IsPressed, it will return false.
     * With this method, other keys like A and S are not checked, so it would be true.
     */
    public static bool CheckShortcutOnly(KeyboardShortcut shortcut) {
        var isDown = Input.GetKeyDown(shortcut.MainKey);
        foreach (var modifier in shortcut.Modifiers) isDown = isDown && Input.GetKey(modifier);
        return isDown;
    }
}