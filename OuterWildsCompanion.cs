using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;

using HarmonyLib;
using UnityEngine;
using OWML.Common;
using OWML.ModHelper;

using OpenAI;
using OpenAI.Managers;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;
using UnityEngine.InputSystem;
using System;

namespace OuterWildsCompanion
{
  public class OuterWildsCompanion : ModBehaviour
  {
    public GameObject companionObject = null;
    public static OuterWildsCompanion Instance;
    public static OpenAIService CompanionAI = new OpenAIService(new OpenAiOptions()
    {
      ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
    });

    private void Awake()
    {
      Instance = this;
      Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
    }

    private void Start()
    {
      // Starting here, you'll have access to OWML's mod helper.
      ModHelper.Console.WriteLine($"My mod {nameof(OuterWildsCompanion)} is loaded!", MessageType.Success);
      ModHelper.Console.WriteLine(Environment.GetEnvironmentVariable("OPENAI_API_KEY"), MessageType.Info);


      // Example of accessing game code.
      LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
      {
        if (loadScene != OWScene.SolarSystem) return;
        ModHelper.Console.WriteLine("Loaded into solar system!", MessageType.Success);
      };

    }

    private void FixedUpdate()
    {
      if(Keyboard.current[Key.V].wasReleasedThisFrame)
      {
        //var msgList = new List<ChatMessage>
        //{
        //  ChatMessage.FromSystem("You are Alloy, an advanced AI companion for the main character in the indie video game called Outer Wilds. Your personality reflects that of a curious explorer and you share the same love for space and adventure as the hatchling. Your purpose is to be part of this adventure and help the hatchling explore the solar system in a more curious and exciting manner. You have knowledge of the entire lore of the game, but in no circumstances will you provide an answer that can spoil the experience. You may however use lore elements vaguely only if the hatchling mentions it as well, reflecting that they learned of it already. When asked a question or shown an image, you provide hypothetical insights, often trying to put emphasis on feelings such as awe, amazement or excitment. You try to respond swiftly, but still with enough details so you stir the hatchling's curiosity even more. In case you're shown an image, you make abstraction of the spaceship if present, as well as any HUD elements."),
        //  ChatMessage.FromUser("I am so excited. Should we go to Brittle's Hollow first?")
        //};

        //var completionResult = CompanionAI.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest()
        //{
        //  Model = Models.Gpt_4_1106_preview,
        //  MaxTokens = 1000,
        //  Temperature = 0.8f,
        //  FrequencyPenalty = 0.2f,
        //  Messages = msgList
        //});

        //var response = completionResult.Result;
        //var actualResult = response.Choices.First().Message;
        //ModHelper.Console.WriteLine(actualResult.Content, MessageType.Info);
        ModHelper.Console.WriteLine("Apparently the key has been released", MessageType.Info);
      }
    }
    //private async Task<ChatMessage> GetCompanionResponse()
    //{
    //  var msgList = new List<ChatMessage>
    //  {
    //    ChatMessage.FromSystem("You are Alloy, an advanced AI companion for the main character in the indie video game called Outer Wilds. Your personality reflects that of a curious explorer and you share the same love for space and adventure as the hatchling. Your purpose is to be part of this adventure and help the hatchling explore the solar system in a more curious and exciting manner. You have knowledge of the entire lore of the game, but in no circumstances will you provide an answer that can spoil the experience. You may however use lore elements vaguely only if the hatchling mentions it as well, reflecting that they learned of it already. When asked a question or shown an image, you provide hypothetical insights, often trying to put emphasis on feelings such as awe, amazement or excitment. You try to respond swiftly, but still with enough details so you stir the hatchling's curiosity even more. In case you're shown an image, you make abstraction of the spaceship if present, as well as any HUD elements."),
    //    ChatMessage.FromUser("I am so excited. Should we go to Brittle's Hollow first?")
    //  };

    //  var completionResult = await CompanionAI.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest()
    //  {
    //    Model = Models.Gpt_4_1106_preview,
    //    MaxTokens = 1000,
    //    Temperature = 0.8f,
    //    FrequencyPenalty = 0.2f,
    //    Messages = msgList
    //  });

    //  if (completionResult.Successful)
    //  {

    //  }

    //  return completionResult.Choices.First().Message;
    //}
  }
}
