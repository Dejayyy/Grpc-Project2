using Grpc.Core;
using Grpc.Net.Client;
using WordleGameServer.Protos;

namespace WordleGameClient
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            var channel = GrpcChannel.ForAddress("https://localhost:7257");
            var client = new DailyWordle.DailyWordleClient(channel);

            // display the start message
            DisplayStart();

            // bidirectional stream
            using var call = client.Play();

            // get the word
            string dailyWord = "";
            if (await call.ResponseStream.MoveNext(default))
            {
                var response = call.ResponseStream.Current;
                dailyWord = response.TodaysWord;
            }

            // handle game
            int guesses = 0;

            try
            {
                while (guesses < 6)
                {
                    // loop until a valid guess is made
                    bool validGuess = false;
                    string guess = "";

                    do
                    {
                        Console.Write($"({guesses + 1}): ");
                        guess = Console.ReadLine().Trim();

                        if (string.IsNullOrWhiteSpace(guess) || guess.Length != 5)
                        {
                            Console.WriteLine("\tInvalid Word, try again");
                            continue;
                        }

                        validGuess = true;
                    } while (!validGuess);

                    // write it to the stream
                    await call.RequestStream.WriteAsync(new GameRequest { Guess = guess });

                    // get response
                    if (await call.ResponseStream.MoveNext(default))
                    {
                        var response = call.ResponseStream.Current;

                        if (response.Message == "INVALID")
                        {
                            Console.WriteLine("\tInvalid Word, try again\n");
                            continue;
                        }
                        else
                        {
                            Console.WriteLine(response.Message);
                        }

                        if (guess.ToLower() == dailyWord.ToLower())
                        {
                            Console.WriteLine("\nYou win!");
                            return;
                        }
                    }
                    else
                    {
                        Console.WriteLine("ERROR: Server error.");
                        break;
                    }

                    guesses++;
                }

                // didnt guess it message
                Console.WriteLine("Game Over. The word was not guessed.");
                Console.WriteLine($"Todays Word: {dailyWord}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CLIENT ERROR: {ex.Message}");
            }
            finally
            {
                await call.RequestStream.CompleteAsync();
            }
        }

        public static void DisplayStart()
        {
            Console.WriteLine("+--------+");
            Console.WriteLine("| WORDLE |");
            Console.WriteLine("+--------+\n");
            Console.WriteLine("You have 6 chances to guess a 5-letter word");
            Console.WriteLine("Each guess must be a 'playable 5-letter word");
            Console.WriteLine("After a guess the game will display a series of");
            Console.WriteLine("characters to show you how good your guess was\n");
            Console.WriteLine("x - means the letter above is not in the word");
            Console.WriteLine("? - means the letter should be in another spot");
            Console.WriteLine("* - means the letter is correct in this spot\n");
            Console.WriteLine("Available: a,b,c,d,e,f,g,h,i,j,k,l,m,n,o,p,q,r,s,t,u,v,w,x,y,z\n");
        }
    }
}
