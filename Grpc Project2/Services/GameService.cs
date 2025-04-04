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

        public override async Task Play(IAsyncStreamReader<GameRequest> requestStream, IServerStreamWriter<GameResponse> responseStream, ServerCallContext context)
        {
            string dailyWord = await GetDailyWord();
            bool gameWon = false;
            int guesses = 0;

            // letter handling
            HashSet<char> unusedLetters = new("abcdefghijklmnopqrstuvwxyz");
            HashSet<char> includedLetters = new();
            HashSet<char> excludedLetters = new();

            Dictionary<char, int> matches = new();

            // Add a player
            UpdateStats(stats => stats.PlayerCount++);

            await responseStream.WriteAsync(new GameResponse { TodaysWord = dailyWord });

            while (await requestStream.MoveNext())
            {
                string guess = requestStream.Current.Guess.Trim();
                
                // validate - make sure its a valid wordle word
                var validation = await _wordClient.ValidateWordAsync(new WordToValidate { Word = guess.ToLower() });

                if (!validation.IsValid)
                {
                    string invalidMessage = "INVALID";
                    await responseStream.WriteAsync(new GameResponse { Message = invalidMessage });
                    continue; 
                }

                // guess is valid, increment
                guesses++;

                // build out the result
                char[] results = new char[5];
                for (int i = 0; i < 5; i++) results[i] = 'x';

                // check for correct placements
                for (int i = 0; i < 5; i++)
                {
                    if (guess[i] == dailyWord[i])
                    {
                        results[i] = '*';
                        matches[guess[i]] = matches.GetValueOrDefault(guess[i], 0) + 1;
                        includedLetters.Add(guess[i]);
                    }
                }

                // check for misplaced letters
                for (int i = 0; i  < 5; i++)
                {
                    if (guess[i] != dailyWord[i] && dailyWord.Contains(guess[i]))
                    {
                        int currentMatchCount = matches.GetValueOrDefault(guess[i], 0);
                        int totalCountInWord = dailyWord.Count(c => c == guess[i]);

                        if (currentMatchCount < totalCountInWord)
                        {
                            results[i] = '?';
                            matches[guess[i]] = currentMatchCount + 1;
                            includedLetters.Add(guess[i]);
                        }
                    }
                }

                // remove letters not in word
                foreach (char c in guess)
                {
                    if (!dailyWord.Contains(c)) excludedLetters.Add(c);
                    unusedLetters.Remove(c);
                }

                // if all stars, then the game is won
                gameWon = results.All(c => c == '*');

                string resultMessage = $"     {new string(results)}\n\n" +
                                       $"     Included: {string.Join(",", includedLetters)}\n" +
                                       $"     Available: {string.Join(",", unusedLetters)}\n" +
                                       $"     Excluded: {string.Join(",", excludedLetters)}\n";

                // send to response stream
                await responseStream.WriteAsync(new GameResponse { Message = resultMessage });

                if (gameWon) break;
            }

            UpdateStats(stats => {
                stats.GuessesMade += guesses;
                if (gameWon)
                    stats.GamesWon++;
                else
                    stats.GamesLost++;
            });
        }

        public override Task<StatsResponse> GetStats(StatsRequest request, ServerCallContext context)
        {
            StatsMutex.WaitOne();

            // try-finally for mutex handling
            try
            {
                // get the stats
                if (File.Exists(StatsFilePath))
                {
                    var json = File.ReadAllText(StatsFilePath);
                    var stats = JsonSerializer.Deserialize<BlankStats>(json);

                    if (stats != null)
                    {
                        return Task.FromResult(new StatsResponse
                        {
                            PlayerCount = stats.PlayerCount,
                            GamesWon = stats.GamesWon,
                            GamesLost = stats.GamesLost,
                            GuessesMade = stats.GuessesMade
                        });
                    }
                }
            }
            finally
            {
                StatsMutex.ReleaseMutex(); 
            }

            return Task.FromResult(new StatsResponse());
        }

        private void UpdateStats(Action<BlankStats> update)
        {
            StatsMutex.WaitOne();

            // try-finally
            try
            {
                BlankStats stats;
                if (File.Exists(StatsFilePath))
                {
                    var json = File.ReadAllText(StatsFilePath);
                    stats = JsonSerializer.Deserialize<BlankStats>(json) ?? new BlankStats();
                }
                else
                {
                    stats = new BlankStats();
                }

                update(stats);

                var updatedJson = JsonSerializer.Serialize(stats);
                File.WriteAllText(StatsFilePath, updatedJson);
            }
            finally
            {
                StatsMutex.ReleaseMutex();
            }
        }

        public class BlankStats
        {
            public int PlayerCount { get; set; } = 0;
            public int GamesWon { get; set; } = 0;
            public int GamesLost { get; set; } = 0;
            public int GuessesMade { get; set; } = 0;
        }
    }
}
