using WordServer.Protos;
using WordleGameServer.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();

builder.Services.AddGrpcClient<DailyWord.DailyWordClient>(o =>
{
    o.Address = new Uri("https://localhost:7206");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<GameService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
