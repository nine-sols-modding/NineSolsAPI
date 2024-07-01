using System;
using System.Collections.Generic;
using UnityEngine;
using Input = UnityEngine.Input;

namespace NineSolsAPI;

internal class KeyBind {
    public MonoBehaviour Owner;
    public KeyCode[] Keys;
    public Action Action;
}

public class KeybindManager {
    private List<KeyBind> keybindings = [];

    internal void Unload() {
        keybindings.Clear();
    }

    public static void Add(MonoBehaviour owner, Action action, params KeyCode[] keys) {
        NineSolsAPICore.Instance.KeybindManager.AddKeybind(owner, action, keys);
    }

    private void AddKeybind(MonoBehaviour owner, Action action, params KeyCode[] keys) {
        if (keys.Length == 0) throw new Exception("zero keys");
        keybindings.Add(new KeyBind() { Owner = owner, Action = action, Keys = keys });
    }

    internal void Update() {
        var someOutdated = false;
        foreach (var keybind in keybindings) {
            if (!keybind.Owner) {
                someOutdated = true;
                continue;
            }

            var pressed = true;

            var hasControl = false;
            var hasShift = false;
            var hasAlt = false;

            for (var i = 0; i < keybind.Keys.Length; i++) {
                var key = keybind.Keys[i];

                // TODO refactor this
                hasShift |= key == KeyCode.LeftShift;
                hasControl |= key == KeyCode.LeftControl;
                hasAlt |= key == KeyCode.LeftAlt;

                var last = i == keybind.Keys.Length - 1;

                pressed &= last ? Input.GetKeyDown(key) : Input.GetKey(key);
            }

            if (!hasControl && Input.GetKey(KeyCode.LeftControl)) pressed = false;
            if (!hasShift && Input.GetKey(KeyCode.LeftShift)) pressed = false;
            if (!hasAlt && Input.GetKey(KeyCode.LeftAlt)) pressed = false;

            if (!pressed) continue;

            try {
                keybind.Action.Invoke();
            } catch (Exception e) {
                Log.Error($"Failed to run action: {e}");
            }
        }

        if (someOutdated) {
            var removed = keybindings.RemoveAll(keybinding => !keybinding.Owner);
        }
    }
}