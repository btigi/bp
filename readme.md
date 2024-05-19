## Introduction

bp is console application that wraps conversation with ollama and runs the resultant text through Eleven Labs.

## Download

Compiled downloads are not available.

## Compiling

To clone and run this application, you'll need [Git](https://git-scm.com) and [.NET](https://dotnet.microsoft.com/) installed on your computer. From your command line:

```
# Clone this repository
$ git clone https://github.com/btigi/bp

# Go into the repository
$ cd src

# Build  the app
$ dotnet build
```

## Prerequisites

- Install [ollama](https://ollama.com/)
- Create an account on [Elevn Labs](https://elevenlabs.io/) and obtain an API key

## Usage

bp requires six configuration settings to run, read from environment variables. To accommodate multiple instances of bp running on the same machine the names of the variables are stored in the appsettings.json file, meaning each instance can point to a different set of environment variables. The six settings are:
 - Model - the name of the ollama model to use. The model is not installed by bp and must be pre-configured in ollama.
 - InitialPrompt - the initial prompt send to the model.
 - WelcomeMessage - the text displayed to the user when the application initialization is complete.
 - ElevenLabsApiKey - the API key to use when calling Eleven Labs.
 - VoiceId - the ID of the voice to use when calling Eleven Labs.
 - ollamaUrl - the URL of the ollama instance to call (usually http://localhost:11434)

 Once configured you can run bp at the command line:
 ```bp```

 bp will show the Welcome Message, then you can have a question / answer conversation with the ollama model. Responses provided by the model are narrated in the Eleven Labs voice you have specified.

 You can skip the call to Elven Labs and response narration by ending your query with the ~ character.


## Licencing

bp is licenced under CC BY-NC-ND 4.0 https://creativecommons.org/licenses/by-nc-nd/4.0/ Full licence details are available in licence.md