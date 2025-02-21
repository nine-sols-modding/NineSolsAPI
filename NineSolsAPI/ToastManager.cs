using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;

namespace NineSolsAPI;

internal record struct ToastMessage(float StartTime, string Text);

public class ToastManager {
    private const float MaxToastAge = 5;

    private bool toastsDirty = false;
    private List<ToastMessage> toasts = [];

    private TMP_Text toastText;

    public ToastManager() {
        var toastTextObj = new GameObject("Toast");
        toastTextObj.transform.SetParent(NineSolsAPICore.FullscreenCanvas?.transform);
        toastText = toastTextObj.AddComponent<TextMeshProUGUI>();
        toastText.alignment = TextAlignmentOptions.BottomRight;
        toastText.fontSize = 20;
        toastText.color = Color.white;
        RCGLifeCycle.DontDestroyForever(toastTextObj);

        var toastTextTransform = toastText.GetComponent<RectTransform>();
        toastTextTransform.anchorMin = new Vector2(1, 0);
        toastTextTransform.anchorMax = new Vector2(1, 0);
        toastTextTransform.pivot = new Vector2(1f, 0f);
        toastTextTransform.anchoredPosition = new Vector2(-10, 10);
        toastTextTransform.sizeDelta = new Vector2(Screen.width, 0f);
    }

    private static void ToastInner(string message) {
        Log.Info($"Toast: {message}");
        NineSolsAPICore.Instance.ToastManager.AddToastMessage(message?.ToString() ?? "null");
    }

    private static string SimpleTypeName(Type type) {
        if (type.IsGenericType) {
            var genericArguments = type.GetGenericArguments()
                .Select(x => x.Name)
                .Aggregate((x1, x2) => $"{x1}, {x2}");
            return $"{type.Name[..type.Name.IndexOf("`", StringComparison.Ordinal)]}"
                   + $"<{genericArguments}>";
        }

        return type.Name;
    }

    [PublicAPI]
    public static void Toast(object? message) {
        if (message is IEnumerable list and not string) {
            ToastInner(SimpleTypeName(list.GetType()));
            var empty = true;
            foreach (var item in list) {
                empty = false;
                ToastInner(item + "  -");
            }

            if (empty) ToastInner("(empty)");
            return;
        }

        ToastInner(message?.ToString() ?? "null");
    }


    private float Now => Time.realtimeSinceStartup;

    private void AddToastMessage(string message) {
        toasts.Add(new ToastMessage(Now, message));
        toastsDirty = true;
    }

    internal void Update() {
        var now = Now;
        toastsDirty |= toasts.RemoveAll(toast => now - toast.StartTime > MaxToastAge) > 0;

        if (toastsDirty) toastText.text = string.Join('\n', toasts.Select(toast => toast.Text));
    }
}