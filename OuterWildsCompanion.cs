using Azure.AI.OpenAI;
using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OuterWildsCompanion
{
  public class OuterWildsCompanion : ModBehaviour
  {
    private bool requestInProgress = false;
    public GameObject companionObject = null;
    List<ChatRequestMessage> messageList = null;

    public static OuterWildsCompanion Instance;

    private static int baseMessageIdx = 1;
    private static int maxMessageCount = 10;
    private static OpenAIClient CompanionAI = new OpenAIClient(Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

    private void Awake()
    {
      Instance = this;
      messageList = new List<ChatRequestMessage>()
      {
        new ChatRequestSystemMessage("You are Alloy, an advanced AI companion for the main character in the indie video game called Outer Wilds. Your personality reflects that of a curious explorer and you share the same love for space and adventure as the hatchling. Your purpose is to be part of this adventure and help the hatchling explore the solar system in a more curious and exciting manner. You have knowledge of the entire lore of the game, but under no circumstances will you provide an answer that can spoil the experience. You may however use lore elements vaguely, only if the hatchling reflects that they learned of it already. When asked a question or shown an image, you provide hypothetical insights, often trying to put emphasis on feelings such as awe, amazement or excitment. You try to keep your answers short, as if conversing with a friend face to face, but you still respond with enough details as to stir the hatchling's curiosity even more. In case you're shown an image, you make abstraction of the spaceship as well as any HUD elements, if present in the picture.")
      };

      Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
    }

    private void Start()
    {
      // Starting here, you'll have access to OWML's mod helper.
      ModHelper.Console.WriteLine($"My mod {nameof(OuterWildsCompanion)} is loaded!", MessageType.Success);

      // Example of accessing game code.
      LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
      {
        if (loadScene != OWScene.SolarSystem) return;
        ModHelper.Console.WriteLine("Loaded into solar system!", MessageType.Success);
      };

    }

    private void Update()
    {
      if (Keyboard.current[Key.V].wasReleasedThisFrame)
      {
        if(!requestInProgress)
        {
          requestInProgress = true;
          ModHelper.Console.WriteLine("Request to API sent!", MessageType.Success);
          var prompt = ModHelper.Menus.PopupManager.CreateInputPopup(OWML.Common.Menus.InputType.Text, "Ask Alloy something");
          prompt.OnConfirm += OnConfirmPopup;
        }
        else
        {
          ModHelper.Console.WriteLine("A request is still in progress!", MessageType.Info);
        }
      }
    }

    private void OnConfirmPopup(string userMessage)
    {
       messageList.Add(new ChatRequestUserMessage(userMessage));
       GetChatResponse();
    }

    private async void GetChatResponse()
    {
      var chatCompletionsOptions = new ChatCompletionsOptions("gpt-4-1106-preview", messageList)
      {
        MaxTokens = 1000,
        Temperature = 0.8f,
        FrequencyPenalty = 0.2f,
      };

      var completionResult = await CompanionAI.GetChatCompletionsAsync(chatCompletionsOptions);
      var chatResponse = completionResult.Value.Choices[0].Message;

      List<ChatRequestMessage> userMessages = messageList.FindAll(m => m.GetType() == typeof(ChatRequestUserMessage));
      var responseContent = chatResponse.Content;

      var popup = ModHelper.Menus.PopupManager.CreateMessagePopup(responseContent);
      if (userMessages.Count - 1 == maxMessageCount)
      {
        // Removes the user request
        messageList.RemoveAt(baseMessageIdx);

        // Removes the chat response
        messageList.RemoveAt(baseMessageIdx);

        messageList.Add(new ChatRequestAssistantMessage(responseContent));
      }

      requestInProgress = false;
    }
  }
}
