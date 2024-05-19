using ElevenLabs;
using Microsoft.Extensions.Configuration;
using NetCoreAudio;
using OllamaSharp;

var builder = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.json", true, true);

var config = builder.Build();

var modelConfig = config["Model"];
var initialPromptConfig = config["InitialPrompt"];
var welcomeMessageConfig = config["WelcomeMessage"];
var elevenLabsApiKeyConfig = config["ElevenLabsApiKey"];
var elevenLabsVoiceIdConfig = config["VoiceId"];
var ollamaUrlConfig = config["OllamaUrl"];

if (String.IsNullOrEmpty(modelConfig) ||
    String.IsNullOrEmpty(initialPromptConfig) ||
    String.IsNullOrEmpty(welcomeMessageConfig) ||
    String.IsNullOrEmpty(elevenLabsApiKeyConfig) ||
    String.IsNullOrEmpty(elevenLabsVoiceIdConfig) ||
    String.IsNullOrEmpty(ollamaUrlConfig))
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

if (String.IsNullOrEmpty(model) ||
    String.IsNullOrEmpty(initialPrompt) ||
    String.IsNullOrEmpty(welcomeMessage) ||
    String.IsNullOrEmpty(elevenLabsApiKey) ||
    String.IsNullOrEmpty(elevenLabsVoiceId) ||
    String.IsNullOrEmpty(ollamaUrl))
{
    Console.WriteLine("Environment settings not found.");
    return;
}

var uri = new Uri(ollamaUrl);
var ollama = new OllamaApiClient(uri)
{
    SelectedModel = model
};

var api = new ElevenLabsClient(elevenLabsApiKey);

var context = await ollama.GetCompletion(initialPrompt, null!);

Console.WriteLine(welcomeMessage);
Console.Write("> ");
var line = Console.ReadLine();

while (!string.IsNullOrEmpty(line))
{
    var completion = await ollama.GetCompletion(line, context);
    context = completion;

    var text = completion.Response;

    if (!line.EndsWith('~'))
    {
        var voice = await api.VoicesEndpoint.GetVoiceAsync(elevenLabsVoiceId);
        var defaultVoiceSettings = await api.VoicesEndpoint.GetDefaultVoiceSettingsAsync();
        var voiceClip = await api.TextToSpeechEndpoint.TextToSpeechAsync(text, voice, defaultVoiceSettings);
        await File.WriteAllBytesAsync($"{voiceClip.Id}.mp3", voiceClip.ClipData.ToArray());
        Console.WriteLine(completion.Response);
        Console.WriteLine();
        var player = new Player();
        player.Play($"{voiceClip.Id}.mp3").Wait();
    }
    else
    {
        Console.WriteLine(completion.Response);
        Console.WriteLine();
    }
    Console.Write("> ");
    line = Console.ReadLine();
}