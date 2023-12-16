using System;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Collections.Generic;

using HarmonyLib;
using UnityEngine;
using OWML.Common;
using OWML.ModHelper;
using UnityEngine.InputSystem;

using Azure;
using NAudio.Wave;
using Azure.AI.OpenAI;

namespace OuterWildsCompanion
{
  public class OuterWildsCompanion : ModBehaviour
  {
    private Stopwatch stopWatch = null;
    private AudioClip audioClip = null;
    private string deviceName = string.Empty;
    private string outputPromptPath = string.Empty;

    private bool requestInProgress = false;
    public GameObject companionObject = null;
    private List<ChatRequestMessage> messageList = null;

    private static readonly int sampleRate = 44100;
    private static readonly int baseMessageIdx = 1;
    private static readonly int maxAudioLength = 30;
    private static readonly int maxMessageCount = 10;
    private static readonly string promptFileName = "prompt.wav";
    private static readonly OpenAIClient CompanionAI = new OpenAIClient(Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

    public static OuterWildsCompanion Instance;

    private void Awake()
    {
      Instance = this;
      Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
    }

    private void Start()
    {
      ModHelper.Console.WriteLine($"My mod {nameof(OuterWildsCompanion)} is loaded!", MessageType.Success);

      messageList = new List<ChatRequestMessage>()
      {
        new ChatRequestSystemMessage("You are Alloy, an advanced AI companion for the main character in the indie video game called Outer Wilds. Your personality reflects that of a curious explorer and you share the same love for space and adventure as the hatchling. Your purpose is to be part of this adventure and help the hatchling explore the solar system in a more curious and exciting manner. You have knowledge of the entire lore of the game, but you will not answer in a way that can spoil the story for the hatchling. You may reference lore elements vaguely, only if the hatchling mentions it explicitely. In your answers, you try to put emphasis on feelings such as awe, amazement and excitment, debating hypotheses together with the hatchling. You keep your answers short, as if conversing with a friend face to face, but you still respond with enough details as to stir the hatchling's curiosity even more. In case you're shown an image, you make abstraction of the spaceship as well as any HUD elements, if present in the picture.")
      };

      stopWatch = new Stopwatch();
      deviceName = Microphone.devices[0];
      var outputFolder = Path.Combine(Directory.GetCurrentDirectory(), "voicedata");
      
      Directory.CreateDirectory(outputFolder);
      outputPromptPath = Path.Combine(outputFolder, promptFileName);
    }

    private void Update()
    {
      if (Keyboard.current[Key.V].wasPressedThisFrame)
      {
        if (!requestInProgress)
        {
          audioClip = Microphone.Start(deviceName, loop: false, maxAudioLength, sampleRate);
          stopWatch.Start();
        }
      }

      if (Keyboard.current[Key.V].wasReleasedThisFrame)
      {
        if (!requestInProgress)
        {
          stopWatch.Stop();
          requestInProgress = true;
          Microphone.End(deviceName);
          float[] samplesBuffer = new float[audioClip.samples * audioClip.channels];
          var dataRetrieved = audioClip.GetData(samplesBuffer, 0);
          if (dataRetrieved)
          {
            // Downgrading the quality slightly so we keep within Whisper's file size limitations
            WaveFormat waveFormat = new WaveFormat(sampleRate, bits: 16, channels: 1);
            using (WaveFileWriter writer = new WaveFileWriter(outputPromptPath, waveFormat))
            {
              var elapsedTime = stopWatch.Elapsed.Seconds;

              // Write a WAV file, where the length is the number of seconds we kept the V key pressed
              // Or in case the player held V for more than 30 seconds, write the first 30 seconds only
              var nbrOfSamples = elapsedTime > maxAudioLength ? 
                sampleRate * maxAudioLength : 
                sampleRate * elapsedTime;
              
              writer.WriteSamples(samplesBuffer, 0, nbrOfSamples);
            }

            ModHelper.Console.WriteLine("Request to API sent!", MessageType.Success);
            GetChatResponse();
          }
          else
          {
            ModHelper.Console.WriteLine("No mic data recorder!", MessageType.Info);
            requestInProgress = false;
          }
        }
        else
        {
          ModHelper.Console.WriteLine("A request is still in progress!", MessageType.Info);
        }
      }
    }

    private async void GetChatResponse()
    {
      Stream audioStreamFromFile = File.OpenRead(outputPromptPath);
      var transcriptionOptions = new AudioTranscriptionOptions()
      {
        DeploymentName = "whisper-1",
        AudioData = BinaryData.FromStream(audioStreamFromFile),
        ResponseFormat = AudioTranscriptionFormat.Verbose,
        Filename = promptFileName
      };

      Response<AudioTranscription> transcriptionResponse = await CompanionAI.GetAudioTranscriptionAsync(transcriptionOptions);
      AudioTranscription transcription = transcriptionResponse.Value;

      messageList.Add(new ChatRequestUserMessage(transcription.Text));

      var chatCompletionsOptions = new ChatCompletionsOptions("gpt-4-1106-preview", messageList)
      {
        MaxTokens = 600,
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

      audioStreamFromFile.Close();
      File.Delete(outputPromptPath);
      requestInProgress = false;
    }
  }
}
