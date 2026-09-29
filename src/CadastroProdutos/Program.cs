using System.Globalization;
using System.Text;
using CadastroProdutos;
using CadastroProdutos.Data;
using CadastroProdutos.Logging;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

Console.OutputEncoding = Encoding.UTF8;
CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("pt-BR");
CultureInfo.CurrentCulture = new CultureInfo("pt-BR");

var baseDir = AppContext.BaseDirectory;

IConfiguration configuration;
try
{
    configuration = new ConfigurationBuilder()
        .SetBasePath(baseDir)
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
        .Build();
}
catch (Exception ex) when (ex is FileNotFoundException or FormatException or InvalidDataException)
{
    Console.WriteLine($"Erro ao ler o appsettings.json: {ex.Message}");
    return 1;
}

var connectionString = configuration.GetConnectionString("ProdutosDb");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.WriteLine("Connection string 'ProdutosDb' não encontrada no appsettings.json.");
    return 1;
}

// Caminhos relativos são resolvidos a partir da pasta do executável.
connectionString = ResolverCaminhoBanco(connectionString, baseDir);
var caminhoLog = Path.Combine(baseDir, configuration["Logging:ArquivoLog"] ?? "logs/operacoes.log");
var caminhoScript = Path.Combine(baseDir, configuration["Database:ScriptCriacao"] ?? "database/criar_tabela_produtos.sql");

var logger = new FileLogger(caminhoLog);
logger.Info("Aplicação iniciada.");

try
{
    DatabaseInitializer.Inicializar(connectionString, caminhoScript, logger);
}
catch (Exception ex) when (ex is DatabaseException or FileNotFoundException)
{
    Console.WriteLine($"Erro: {ex.Message}");
    if (ex.InnerException is not null)
        Console.WriteLine($"Detalhes: {ex.InnerException.Message}");
    return 1;
}

var repository = new ProdutoRepository(connectionString, logger);
var menu = new Menu(repository, logger);
menu.Executar();

logger.Info("Aplicação encerrada.");
return 0;

static string ResolverCaminhoBanco(string connectionString, string baseDir)
{
    var builder = new SqliteConnectionStringBuilder(connectionString);
    if (!string.IsNullOrWhiteSpace(builder.DataSource)
        && builder.DataSource != ":memory:"
        && !Path.IsPathRooted(builder.DataSource))
    {
        builder.DataSource = Path.Combine(baseDir, builder.DataSource);
    }
    return builder.ToString();
}
