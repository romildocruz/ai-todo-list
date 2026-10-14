namespace AiTodo.Api.Endpoints.Todos.UpdateTodo;

public sealed class UpdateTodoRequest
{
    /// <summary>Identificador da atividade (rota).</summary>
    public string? Id { get; set; }

    /// <summary>Obrigatório, até 100 caracteres.</summary>
    public string? Title { get; set; }

    /// <summary>Opcional, até 500 caracteres. Ausente ou vazia limpa a descrição.</summary>
    public string? Description { get; set; }
}
