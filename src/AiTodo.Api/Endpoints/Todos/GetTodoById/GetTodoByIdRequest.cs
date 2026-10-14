namespace AiTodo.Api.Endpoints.Todos.GetTodoById;

public sealed class GetTodoByIdRequest
{
    /// <summary>Identificador da atividade (24 caracteres hexadecimais).</summary>
    public string? Id { get; set; }
}
