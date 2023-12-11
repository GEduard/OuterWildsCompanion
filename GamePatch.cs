using HarmonyLib;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace OuterWildsCompanion
{
  [HarmonyPatch]
  public class GamePatch
  {
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ReticleController), nameof(ReticleController.Awake))]
    public static void ReticleController_Awake_Postfix(ReticleController __instance)
    {
      var mainCanvas = __instance._canvas;

      OuterWildsCompanion.Instance.companionObject = new GameObject("CompanionSprite");
      RectTransform rectTrans = OuterWildsCompanion.Instance.companionObject.AddComponent<RectTransform>();
      rectTrans.transform.SetParent(mainCanvas.transform);

      rectTrans.localScale = Vector3.one;
      rectTrans.anchorMin = new Vector2(1, 0);
      rectTrans.anchorMax = new Vector2(1, 0);
      rectTrans.sizeDelta = new Vector2(250, 250);

      Vector2 shiftDirection = new Vector2(0.5f - rectTrans.anchorMax.x, 0.5f - rectTrans.anchorMax.y);
      rectTrans.anchoredPosition = shiftDirection * rectTrans.rect.size;

      var fileData = File.ReadAllBytes("C:\\Users\\Eduard Gothard\\Pictures\\PhotoshopEdits\\Alloy.png");
      Texture2D companionTexture = new Texture2D(850, 850);
      companionTexture.LoadImage(fileData);

      Image companionImage = OuterWildsCompanion.Instance.companionObject.AddComponent<Image>();
      companionImage.sprite = Sprite.Create(companionTexture, new Rect(0, 0, companionTexture.width, companionTexture.height), new Vector2(0.5f, 0.5f));
      companionImage.transform.SetParent(mainCanvas.transform);
      OuterWildsCompanion.Instance.companionObject.SetActive(false);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(HUDCamera), nameof(HUDCamera.ActivateHUD))]
    public static void HUDCamera_ActivateHUD_Postfix(HUDCamera __instance)
    {
      OuterWildsCompanion.Instance.companionObject.SetActive(true);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(HUDCamera), nameof(HUDCamera.DeactivateHUD))]
    public static void HUDCamera_DeactivateHUD_Postfix(HUDCamera __instance)
    {
      OuterWildsCompanion.Instance.companionObject.SetActive(false);
    }
  }
}
