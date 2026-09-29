using CadastroProdutos.Logging;
using Microsoft.Data.Sqlite;

namespace CadastroProdutos.Data;

/// <summary>
/// Executa o script .sql de criação da tabela (CREATE TABLE IF NOT EXISTS).
/// </summary>
public static class DatabaseInitializer
{
    public static void Inicializar(string connectionString, string caminhoScript, FileLogger logger)
    {
        if (!File.Exists(caminhoScript))
            throw new FileNotFoundException($"Script de criação não encontrado: {caminhoScript}");

        var script = File.ReadAllText(caminhoScript);

        try
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = script;
            command.ExecuteNonQuery();

            logger.Info($"Banco de dados inicializado com o script '{Path.GetFileName(caminhoScript)}'.");
        }
        catch (SqliteException ex)
        {
            logger.Erro("Falha ao inicializar o banco de dados.", ex);
            throw new DatabaseException("Não foi possível inicializar o banco de dados.", ex);
        }
    }
}
