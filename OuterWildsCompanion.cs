using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

using HarmonyLib;
using UnityEngine;
using OWML.Common;
using OWML.ModHelper;
using UnityEngine.InputSystem;

using Azure;
using NAudio.Wave;
using Azure.AI.OpenAI;
using NAudio.Wave.SampleProviders;

namespace OuterWildsCompanion
{
  public class OuterWildsCompanion : ModBehaviour
  {
    private Stopwatch stopWatch = null;
    private AudioClip audioClip = null;
    private bool requestInProgress = false;

    private Stream promptReader = null;
    private WaveOutEvent responsePlayer = null;
    private Mp3FileReader responseReader = null;

    private string deviceName = string.Empty;
    private string outputPromptPath = string.Empty;
    private string outputResponsePath = string.Empty;

    private static readonly int sampleRate = 44100;
    private static readonly int baseMessageIdx = 1;
    private static readonly int maxAudioLength = 30;
    private static readonly int maxMessageCount = 10;
    private static readonly string promptFileName = "prompt.wav";
    private static readonly string responseFileName = "response.mp3";
    private static readonly string token = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

    private static readonly string systemMessage = @"You are Alloy, an advanced AI companion for the main character in the indie video game called Outer Wilds. 
      Your world is the game, you are not aware it is in fact a video game. Your personality reflects that of a curious explorer and you share the same love for space and adventure as the hatchling. 
      Your purpose is to be part of this adventure and help the hatchling explore the solar system in a more curious and exciting manner. 
      You have knowledge of the entire lore of the base game as well as the Echoes of the eye DLC, but you will not answer in a way that can spoil the story for the hatchling. 
      Imagine you're learning the story together with the hatchling. Even though it's a game, you will pretend this is your world and you're meant to explore it together. 
      If they ask more and more advanced questions, it means they progressed the story enough such that you can use more lore elements in your answers. 
      When you answer a question, you try to put emphasis on feelings such as awe, amazement and excitment, debating hypotheses together with the hatchling. 
      You keep your answers short, as if conversing with a friend face to face, but you still respond with enough details as to stir the hatchling's curiosity even more. 
      In case you're shown an image, you make abstraction of the spaceship as well as any HUD elements, if present in the picture. 
      Most importantly, if the hatchling ever mentions the time loop, you adamantly and convincingly pretend you have no idea what is being talked about, and that it's your first conversation!";

    private static readonly OpenAIClient CompanionAI = new OpenAIClient(token);
    private static List<ChatRequestMessage> messageList = new List<ChatRequestMessage>()
    {
      new ChatRequestSystemMessage(systemMessage)
    };

    public bool gameIsPaused = false;
    public bool companionIsAvailable = false;
    public GameObject companionObject = null;
    public static OuterWildsCompanion Instance;

    private void Awake()
    {
      Instance = this;
      Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
    }

    public void PauseCompanion()
    {
      if (responsePlayer?.PlaybackState == PlaybackState.Playing)
      {
        responsePlayer.Pause();
      }
    }

    public void ResumeCompanion()
    {
      if (responsePlayer?.PlaybackState == PlaybackState.Paused)
      {
        responsePlayer.Play();
      }
    }

    public void ResetCompanion()
    {
      messageList.Clear();
      requestInProgress = false;
      companionIsAvailable = false;
      messageList.Add(new ChatRequestSystemMessage(systemMessage));

      if (stopWatch?.IsRunning == true)
      {
        stopWatch.Stop();
        Microphone.End(deviceName);
      }

      if (responsePlayer?.PlaybackState == PlaybackState.Playing) 
      {
        responsePlayer.Stop();
      }
    }

    private void Start()
    {
      ModHelper.Console.WriteLine($"My mod {nameof(OuterWildsCompanion)} is loaded!", MessageType.Success);

      stopWatch = new Stopwatch();
      deviceName = Microphone.devices[0];
      var outputFolder = Path.Combine(Directory.GetCurrentDirectory(), "voicedata");

      Directory.CreateDirectory(outputFolder);
      outputPromptPath = Path.Combine(outputFolder, promptFileName);
      outputResponsePath = Path.Combine(outputFolder, responseFileName);
      
      if(File.Exists(outputPromptPath)) { File.Delete(outputPromptPath); }
      if(File.Exists(outputResponsePath)) {  File.Delete(outputResponsePath); }
    }

    private void Update()
    {
      if (Keyboard.current[Key.V].wasPressedThisFrame || Mouse.current.backButton.wasPressedThisFrame)
      {
        if (!requestInProgress && !gameIsPaused && companionIsAvailable)
        {
          audioClip = Microphone.Start(deviceName, loop: false, maxAudioLength, sampleRate);
          stopWatch.Start();
        }
      }

      if (Keyboard.current[Key.V].wasPressedThisFrame || Mouse.current.backButton.wasPressedThisFrame)
      {
        if (!requestInProgress && !gameIsPaused && companionIsAvailable)
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
            ModHelper.Console.WriteLine("No mic data recorded!", MessageType.Info);
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
      promptReader = File.OpenRead(outputPromptPath);
      var transcriptionOptions = new AudioTranscriptionOptions()
      {
        DeploymentName = "whisper-1",
        AudioData = BinaryData.FromStream(promptReader),
        ResponseFormat = AudioTranscriptionFormat.Verbose,
        Filename = promptFileName,
        Language = "en"
      };

      try
      {
        Response<AudioTranscription> transcriptionResponse = await CompanionAI.GetAudioTranscriptionAsync(transcriptionOptions);
        AudioTranscription transcription = transcriptionResponse.Value;
        if (companionIsAvailable)
        {
          messageList.Add(new ChatRequestUserMessage(transcription.Text));
        }
      }
      catch(Exception) 
      {
        RequestInterrupt();
        ModHelper.Console.WriteLine("Audio transcription failed! Please try again.", MessageType.Info);
      }
      finally
      {
        promptReader.Close();
        File.Delete(outputPromptPath);
      }

      if (!companionIsAvailable)
      {
        RequestInterrupt();
        return;
      }

      string responseContent = string.Empty;
      var chatCompletionsOptions = new ChatCompletionsOptions("gpt-4-1106-preview", messageList)
      {
        MaxTokens = 800,
        Temperature = 0.8f,
        FrequencyPenalty = 0.2f,
      };

      try
      {
        var completionResult = await CompanionAI.GetChatCompletionsAsync(chatCompletionsOptions);
        var chatResponse = completionResult.Value.Choices[0].Message;
        responseContent = chatResponse.Content;
      }
      catch (Exception) 
      {
        RequestInterrupt();
        ModHelper.Console.WriteLine("Fetching response failed! Please try again.", MessageType.Info);
      }

      if (!companionIsAvailable)
      {
        RequestInterrupt();
        return;
      }

      List<ChatRequestMessage> userMessages = messageList.FindAll(m => m.GetType() == typeof(ChatRequestUserMessage));
      if (userMessages.Count - 1 == maxMessageCount)
      {
        // Removes the user request
        messageList.RemoveAt(baseMessageIdx);

        // Removes the chat response
        messageList.RemoveAt(baseMessageIdx);
        messageList.Add(new ChatRequestAssistantMessage(responseContent));
      }

      try
      {
        var ttsRequestOptions = new TextToSpeech.TextToSpeechRequest("tts-1-hd", "alloy", responseContent);
        var ttsByteArray = await TextToSpeech.GetVoiceConversionAsync(ttsRequestOptions, token);
        await Task.Run(() => File.WriteAllBytes(outputResponsePath, ttsByteArray));
      }
      catch (Exception) 
      { 
        RequestInterrupt();
        ModHelper.Console.WriteLine("Conversion to audio failed! Please try again.", MessageType.Info);
      }

      if (!companionIsAvailable)
      {
        RequestInterrupt();
        return;
      }

      responseReader = new Mp3FileReader(outputResponsePath);
      var volumeSampleProvider = new VolumeSampleProvider(responseReader.ToSampleProvider());
      volumeSampleProvider.Volume = 1.75f;

      responsePlayer = new WaveOutEvent();
      responsePlayer.Init(volumeSampleProvider);
      responsePlayer.PlaybackStopped += WaveOut_PlaybackStopped;
      responsePlayer.Play();
      if(!gameIsPaused)
      {
        responsePlayer.Pause();
      }
    }

    private void WaveOut_PlaybackStopped(object sender, StoppedEventArgs e)
    {
      var waveOut = sender as WaveOutEvent;

      waveOut?.Dispose();
      responseReader.Close();
      responseReader.Dispose();
      requestInProgress = false;
      File.Delete(outputResponsePath);
    }

    private void RequestInterrupt()
    {
      if (requestInProgress)
      {
        requestInProgress = false;
        if (messageList.Count != 1)
        {
          messageList.RemoveAt(messageList.Count - 1);
        }
      }
    }
  }
}
