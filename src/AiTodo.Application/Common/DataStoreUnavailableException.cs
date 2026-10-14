namespace AiTodo.Application.Common;

/// <summary>
/// The data store could not be reached (timeout, refused connection, authentication failure...).
/// Infrastructure translates driver-specific exceptions into this one so that outer layers can react
/// (the API answers 503) without knowing which database technology is in use.
/// </summary>
public sealed class DataStoreUnavailableException(Exception innerException)
    : Exception("O armazenamento de dados está indisponível.", innerException);
