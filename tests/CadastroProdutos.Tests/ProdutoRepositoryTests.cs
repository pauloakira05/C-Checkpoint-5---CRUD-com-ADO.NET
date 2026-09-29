using CadastroProdutos.Data;
using CadastroProdutos.Logging;
using CadastroProdutos.Models;
using Microsoft.Data.Sqlite;

namespace CadastroProdutos.Tests;

/// <summary>
/// Testes do ProdutoRepository usando um banco SQLite temporário
/// criado a partir do mesmo script .sql entregue no projeto.
/// </summary>
public class ProdutoRepositoryTests : IDisposable
{
    private readonly string _diretorio;
    private readonly string _connectionString;
    private readonly FileLogger _logger;
    private readonly ProdutoRepository _repository;

    public ProdutoRepositoryTests()
    {
        _diretorio = Path.Combine(Path.GetTempPath(), "cadastro-produtos-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_diretorio);

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(_diretorio, "teste.db"),
            Pooling = false
        }.ToString();

        _logger = new FileLogger(Path.Combine(_diretorio, "logs", "operacoes.log"));
        var script = Path.Combine(AppContext.BaseDirectory, "database", "criar_tabela_produtos.sql");
        DatabaseInitializer.Inicializar(_connectionString, script, _logger);

        _repository = new ProdutoRepository(_connectionString, _logger);
    }

    public void Dispose()
    {
        try { Directory.Delete(_diretorio, recursive: true); } catch (IOException) { }
    }

    private static Produto NovoProduto(string nome = "Caneta Azul", decimal preco = 2.50m, int estoque = 100, string categoria = "Papelaria") =>
        new() { Nome = nome, Preco = preco, Estoque = estoque, Categoria = categoria };

    [Fact]
    public void Inserir_DeveGerarIdEPersistirOsDados()
    {
        var id = _repository.Inserir(NovoProduto());

        Assert.True(id > 0);
        var salvo = _repository.BuscarPorId(id);
        Assert.NotNull(salvo);
        Assert.Equal("Caneta Azul", salvo!.Nome);
        Assert.Equal(2.50m, salvo.Preco);
        Assert.Equal(100, salvo.Estoque);
        Assert.Equal("Papelaria", salvo.Categoria);
        Assert.True(salvo.Ativo);
    }

    [Fact]
    public void Listar_DeveRetornarTodosOsProdutosAtivosOrdenadosPorId()
    {
        var id1 = _repository.Inserir(NovoProduto("Lápis"));
        var id2 = _repository.Inserir(NovoProduto("Borracha"));

        var produtos = _repository.Listar();

        Assert.Equal(new[] { id1, id2 }, produtos.Select(p => p.Id));
    }

    [Fact]
    public void BuscarPorId_Inexistente_DeveRetornarNull()
    {
        Assert.Null(_repository.BuscarPorId(12345));
    }

    [Fact]
    public void Atualizar_DeveAlterarOsCampos()
    {
        var id = _repository.Inserir(NovoProduto());
        var produto = _repository.BuscarPorId(id)!;
        produto.Nome = "Caneta Vermelha";
        produto.Preco = 3.75m;
        produto.Estoque = 50;
        produto.Categoria = "Escritório";

        Assert.True(_repository.Atualizar(produto));

        var atualizado = _repository.BuscarPorId(id)!;
        Assert.Equal("Caneta Vermelha", atualizado.Nome);
        Assert.Equal(3.75m, atualizado.Preco);
        Assert.Equal(50, atualizado.Estoque);
        Assert.Equal("Escritório", atualizado.Categoria);
    }

    [Fact]
    public void Atualizar_Inexistente_DeveRetornarFalse()
    {
        var produto = NovoProduto();
        produto.Id = 999;

        Assert.False(_repository.Atualizar(produto));
    }

    [Fact]
    public void Excluir_SoftDelete_DeveOcultarProdutoMasManterRegistroNoBanco()
    {
        var id = _repository.Inserir(NovoProduto());

        Assert.True(_repository.Excluir(id));

        Assert.Null(_repository.BuscarPorId(id));
        Assert.Empty(_repository.Listar());
        Assert.Equal(0L, ContarRegistros("SELECT Ativo FROM Produtos WHERE Id = " + id));
        Assert.Equal(1L, ContarRegistros("SELECT COUNT(*) FROM Produtos WHERE DataExclusao IS NOT NULL"));
    }

    [Fact]
    public void Excluir_JaExcluido_DeveRetornarFalse()
    {
        var id = _repository.Inserir(NovoProduto());
        _repository.Excluir(id);

        Assert.False(_repository.Excluir(id));
    }

    [Fact]
    public void Excluir_Definitivo_DeveRemoverRegistroFisicamente()
    {
        var id = _repository.Inserir(NovoProduto());

        Assert.True(_repository.Excluir(id, definitivo: true));

        Assert.Equal(0L, ContarRegistros("SELECT COUNT(*) FROM Produtos"));
    }

    [Fact]
    public void ComandosParametrizados_DevemImpedirSqlInjection()
    {
        const string nomeMalicioso = "X'); DROP TABLE Produtos; --";
        var id = _repository.Inserir(NovoProduto(nomeMalicioso));

        // A tabela continua existindo e o texto foi gravado literalmente.
        var salvo = _repository.BuscarPorId(id);
        Assert.Equal(nomeMalicioso, salvo!.Nome);
        Assert.Single(_repository.Listar());
    }

    [Theory]
    [InlineData("", 10, 1, "Cat")]
    [InlineData("Nome", -1, 1, "Cat")]
    [InlineData("Nome", 10, -1, "Cat")]
    [InlineData("Nome", 10, 1, " ")]
    public void Inserir_ComDadosInvalidos_DeveLancarArgumentException(string nome, decimal preco, int estoque, string categoria)
    {
        Assert.Throws<ArgumentException>(() => _repository.Inserir(NovoProduto(nome, preco, estoque, categoria)));
        Assert.Empty(_repository.Listar());
    }

    [Fact]
    public void ErroDoBanco_DeveSerConvertidoEmDatabaseExceptionERegistradoNoLog()
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "DROP TABLE Produtos;";
            command.ExecuteNonQuery();
        }

        var ex = Assert.Throws<DatabaseException>(() => _repository.Listar());
        Assert.IsType<SqliteException>(ex.InnerException);

        var log = File.ReadAllText(_logger.CaminhoArquivo);
        Assert.Contains("[ERRO] LISTAR", log);
    }

    [Fact]
    public void Operacoes_DevemSerRegistradasNoArquivoDeLog()
    {
        var id = _repository.Inserir(NovoProduto());
        _repository.Listar();
        _repository.BuscarPorId(id);
        _repository.Excluir(id);

        var log = File.ReadAllText(_logger.CaminhoArquivo);
        Assert.Contains("INSERIR", log);
        Assert.Contains("LISTAR", log);
        Assert.Contains("BUSCAR", log);
        Assert.Contains("EXCLUIR", log);
    }

    private long ContarRegistros(string sql)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        using var reader = command.ExecuteReader();
        reader.Read();
        return reader.GetInt64(0);
    }
}
