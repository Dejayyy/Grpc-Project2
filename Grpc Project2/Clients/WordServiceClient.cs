// William Mouhtouris and Ayden Nicholson
// Project 2 - Wordle
// 04-04-2025

using Grpc.Net.Client;
using WordServer.Protos;

namespace WordleGameServer.Clients
{
    public static class WordServiceClient
    {
        private static DailyWord.DailyWordClient? _client = null;

        // gets the word
        public static string GetWord(string word)
        {
            ConnectToService();
            WordResponse? resp = _client?.GetWord(new WordRequest { Word = word });
            return resp?.Word ?? "";
        }

        // connects to the grpc
        private static void ConnectToService()
        {
            if (_client is null)
            {
                var channel = GrpcChannel.ForAddress("https://localhost:7227");
                _client = new DailyWord.DailyWordClient(channel);
            }
        }
    }
}
