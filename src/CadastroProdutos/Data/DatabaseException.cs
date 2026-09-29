namespace CadastroProdutos.Data;

/// <summary>
/// Exceção lançada pela camada de acesso a dados com uma mensagem amigável,
/// preservando a exceção original do banco em InnerException.
/// </summary>
public class DatabaseException : Exception
{
    public DatabaseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
