using System.Collections.Concurrent;
using System.Net;
using AiTodo.Api.IntegrationTests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AiTodo.Api.IntegrationTests.Endpoints;

/// <summary>The global Pre/Post Processors log every endpoint execution (request received / completed).</summary>
public sealed class RequestLoggingTests : IClassFixture<ApiFactory>
{
    private readonly CapturingLoggerProvider _logs = new();
    private readonly HttpClient _client;

    public RequestLoggingTests(ApiFactory factory)
        => _client = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton<ILoggerProvider>(_logs))).CreateClient();

    [Fact]
    public async Task Request_logs_received_and_completed_with_status_code()
    {
        var response = await _client.GetAsync("/api/todos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(_logs.Messages, m => m.StartsWith("Requisição recebida: GET /api/todos"));
        Assert.Contains(_logs.Messages, m => m.StartsWith("Requisição concluída: GET /api/todos -> 200 em "));
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public ConcurrentQueue<string> Messages { get; } = new();

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(Messages);

        public void Dispose()
        {
        }

        private sealed class CapturingLogger(ConcurrentQueue<string> messages) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter)
                => messages.Enqueue(formatter(state, exception));
        }
    }
}
