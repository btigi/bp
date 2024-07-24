using bpe;
using Microsoft.Extensions.Configuration;
using System.CommandLine;

var builder = new ConfigurationBuilder()
                .AddJsonFile($"bpe.json", true, true);

var config = builder.Build();

/* Read shared config from environment variables */
var modelConfig = config["Model"];
var ollamaUrlConfig = config["OllamaUrl"];
var vectorDBConfig = config["VectorDBpath"];

if (String.IsNullOrEmpty(modelConfig) ||
    String.IsNullOrEmpty(ollamaUrlConfig) ||
    String.IsNullOrEmpty(vectorDBConfig))
{
    Console.WriteLine("Local settings not found.");
    return 1;
}

var model = Environment.GetEnvironmentVariable(modelConfig, EnvironmentVariableTarget.Machine);
var ollamaUrl = Environment.GetEnvironmentVariable(ollamaUrlConfig, EnvironmentVariableTarget.Machine);
var vectorDBDirectory = Environment.GetEnvironmentVariable(vectorDBConfig, EnvironmentVariableTarget.Machine);

if (String.IsNullOrEmpty(model) ||
    String.IsNullOrEmpty(ollamaUrl) ||
    String.IsNullOrEmpty(vectorDBDirectory))
{
    Console.WriteLine("Environment settings not found.");
    return 1;
}

if (System.Diagnostics.Debugger.IsAttached)
{
    args = new string[4];
    args[0] = "--directory";
    args[1] = @"C:\_";
    args[2] = "--filetype";
    args[3] = "txt";
}


/* Read command-line arguments */
const int DefaultIndexCount = 32;
var rootCommand = new RootCommand("File vectorization");

var directoryOption = new Option<string>(name: "--directory", description: "The directory to index");
directoryOption.AddAlias("-d");
directoryOption.IsRequired = true;
rootCommand.AddOption(directoryOption);

var filetypeOption = new Option<string>(name: "--filetype", description: "The file type to process, e.g. txt");
filetypeOption.AddAlias("-f");
filetypeOption.IsRequired = true;
rootCommand.AddOption(filetypeOption);

var indexCountOption = new Option<int>(name: "--indexcount", description: $"The number of indexes (defaults to {DefaultIndexCount})");
indexCountOption.AddAlias("-i");
indexCountOption.SetDefaultValue(DefaultIndexCount);
rootCommand.AddOption(indexCountOption);

rootCommand.SetHandler(
    (directory, filetype, indexCount) => { Process(directory, filetype, model, ollamaUrl, vectorDBDirectory, indexCount); },
    directoryOption, filetypeOption, indexCountOption
);

return await rootCommand.InvokeAsync(args);

/* Run the vectorization process */
static void Process(string directory, string filetype, string model, string ollamaUrl, string databasePath, int indexCount)
{
    // Always process all files of the specified file type in the specified directory, adding all content
    var vectorDB = new HyperVectorDB.HyperVectorDB(new OllamaEmbedder(model, ollamaUrl), databasePath, indexCount);
    if (Directory.Exists(databasePath) && File.Exists(Path.Combine(databasePath, "indexs.txt")))
    {
        Console.WriteLine("Loading database");
        vectorDB.Load();
    }
    else
    {
        Console.WriteLine("Creating database");
        vectorDB.CreateIndex("Index");
    }

    var files = Directory.EnumerateFiles(directory, $"*.{filetype}", SearchOption.AllDirectories);
    Console.WriteLine($"Indexing {files.Count()} files.");
    int i = 0;
    foreach (string file in files)
    {
        Console.WriteLine(file);
        vectorDB.IndexDocumentFile(file);
        if (i % 10 == 0)
        {
            vectorDB.Save();
        }
        i++;
    }
    vectorDB.Save();

    Console.WriteLine("Indexing complete");
}