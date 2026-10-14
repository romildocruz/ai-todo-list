using System.Text.Json;
using System.Text.Json.Serialization;
using AiTodo.Api.Common;
using AiTodo.Api.Processors;
using AiTodo.Api.Serialization;
using AiTodo.Application.Features.Todos;
using AiTodo.Infrastructure;
using FastEndpoints;
using FastEndpoints.Swagger;

const string CorsPolicy = "WebClient";

var builder = WebApplication.CreateBuilder(args);

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
    options.AddPolicy(CorsPolicy, policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .WithExposedHeaders("Location")));

// Composition root: Infrastructure is referenced only here, to wire the ports used by Application.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddInfrastructure();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Endpoints live in this assembly; command/query handlers (FastEndpoints command bus) live in Application.
builder.Services.AddFastEndpoints(options => options.Assemblies =
[
    typeof(Program).Assembly,
    typeof(TodoResponse).Assembly
]);

if (builder.Environment.IsDevelopment())
{
    builder.Services.SwaggerDocument(options =>
    {
        // Documentation only: TodoStatus is read/written by its own strict converter at runtime,
        // which OpenAPI generation cannot see, so describe the enum as strings (ToDo/Doing/Done).
        options.SerializerSettings = serializer => serializer.Converters.Add(new JsonStringEnumConverter());

        options.DocumentSettings = settings =>
        {
            settings.Title = "AI Todo List API";
            settings.Version = "v1";
        };
    });
}

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors(CorsPolicy);
app.UseFastEndpoints(config =>
{
    config.Endpoints.RoutePrefix = "api";

    // Cross-cutting concern (request logging) as global Pre/Post Processors, not inside endpoints.
    config.Endpoints.Configurator = endpoint =>
    {
        endpoint.PreProcessor<RequestLoggingPreProcessor>(Order.Before);
        endpoint.PostProcessor<RequestLoggingPostProcessor>(Order.After);
    };

    // Validation and binding errors become application/problem+json with per-field errors.
    config.Errors.UseProblemDetails(problem => problem.ResponseBuilder = ApiProblemDetails.Build);

    // Status travels as "ToDo" | "Doing" | "Done" only (no numbers, no flag-style lists).
    config.Serializer.Options.Converters.Add(new TodoStatusJsonConverter());

    // A JSON content type with an empty body (typical of GET/DELETE sent by generic HTTP wrappers)
    // means "no body": bind route/query values only instead of failing with a deserialization error.
    var jsonOptions = config.Serializer.Options;
    config.Serializer.RequestDeserializer = async (request, requestType, _, cancellationToken) =>
        await RequestBody.IsEmptyAsync(request, cancellationToken)
            ? Activator.CreateInstance(requestType)
            : await JsonSerializer.DeserializeAsync(request.Body, requestType, jsonOptions, cancellationToken);
});

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerGen();
}

app.Run();

public partial class Program;
