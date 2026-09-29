using CadastroProdutos.Logging;
using CadastroProdutos.Models;
using Microsoft.Data.Sqlite;

namespace CadastroProdutos.Data;

/// <summary>
/// Responsável por todo o acesso ao banco de dados da tabela Produtos via ADO.NET.
/// Todas as instruções SQL são parametrizadas (prevenção contra SQL Injection).
/// </summary>
public class ProdutoRepository
{
    private readonly string _connectionString;
    private readonly FileLogger _logger;

    private const string ColunasSelect =
        "Id, Nome, Preco, Estoque, Categoria, Ativo, DataCadastro, DataExclusao";

    public ProdutoRepository(string connectionString, FileLogger logger)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("A connection string não pode ser vazia.", nameof(connectionString));

        _connectionString = connectionString;
        _logger = logger;
    }

    private SqliteConnection CriarConexao()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    /// <summary>
    /// Insere um novo produto e retorna o Id gerado.
    /// O INSERT e a leitura do Id ocorrem dentro da mesma transação.
    /// </summary>
    public int Inserir(Produto produto)
    {
        Validar(produto);

        try
        {
            using var connection = CriarConexao();
            using var transaction = connection.BeginTransaction();

            try
            {
                using (var insert = connection.CreateCommand())
                {
                    insert.Transaction = transaction;
                    insert.CommandText = @"
                        INSERT INTO Produtos (Nome, Preco, Estoque, Categoria)
                        VALUES (@Nome, @Preco, @Estoque, @Categoria);";
                    insert.Parameters.AddWithValue("@Nome", produto.Nome.Trim());
                    insert.Parameters.AddWithValue("@Preco", produto.Preco);
                    insert.Parameters.AddWithValue("@Estoque", produto.Estoque);
                    insert.Parameters.AddWithValue("@Categoria", produto.Categoria.Trim());
                    insert.ExecuteNonQuery();
                }

                int novoId;
                using (var select = connection.CreateCommand())
                {
                    select.Transaction = transaction;
                    select.CommandText = "SELECT last_insert_rowid();";
                    using var reader = select.ExecuteReader();
                    reader.Read();
                    novoId = Convert.ToInt32(reader.GetInt64(0));
                }

                transaction.Commit();

                produto.Id = novoId;
                _logger.Info($"INSERIR - Produto inserido: {produto}");
                return novoId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        catch (SqliteException ex)
        {
            _logger.Erro($"INSERIR - Falha ao inserir o produto '{produto.Nome}'.", ex);
            throw new DatabaseException("Erro ao inserir o produto no banco de dados.", ex);
        }
    }

    /// <summary>
    /// Lista os produtos ativos (não excluídos logicamente).
    /// </summary>
    public List<Produto> Listar()
    {
        try
        {
            using var connection = CriarConexao();
            using var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT {ColunasSelect}
                FROM Produtos
                WHERE Ativo = @Ativo
                ORDER BY Id;";
            command.Parameters.AddWithValue("@Ativo", 1);

            var produtos = new List<Produto>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                produtos.Add(Mapear(reader));
            }

            _logger.Info($"LISTAR - {produtos.Count} produto(s) retornado(s).");
            return produtos;
        }
        catch (SqliteException ex)
        {
            _logger.Erro("LISTAR - Falha ao listar os produtos.", ex);
            throw new DatabaseException("Erro ao listar os produtos.", ex);
        }
    }

    /// <summary>
    /// Busca um produto ativo pelo Id. Retorna null quando não encontrado.
    /// </summary>
    public Produto? BuscarPorId(int id)
    {
        try
        {
            using var connection = CriarConexao();
            using var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT {ColunasSelect}
                FROM Produtos
                WHERE Id = @Id AND Ativo = 1;";
            command.Parameters.AddWithValue("@Id", id);

            using var reader = command.ExecuteReader();
            var produto = reader.Read() ? Mapear(reader) : null;

            _logger.Info(produto is null
                ? $"BUSCAR - Produto Id {id} não encontrado."
                : $"BUSCAR - Produto encontrado: {produto}");
            return produto;
        }
        catch (SqliteException ex)
        {
            _logger.Erro($"BUSCAR - Falha ao buscar o produto Id {id}.", ex);
            throw new DatabaseException("Erro ao buscar o produto.", ex);
        }
    }

    /// <summary>
    /// Atualiza um produto ativo. Retorna true se algum registro foi alterado.
    /// </summary>
    public bool Atualizar(Produto produto)
    {
        Validar(produto);

        try
        {
            using var connection = CriarConexao();
            using var transaction = connection.BeginTransaction();

            try
            {
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = @"
                    UPDATE Produtos
                    SET Nome = @Nome,
                        Preco = @Preco,
                        Estoque = @Estoque,
                        Categoria = @Categoria
                    WHERE Id = @Id AND Ativo = 1;";
                command.Parameters.AddWithValue("@Nome", produto.Nome.Trim());
                command.Parameters.AddWithValue("@Preco", produto.Preco);
                command.Parameters.AddWithValue("@Estoque", produto.Estoque);
                command.Parameters.AddWithValue("@Categoria", produto.Categoria.Trim());
                command.Parameters.AddWithValue("@Id", produto.Id);

                var linhasAfetadas = command.ExecuteNonQuery();
                transaction.Commit();

                if (linhasAfetadas > 0)
                    _logger.Info($"ATUALIZAR - Produto atualizado: {produto}");
                else
                    _logger.Aviso($"ATUALIZAR - Produto Id {produto.Id} não encontrado.");

                return linhasAfetadas > 0;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        catch (SqliteException ex)
        {
            _logger.Erro($"ATUALIZAR - Falha ao atualizar o produto Id {produto.Id}.", ex);
            throw new DatabaseException("Erro ao atualizar o produto.", ex);
        }
    }

    /// <summary>
    /// Exclui um produto. Por padrão faz SOFT DELETE (marca Ativo = 0);
    /// com <paramref name="definitivo"/> = true executa um DELETE físico.
    /// Retorna true se algum registro foi afetado.
    /// </summary>
    public bool Excluir(int id, bool definitivo = false)
    {
        try
        {
            using var connection = CriarConexao();
            using var transaction = connection.BeginTransaction();

            try
            {
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = definitivo
                    ? "DELETE FROM Produtos WHERE Id = @Id;"
                    : @"UPDATE Produtos
                        SET Ativo = 0,
                            DataExclusao = @DataExclusao
                        WHERE Id = @Id AND Ativo = 1;";
                command.Parameters.AddWithValue("@Id", id);
                if (!definitivo)
                    command.Parameters.AddWithValue("@DataExclusao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                var linhasAfetadas = command.ExecuteNonQuery();
                transaction.Commit();

                var tipo = definitivo ? "definitiva" : "lógica (soft delete)";
                if (linhasAfetadas > 0)
                    _logger.Info($"EXCLUIR - Produto Id {id} excluído - exclusão {tipo}.");
                else
                    _logger.Aviso($"EXCLUIR - Produto Id {id} não encontrado - exclusão {tipo}.");

                return linhasAfetadas > 0;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        catch (SqliteException ex)
        {
            _logger.Erro($"EXCLUIR - Falha ao excluir o produto Id {id}.", ex);
            throw new DatabaseException("Erro ao excluir o produto.", ex);
        }
    }

    /// <summary>
    /// Mapeamento manual de uma linha do DataReader para um objeto Produto.
    /// </summary>
    private static Produto Mapear(SqliteDataReader reader)
    {
        var ordDataExclusao = reader.GetOrdinal("DataExclusao");

        return new Produto
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            Nome = reader.GetString(reader.GetOrdinal("Nome")),
            Preco = reader.GetDecimal(reader.GetOrdinal("Preco")),
            Estoque = reader.GetInt32(reader.GetOrdinal("Estoque")),
            Categoria = reader.GetString(reader.GetOrdinal("Categoria")),
            Ativo = reader.GetInt32(reader.GetOrdinal("Ativo")) == 1,
            DataCadastro = reader.GetDateTime(reader.GetOrdinal("DataCadastro")),
            DataExclusao = reader.IsDBNull(ordDataExclusao) ? null : reader.GetDateTime(ordDataExclusao)
        };
    }

    private static void Validar(Produto produto)
    {
        ArgumentNullException.ThrowIfNull(produto);

        if (string.IsNullOrWhiteSpace(produto.Nome))
            throw new ArgumentException("O nome do produto é obrigatório.");
        if (string.IsNullOrWhiteSpace(produto.Categoria))
            throw new ArgumentException("A categoria do produto é obrigatória.");
        if (produto.Preco < 0)
            throw new ArgumentException("O preço não pode ser negativo.");
        if (produto.Estoque < 0)
            throw new ArgumentException("O estoque não pode ser negativo.");
    }
}
