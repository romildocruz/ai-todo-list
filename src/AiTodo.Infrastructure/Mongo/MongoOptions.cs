using System.ComponentModel.DataAnnotations;

namespace AiTodo.Infrastructure.Mongo;

/// <summary>
/// MongoDB settings bound from the "Mongo" configuration section. The connection string
/// is never stored in source control: provide it via appsettings.Development.json (local,
/// git-ignored), user-secrets or the Mongo__ConnectionString environment variable.
/// </summary>
public sealed class MongoOptions
{
    public const string SectionName = "Mongo";

    [Required(AllowEmptyStrings = false,
        ErrorMessage = "A chave 'Mongo:ConnectionString' não está configurada. Defina-a em appsettings.Development.json, user-secrets ou na variável de ambiente Mongo__ConnectionString.")]
    public string ConnectionString { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "A chave 'Mongo:DatabaseName' não está configurada.")]
    public string DatabaseName { get; set; } = "AiTodoDb";

    [Required(AllowEmptyStrings = false, ErrorMessage = "A chave 'Mongo:CollectionName' não está configurada.")]
    public string CollectionName { get; set; } = "Todos";

    [Range(1, 120, ErrorMessage = "'Mongo:ServerSelectionTimeoutSeconds' deve estar entre 1 e 120.")]
    public int ServerSelectionTimeoutSeconds { get; set; } = 3;

    [Range(1, 120, ErrorMessage = "'Mongo:ConnectTimeoutSeconds' deve estar entre 1 e 120.")]
    public int ConnectTimeoutSeconds { get; set; } = 3;
}
