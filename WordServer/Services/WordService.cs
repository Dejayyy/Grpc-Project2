using WordServer.Protos;
using Grpc.Core;
using System;
using Newtonsoft.Json;

namespace WordServer.Services
{
    public class WordService : DailyWord.DailyWordBase
    {
        private string _jsonFilePath = "Wordle.json";
        private List<string> _words = new();
        private string _todaysWord;

        public WordService()
        {
            _words = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(_jsonFilePath));

            var random = new Random();
            int index = random.Next(_words.Count);
            _todaysWord = _words[index].Trim().ToLower();
        }

        public override Task<WordResponse> GetWord(WordRequest request, ServerCallContext context)
        {
            return Task.FromResult(new WordResponse { Word = _todaysWord });
        }

        public override Task<ValidationResponse> ValidateWord(WordToValidate request,  ServerCallContext context)
        {
            bool isValid = _words.Contains(request.Word.ToLower());
            return Task.FromResult(new ValidationResponse { IsValid = isValid } );
        }
    }
}
