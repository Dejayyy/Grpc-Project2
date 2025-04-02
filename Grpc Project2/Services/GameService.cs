using Grpc.Core;
using WordleGameServer.Protos;
using WordServer.Protos;

namespace WordleGameServer.Services
{
    public class GameService : DailyWordle.DailyWordleBase
    {
        private string StatsFilePath = "stats.json";
        private Mutex StatsMutex = new();

        //private DailyWord.DailyWordClient client;

        //public GameService(DailyWord.DailyWordleClient wordClient)
        //{
        //    client = wordleClient;
        //}

        public override Task Play(IAsyncStreamReader<GuessRequest> requestStream, IServerStreamWriter<GuessResponse> responseStream, ServerCallContext context)
        {
            return base.Play(requestStream, responseStream, context);
        }

        public override Task<GameStats> GetStats(Protos.Empty request, ServerCallContext context)
        {
            return base.GetStats(request, context);
        }
    }
}
