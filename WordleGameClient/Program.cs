using Grpc.Net.Client;
using WordleGameServer.Protos;

namespace WordleGameClient
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            var channel = GrpcChannel.ForAddress("https://localhost:7070");
            var client = new DailyWordle.DailyWordleClient(channel);

            // display the start message
            DisplayStart();

            // call Play()
            //using var play = client.Play();
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
