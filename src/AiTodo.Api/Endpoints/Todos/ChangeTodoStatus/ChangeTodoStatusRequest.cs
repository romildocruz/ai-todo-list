using AiTodo.Domain.Todos;

namespace AiTodo.Api.Endpoints.Todos.ChangeTodoStatus;

public sealed class ChangeTodoStatusRequest
{
    /// <summary>Identificador da atividade (rota).</summary>
    public string? Id { get; set; }

    /// <summary>Novo status: ToDo, Doing ou Done. Nullable so that a missing value is rejected instead of defaulting to ToDo.</summary>
    public TodoStatus? Status { get; set; }
}
