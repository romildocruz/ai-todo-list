namespace AiTodo.Api.Endpoints.Todos.DeleteTodo;

public sealed class DeleteTodoRequest
{
    /// <summary>Identificador da atividade (24 caracteres hexadecimais).</summary>
    public string? Id { get; set; }
}
