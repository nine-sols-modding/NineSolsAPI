using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using JetBrains.Annotations;
using UnityEngine;
using Input = UnityEngine.Input;

namespace NineSolsAPI;

internal class KeyBind {
    public MonoBehaviour Owner;
    public KeyboardShortcut Shortcut;
    public Action Action;
}

public class KeybindManager {
    private List<KeyBind> keybindings = [];

    internal void Unload() {
        keybindings.Clear();
    }

    public static void Add(MonoBehaviour owner, Action action, KeyCode mainKey, KeyCode[] modifiers = null) {
        NineSolsAPICore.Instance.KeybindManager.AddKeybind(owner, action, new KeyboardShortcut(mainKey, modifiers ?? []));
    }
    public static void Add(MonoBehaviour owner, Action action, KeyboardShortcut shortcut) {
        NineSolsAPICore.Instance.KeybindManager.AddKeybind(owner, action, shortcut);
    }

    private void AddKeybind(MonoBehaviour owner, Action action, KeyboardShortcut shortcut) {
        keybindings.Add(new KeyBind() { Owner = owner, Action = action, Shortcut = shortcut });
    }

    internal void Update() {
        var someOutdated = false;
        foreach (var keybind in keybindings) {
            if (!keybind.Owner) {
                someOutdated = true;
                continue;
            }

            if (!keybind.Shortcut.IsPressed()) continue;

            try {
                keybind.Action.Invoke();
            } catch (Exception e) {
                Log.Error($"Failed to run action: {e}");
            }
        }

        if (someOutdated) {
            keybindings.RemoveAll(keybinding => !keybinding.Owner);
        }
    }
}