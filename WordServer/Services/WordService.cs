using WordServer.Protos;
using Grpc.Core;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;

namespace WordServer.Services
{
    public class WordService : DailyWord.DailyWordBase
    {
        private string JsonFilePath = "Wordle.json";
        private List<string> Words = new();
        private Dictionary<DateTime, string> WordPairs = new();

        public WordService()
        {
            if (File.Exists(JsonFilePath))
            {
                Words = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(JsonFilePath));
            }
        }

        public override Task<WordResponse> GetWord(Empty request, ServerCallContext context)
        {
            DateTime today = DateTime.UtcNow.Date;

            if (!WordPairs.ContainsKey(today))
            {
                var random = new Random();
                WordPairs[today] = Words[random.Next(Words.Count)];
            }

            return Task.FromResult(new WordResponse { Word = WordPairs[today] } );
        }

        public override Task<ValidationResponse> ValidateWord(WordRequest request,  ServerCallContext context)
        {
            bool isValid = Words.Contains(request.Word.ToUpper());
            return Task.FromResult(new ValidationResponse { IsValid = isValid } );
        }
    }
}
