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
        var toastTextObj = new GameObject();
        toastTextObj.transform.SetParent(NineSolsAPICore.FullscreenCanvas.transform);
        toastText = toastTextObj.AddComponent<TextMeshProUGUI>();
        toastText.alignment = TextAlignmentOptions.BottomRight;
        toastText.fontSize = 20;
        toastText.color = Color.white;

        var toastTextTransform = toastText.GetComponent<RectTransform>();
        toastTextTransform.anchorMin = new Vector2(1, 0);
        toastTextTransform.anchorMax = new Vector2(1, 0);
        toastTextTransform.pivot = new Vector2(1f, 0f);
        toastTextTransform.anchoredPosition = new Vector2(-10, 10);
        toastTextTransform.sizeDelta = new Vector2(800f, 0f);
    }

    [PublicAPI]
    public static void Toast(object message) {
        NineSolsAPICore.Instance.ToastManager.AddToastMessage(message.ToString());
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