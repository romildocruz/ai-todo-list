using AiTodo.Domain.Todos;
using FluentValidation;

namespace AiTodo.Api.Validation;

/// <summary>
/// Reusable FluentValidation rules (messages in pt-BR) for HTTP requests. The limits come from
/// <see cref="TodoRules"/>; business invariants are enforced by the Domain regardless.
/// </summary>
public static class TodoValidationRules
{
    public static IRuleBuilderOptions<T, string?> ValidTitle<T>(this IRuleBuilder<T, string?> rule)
        => rule
            .Must(title => !string.IsNullOrWhiteSpace(title))
                .WithMessage("O título é obrigatório.")
            .Must(title => string.IsNullOrWhiteSpace(title) || TodoRules.IsValidTitle(TodoRules.NormalizeTitle(title)))
                .WithMessage($"O título deve ter no máximo {TodoRules.MaxTitleLength} caracteres.");

    public static IRuleBuilderOptions<T, string?> ValidDescription<T>(this IRuleBuilder<T, string?> rule)
        => rule
            .Must(description => TodoRules.IsValidDescription(TodoRules.NormalizeDescription(description)))
                .WithMessage($"A descrição deve ter no máximo {TodoRules.MaxDescriptionLength} caracteres.");

    /// <summary>The id must be a valid identifier (24 hexadecimal characters) before any database access.</summary>
    public static IRuleBuilderOptions<T, string?> ValidTodoId<T>(this IRuleBuilder<T, string?> rule)
        => rule.Must(id => TodoId.TryParse(id, out _))
            .WithMessage("O identificador informado é inválido.");
}
