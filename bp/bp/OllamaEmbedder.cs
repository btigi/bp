using HyperVectorDB.Embedder;
using OllamaSharp;
using System.Reflection.Metadata.Ecma335;

namespace bpe
{
    internal class OllamaEmbedder : IEmbedder
    {
        private readonly OllamaApiClient client;

        public OllamaEmbedder(string model, string url)
        {
            var uri = new Uri(url);
            client = new OllamaApiClient(uri)
            {
                SelectedModel = model
            };
        }

        public double[] GetVector(string Document)
        {
            var embedResponse1 = client.GenerateEmbeddings(Document).GetAwaiter().GetResult();
            return embedResponse1.Embedding;
        }

        public double[][] GetVectors(string[] Documents)
        {
            List<double[]> vectors = new();
            foreach (string document in Documents)
            {
                vectors.Add(GetVector(document));
            }
            return vectors.ToArray();
        }
    }
}