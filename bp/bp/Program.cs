﻿using bpe;
using ElevenLabs;
using Microsoft.Extensions.Configuration;
using NetCoreAudio;
using OllamaSharp;

var configFile = "bp.json";
if (args.Length == 1 && File.Exists(args[0]))
{
    configFile = args[0];
}

var builder = new ConfigurationBuilder()
                  .AddJsonFile(configFile, true, true);

var config = builder.Build();

var modelConfig = config["Model"];
var initialPromptConfig = config["InitialPrompt"];
var welcomeMessageConfig = config["WelcomeMessage"];
var elevenLabsApiKeyConfig = config["ElevenLabsApiKey"];
var elevenLabsVoiceIdConfig = config["VoiceId"];
var ollamaUrlConfig = config["OllamaUrl"];
var keepVoiceFilesConfig = config["KeepVoiceFiles"];
var voiceFileDirectoryConfig = config["VoiceFileDirectory"];
var vectorDBConfig = config["VectorDBpath"];

if (String.IsNullOrEmpty(modelConfig) ||
    String.IsNullOrEmpty(initialPromptConfig) ||
    String.IsNullOrEmpty(welcomeMessageConfig) ||
    String.IsNullOrEmpty(elevenLabsApiKeyConfig) ||
    String.IsNullOrEmpty(elevenLabsVoiceIdConfig) ||
    String.IsNullOrEmpty(ollamaUrlConfig) ||
    String.IsNullOrEmpty(voiceFileDirectoryConfig))
{
    Console.WriteLine("Local settings not found.");
    return;
}

var model = Environment.GetEnvironmentVariable(modelConfig, EnvironmentVariableTarget.Machine);
var initialPrompt = Environment.GetEnvironmentVariable(initialPromptConfig, EnvironmentVariableTarget.Machine);
var welcomeMessage = Environment.GetEnvironmentVariable(welcomeMessageConfig, EnvironmentVariableTarget.Machine);
var elevenLabsApiKey = Environment.GetEnvironmentVariable(elevenLabsApiKeyConfig, EnvironmentVariableTarget.Machine);
var elevenLabsVoiceId = Environment.GetEnvironmentVariable(elevenLabsVoiceIdConfig, EnvironmentVariableTarget.Machine);
var ollamaUrl = Environment.GetEnvironmentVariable(ollamaUrlConfig, EnvironmentVariableTarget.Machine);
var voiceFileDirectory = Environment.GetEnvironmentVariable(voiceFileDirectoryConfig, EnvironmentVariableTarget.Machine);
var vectorDBDirectory = String.Empty;
if (!String.IsNullOrEmpty(vectorDBConfig))
{
    vectorDBDirectory = Environment.GetEnvironmentVariable(vectorDBConfig, EnvironmentVariableTarget.Machine);
}

var keepVoiceFiles = false;
if (!String.IsNullOrEmpty(keepVoiceFilesConfig))
{
    _ = bool.TryParse(Environment.GetEnvironmentVariable(keepVoiceFilesConfig, EnvironmentVariableTarget.Machine), out keepVoiceFiles);
}

if (String.IsNullOrEmpty(model) ||
    String.IsNullOrEmpty(initialPrompt) ||
    String.IsNullOrEmpty(welcomeMessage) ||
    String.IsNullOrEmpty(elevenLabsApiKey) ||
    String.IsNullOrEmpty(elevenLabsVoiceId) ||
    String.IsNullOrEmpty(ollamaUrl) ||
    String.IsNullOrEmpty(voiceFileDirectory))
{
    Console.WriteLine("Environment settings not found.");
    return;
}

if (!Path.Exists(voiceFileDirectory))
{
    Directory.CreateDirectory(voiceFileDirectory);
}

if (!Path.EndsInDirectorySeparator(voiceFileDirectory))
{
    voiceFileDirectory += Path.DirectorySeparatorChar;
}

var uri = new Uri(ollamaUrl);
var ollama = new OllamaApiClient(uri)
{
    SelectedModel = model
};

var api = new ElevenLabsClient(elevenLabsApiKey);

var context = await ollama.GetCompletion(initialPrompt, null!);

HyperVectorDB.HyperVectorDB? vectorDB = null;
if (!String.IsNullOrEmpty(vectorDBDirectory))
{
    vectorDB = new HyperVectorDB.HyperVectorDB(new OllamaEmbedder(model, ollamaUrl), vectorDBDirectory, 32);
    vectorDB.Load();
}

Console.WriteLine(welcomeMessage);
Console.Write("> ");
var line = Console.ReadLine();

while (!string.IsNullOrEmpty(line))
{
    var promptText = line;
    if (vectorDB != null)
    {
        var rag = vectorDB.QueryCosineSimilarity(line, 1);
        promptText = $"Using this information \"{String.Join(" ", rag.Documents.Select(s => s.DocumentString))}\" answer this prompt: {line}";
    }

    var completion = await ollama.GetCompletion(promptText, context);
    context = completion;

    var text = completion.Response;

    if (!line.EndsWith('~'))
    {
        try
        {
            var voice = await api.VoicesEndpoint.GetVoiceAsync(elevenLabsVoiceId);
            var defaultVoiceSettings = await api.VoicesEndpoint.GetDefaultVoiceSettingsAsync();
            var voiceClip = await api.TextToSpeechEndpoint.TextToSpeechAsync(text, voice, defaultVoiceSettings);
            await File.WriteAllBytesAsync($"{voiceFileDirectory}{voiceClip.Id}.mp3", voiceClip.ClipData.ToArray());
            Console.WriteLine(completion.Response);
            Console.WriteLine();
            var player = new Player();
            player.Play($"{voiceFileDirectory}{voiceClip.Id}.mp3").Wait();

            if (!keepVoiceFiles)
            {
                File.Delete($"{voiceFileDirectory}{voiceClip.Id}.mp3");
            }
        }
        catch
        {
            Console.WriteLine($"[voiceless] {completion.Response}");
            Console.WriteLine();
        }
    }
    else
    {
        Console.WriteLine(completion.Response);
        Console.WriteLine();
    }
    Console.Write("> ");
    line = Console.ReadLine();
}