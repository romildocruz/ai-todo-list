namespace AiTodo.Api.Endpoints.Todos.CreateTodo;

public sealed class CreateTodoRequest
{
    /// <summary>Obrigatório, até 100 caracteres (espaços nas bordas são removidos).</summary>
    public string? Title { get; set; }

    /// <summary>Opcional, até 500 caracteres.</summary>
    public string? Description { get; set; }
}
