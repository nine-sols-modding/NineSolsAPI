using System;
using System.Collections.Generic;
using UnityEngine;
using Input = UnityEngine.Input;

namespace NineSolsAPI;

internal class KeyBind {
    public KeyCode[] Keys;
    public Action Action;
}

public class KeybindManager {
    private List<KeyBind> keybindings = [];

    internal void Unload() {
        keybindings.Clear();
    }

    public static void Add(Action action, params KeyCode[] keys) {
        NineSolsAPICore.Instance.KeybindManager.AddKeybind(action, keys);
    }

    private void AddKeybind(Action action, params KeyCode[] keys) {
        if (keys.Length == 0) throw new Exception("zero keys");
        keybindings.Add(new KeyBind() { Action = action, Keys = keys });
    }

    internal void Update() {
        foreach (var keybind in keybindings) {
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

            if (pressed) keybind.Action.Invoke();
        }
    }
}