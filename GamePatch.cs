using System.IO;

using HarmonyLib;
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

      Texture2D companionTexture = new Texture2D(850, 850);
      var companionSprite = Path.Combine(Directory.GetCurrentDirectory(), "Alloy.png");
      var fileData = File.ReadAllBytes(companionSprite);
      companionTexture.LoadImage(fileData);

      Image companionImage = OuterWildsCompanion.Instance.companionObject.AddComponent<Image>();
      companionImage.sprite = Sprite.Create(companionTexture, new Rect(0, 0, companionTexture.width, companionTexture.height), new Vector2(0.5f, 0.5f));
      companionImage.transform.SetParent(mainCanvas.transform);
      OuterWildsCompanion.Instance.companionObject.SetActive(false);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(OWTime), nameof(OWTime.Pause))]
    public static void OWTime_Pause_Postfix()
    {
      OuterWildsCompanion.Instance.PauseCompanion();
      OuterWildsCompanion.Instance.gameIsPaused = true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(OWTime), nameof(OWTime.Unpause))]
    public static void OWTime_Unpause_Postfix()
    {
      OuterWildsCompanion.Instance.ResumeCompanion();
      OuterWildsCompanion.Instance.gameIsPaused = false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(HUDCamera), nameof(HUDCamera.ActivateHUD))]
    public static void HUDCamera_ActivateHUD_Postfix()
    {
      OuterWildsCompanion.Instance.companionIsAvailable = true;
      OuterWildsCompanion.Instance.companionObject.SetActive(true);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(HUDCamera), nameof(HUDCamera.DeactivateHUD))]
    public static void HUDCamera_DeactivateHUD_Prefix()
    {
      OuterWildsCompanion.Instance.companionIsAvailable = false;
      OuterWildsCompanion.Instance.companionObject.SetActive(false);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShipCockpitUI), nameof(ShipCockpitUI.OnEnterFlightConsole))]
    public static void ShipCockpitUI_OnEnterFlightConsole_Postfix()
    {
      OuterWildsCompanion.Instance.companionIsAvailable = true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipCockpitUI), nameof(ShipCockpitUI.OnExitFlightConsole))]
    public static void ShipCockpitUI_OnExitFlightConsole_Prefix()
    {
      OuterWildsCompanion.Instance.companionIsAvailable = false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(DeathManager), nameof(DeathManager.KillPlayer))]
    public static void DeathManager_KillPlayer_Prefix()
    {
      OuterWildsCompanion.Instance.ResetCompanion();
    }
  }
}
