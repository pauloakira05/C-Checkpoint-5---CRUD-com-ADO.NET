using System.Text;

namespace CadastroProdutos.Logging;

/// <summary>
/// Registra em arquivo texto as operações realizadas pela aplicação.
/// </summary>
public class FileLogger
{
    private readonly string _caminhoArquivo;
    private static readonly object _lock = new();

    public FileLogger(string caminhoArquivo)
    {
        _caminhoArquivo = Path.GetFullPath(caminhoArquivo);
        var diretorio = Path.GetDirectoryName(_caminhoArquivo);
        if (!string.IsNullOrEmpty(diretorio))
            Directory.CreateDirectory(diretorio);
    }

    public string CaminhoArquivo => _caminhoArquivo;

    public void Info(string mensagem) => Escrever("INFO", mensagem);

    public void Aviso(string mensagem) => Escrever("AVISO", mensagem);

    public void Erro(string mensagem, Exception? ex = null)
    {
        var texto = ex is null ? mensagem : $"{mensagem} | {ex.GetType().Name}: {ex.Message}";
        Escrever("ERRO", texto);
    }

    private void Escrever(string nivel, string mensagem)
    {
        var linha = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{nivel}] {mensagem}{Environment.NewLine}";
        try
        {
            lock (_lock)
            {
                File.AppendAllText(_caminhoArquivo, linha, Encoding.UTF8);
            }
        }
        catch (IOException)
        {
            // Falha ao gravar o log não deve derrubar a aplicação.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
