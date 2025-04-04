using Grpc.Net.Client;
using WordServer.Protos;

namespace WordleGameServer.Clients
{
    public static class WordServiceClient
    {
        private static DailyWord.DailyWordClient? _client = null;

        public static string GetWord(string word)
        {
            ConnectToService();
            WordResponse? resp = _client?.GetWord(new WordRequest { Word = word });
            return resp?.Word ?? string.Empty;
        }

        private static void ConnectToService()
        {
            if (_client is null)
            {
                var channel = GrpcChannel.ForAddress("https://localhost:7206");
                _client = new DailyWord.DailyWordClient(channel);
            }
        }
    }
}
