namespace AiTodo.Api.Endpoints.Todos.GetTodos;

public sealed class GetTodosRequest
{
    /// <summary>Filtro opcional: ToDo, Doing ou Done.</summary>
    public string? Status { get; set; }

    /// <summary>Filtro opcional: trecho do título (sem diferenciar maiúsculas/minúsculas; texto literal).</summary>
    public string? Search { get; set; }
}
