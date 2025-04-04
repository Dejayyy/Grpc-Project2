using Grpc.Core;
using System.Text.Json;
using WordleGameServer.Protos;
using WordServer.Protos;

namespace WordleGameServer.Services
{
    public class GameService : DailyWordle.DailyWordleBase
    {
        private readonly DailyWord.DailyWordClient _wordClient;
        private readonly string StatsFilePath = "stats.json";
        private readonly Mutex StatsMutex = new();

        public GameService(DailyWord.DailyWordClient wordClient)
        {
            _wordClient = wordClient;
        }

        public async Task<string> GetDailyWord()
        {
            try
            {
                var req = new WordRequest();
                var reply = await _wordClient.GetWordAsync(req);
                return reply.Word;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                return "Error";
            }
        }

        public override async Task Play(IAsyncStreamReader<GuessRequest> requestStream, IServerStreamWriter<GuessResponse> responseStream, ServerCallContext context)
        {
            string dailyWord = await GetDailyWord();
            int turnsUsed = 0;
            bool gameWon = false;

            // letter handling
            HashSet<char> unusedLetters = new("abcdefghijklmnopqrstuvwxyz");
            HashSet<char> includedLetters = new();
            HashSet<char> excludedLetters = new();

            // create dictionary to see if theres a match
            Dictionary<char, int> matches = new();

            foreach (char c in "abcdefghijklmnopqrstuvwxyz")
                matches[c] = 0;

            await foreach (var request in requestStream.ReadAllAsync())
            {
                // if game is over, break, else increase turns
                if (turnsUsed >= 6 || gameWon) break;
                turnsUsed++;

                string guess = request.Word.ToLower();

                // validate
                bool isValid = (await _wordClient.ValidateWordAsync(new WordToValidate { Word = guess })).IsValid;

                if (!isValid)
                {
                    await responseStream.WriteAsync(new GuessResponse
                    {
                        Reply = "Invalid word",
                        GameOver = false
                    });
                    continue;
                }

                // process
                char[] results = new char[5];
                for (int i = 0; i < 5; i++) results[i] = 'x';

                // check for correct
                for (int i = 0; i < 5; i++)
                {
                    if (guess[i] == dailyWord[i])
                    {
                        results[i] = '*';
                        matches[guess[i]]++;
                        includedLetters.Add(guess[i]);
                    }
                }

                // check for misplaced
                for (int i = 0; i < 5; i++)
                {
                    if (guess[i] != dailyWord[i] && dailyWord.Contains(guess[i]))
                    {
                        if (matches[guess[i]] < dailyWord.Count(c => c == guess[i]))
                        {
                            results[i] = '?';
                            matches[guess[i]]++;
                            includedLetters.Add(guess[i]);
                        }
                    }
                }

                // remove excluded
                foreach (char c in guess)
                {
                    if (!dailyWord.Contains(c)) excludedLetters.Add(c);
                    unusedLetters.Remove(c);
                }

                // if all stars, game won
                gameWon = results.All(c => c == '*');
                bool gameOver = gameWon || turnsUsed == 6;

                // update stats
                await responseStream.WriteAsync(new GuessResponse
                {
                    Reply = new string(results),
                    IsCorrect = gameWon,
                    GameOver = gameOver,
                    UnusedLetters = { unusedLetters.Select(c => c.ToString()) }
                });

                if (gameOver) UpdateStats(gameWon, turnsUsed);
            }
        }

        public override Task<GameStats> GetStats(Protos.Empty request, ServerCallContext context)
        {
            StatsMutex.WaitOne();

            try
            {
                if (!File.Exists(StatsFilePath))
                {
                    return Task.FromResult(new GameStats
                    {
                        TotalGames = 0,
                        GamesWon = 0,
                        GamesLost = 0,
                        AverageTurns = 0
                    });
                }

                // handle json
                var json = File.ReadAllText(StatsFilePath);
                var stats = JsonSerializer.Deserialize<Stats>(json) ?? new Stats();

                // calculate average turns
                double avgTurns = stats.TotalGames > 0 ? (double)stats.TotalTurns / stats.TotalGames : 0;

                return Task.FromResult(new GameStats {
                    TotalGames = stats.TotalGames,
                    GamesWon = stats.GamesWon,
                    GamesLost = stats.GamesLost,
                    AverageTurns = avgTurns
                });
            }
            finally
            { 
                StatsMutex.ReleaseMutex(); 
            }
        }

        private void UpdateStats(bool won, int turnsUsed)
        {
            StatsMutex.WaitOne();
            try
            {
                Stats stats;
                if (File.Exists(StatsFilePath))
                {
                    var json = File.ReadAllText(StatsFilePath);
                    stats = JsonSerializer.Deserialize<Stats>(json) ?? new Stats();
                }
                else
                {
                    stats = new Stats();
                }

                stats.TotalGames++;
                if (won)
                    stats.GamesWon++;
                else
                    stats.GamesLost++;

                stats.TotalTurns += turnsUsed;

                var updatedJson = JsonSerializer.Serialize(stats, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(StatsFilePath, updatedJson);
            }
            finally
            {
                StatsMutex.ReleaseMutex();
            }
        }

        private class Stats
        {
            public int TotalGames { get; set; } = 0;
            public int GamesWon { get; set; } = 0;
            public int GamesLost { get; set; } = 0;
            public int TotalTurns { get; set; } = 0;
        }
    }
}
