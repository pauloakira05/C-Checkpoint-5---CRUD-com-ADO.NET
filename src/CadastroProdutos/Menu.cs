using System.Globalization;
using CadastroProdutos.Data;
using CadastroProdutos.Logging;
using CadastroProdutos.Models;

namespace CadastroProdutos;

/// <summary>
/// Interface de console com o menu de operações do cadastro de produtos.
/// </summary>
public class Menu
{
    private readonly ProdutoRepository _repository;
    private readonly FileLogger _logger;

    public Menu(ProdutoRepository repository, FileLogger logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public void Executar()
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine("        CADASTRO DE PRODUTOS");
            Console.WriteLine("========================================");
            Console.WriteLine(" 1 - Inserir produto");
            Console.WriteLine(" 2 - Listar produtos");
            Console.WriteLine(" 3 - Buscar produto por ID");
            Console.WriteLine(" 4 - Atualizar produto");
            Console.WriteLine(" 5 - Excluir produto");
            Console.WriteLine(" 6 - Sair");
            Console.WriteLine("========================================");
            Console.Write("Escolha uma opção: ");

            var opcao = Console.ReadLine();
            if (opcao is null) return; // fim da entrada padrão
            Console.WriteLine();

            try
            {
                switch (opcao.Trim())
                {
                    case "1": Inserir(); break;
                    case "2": Listar(); break;
                    case "3": Buscar(); break;
                    case "4": Atualizar(); break;
                    case "5": Excluir(); break;
                    case "6":
                        Console.WriteLine("Saindo... Até logo!");
                        return;
                    default:
                        Console.WriteLine("Opção inválida. Digite um número de 1 a 6.");
                        break;
                }
            }
            catch (DatabaseException ex)
            {
                Console.WriteLine($"[ERRO DE BANCO] {ex.Message}");
                Console.WriteLine($"Detalhes: {ex.InnerException?.Message}");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"[VALIDAÇÃO] {ex.Message}");
                _logger.Aviso($"Validação: {ex.Message}");
            }
            catch (EntradaCanceladaException)
            {
                return;
            }
        }
    }

    private void Inserir()
    {
        Console.WriteLine("--- Inserir produto ---");
        var produto = new Produto
        {
            Nome = LerTexto("Nome: "),
            Preco = LerDecimal("Preço (ex: 19,90): "),
            Estoque = LerInteiro("Estoque: ", minimo: 0),
            Categoria = LerTexto("Categoria: ")
        };

        var id = _repository.Inserir(produto);
        Console.WriteLine($"Produto inserido com sucesso! Id gerado: {id}");
    }

    private void Listar()
    {
        Console.WriteLine("--- Lista de produtos ---");
        var produtos = _repository.Listar();

        if (produtos.Count == 0)
        {
            Console.WriteLine("Nenhum produto cadastrado.");
            return;
        }

        Console.WriteLine($"{"ID",-5} {"NOME",-28} {"PREÇO",12} {"ESTOQUE",8}  {"CATEGORIA",-15}");
        Console.WriteLine(new string('-', 73));
        foreach (var p in produtos)
        {
            Console.WriteLine($"{p.Id,-5} {Truncar(p.Nome, 28),-28} {p.Preco.ToString("C2"),12} {p.Estoque,8}  {Truncar(p.Categoria, 15),-15}");
        }
        Console.WriteLine(new string('-', 73));
        Console.WriteLine($"Total: {produtos.Count} produto(s).");
    }

    private void Buscar()
    {
        Console.WriteLine("--- Buscar produto por ID ---");
        var id = LerInteiro("Informe o ID: ", minimo: 1);
        var produto = _repository.BuscarPorId(id);

        if (produto is null)
        {
            Console.WriteLine($"Produto com ID {id} não encontrado.");
            return;
        }

        ExibirDetalhes(produto);
    }

    private void Atualizar()
    {
        Console.WriteLine("--- Atualizar produto ---");
        var id = LerInteiro("Informe o ID do produto: ", minimo: 1);
        var produto = _repository.BuscarPorId(id);

        if (produto is null)
        {
            Console.WriteLine($"Produto com ID {id} não encontrado.");
            return;
        }

        ExibirDetalhes(produto);
        Console.WriteLine("Informe os novos valores (ENTER mantém o valor atual):");

        produto.Nome = LerTexto($"Nome [{produto.Nome}]: ", produto.Nome);
        produto.Preco = LerDecimal($"Preço [{produto.Preco:N2}]: ", produto.Preco);
        produto.Estoque = LerInteiro($"Estoque [{produto.Estoque}]: ", minimo: 0, valorAtual: produto.Estoque);
        produto.Categoria = LerTexto($"Categoria [{produto.Categoria}]: ", produto.Categoria);

        Console.WriteLine(_repository.Atualizar(produto)
            ? "Produto atualizado com sucesso!"
            : "Nenhum registro foi atualizado.");
    }

    private void Excluir()
    {
        Console.WriteLine("--- Excluir produto ---");
        var id = LerInteiro("Informe o ID do produto: ", minimo: 1);
        var produto = _repository.BuscarPorId(id);

        if (produto is null)
        {
            Console.WriteLine($"Produto com ID {id} não encontrado.");
            return;
        }

        ExibirDetalhes(produto);
        if (!Confirmar("Confirma a exclusão? (S/N): "))
        {
            Console.WriteLine("Exclusão cancelada.");
            return;
        }

        var definitivo = Confirmar("Excluir DEFINITIVAMENTE do banco? (S = DELETE físico / N = soft delete): ");
        Console.WriteLine(_repository.Excluir(id, definitivo)
            ? $"Produto excluído com sucesso ({(definitivo ? "exclusão definitiva" : "soft delete")})."
            : "Nenhum registro foi excluído.");
    }

    private static void ExibirDetalhes(Produto p)
    {
        Console.WriteLine();
        Console.WriteLine($"  ID ............: {p.Id}");
        Console.WriteLine($"  Nome ..........: {p.Nome}");
        Console.WriteLine($"  Preço .........: {p.Preco:C2}");
        Console.WriteLine($"  Estoque .......: {p.Estoque}");
        Console.WriteLine($"  Categoria .....: {p.Categoria}");
        Console.WriteLine($"  Cadastrado em .: {p.DataCadastro:dd/MM/yyyy HH:mm:ss}");
        Console.WriteLine();
    }

    // ---------------- Leitura e validação da entrada ----------------

    private static string LerLinha(string rotulo)
    {
        Console.Write(rotulo);
        var linha = Console.ReadLine() ?? throw new EntradaCanceladaException();
        return linha.Trim();
    }

    private static string LerTexto(string rotulo, string? valorAtual = null)
    {
        while (true)
        {
            var texto = LerLinha(rotulo);
            if (texto.Length == 0 && valorAtual is not null) return valorAtual;
            if (texto.Length > 0 && texto.Length <= 100) return texto;
            Console.WriteLine("  Valor inválido: informe um texto de 1 a 100 caracteres.");
        }
    }

    private static decimal LerDecimal(string rotulo, decimal? valorAtual = null)
    {
        const NumberStyles estilo = NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite;
        while (true)
        {
            var texto = LerLinha(rotulo);
            if (texto.Length == 0 && valorAtual.HasValue) return valorAtual.Value;

            // Aceita tanto "19,90" quanto "19.90".
            if ((decimal.TryParse(texto, estilo, new CultureInfo("pt-BR"), out var valor)
                 || decimal.TryParse(texto, estilo, CultureInfo.InvariantCulture, out valor))
                && valor >= 0)
            {
                return valor;
            }
            Console.WriteLine("  Valor inválido: informe um número maior ou igual a zero.");
        }
    }

    private static int LerInteiro(string rotulo, int minimo, int? valorAtual = null)
    {
        while (true)
        {
            var texto = LerLinha(rotulo);
            if (texto.Length == 0 && valorAtual.HasValue) return valorAtual.Value;
            if (int.TryParse(texto, out var valor) && valor >= minimo) return valor;
            Console.WriteLine($"  Valor inválido: informe um número inteiro maior ou igual a {minimo}.");
        }
    }

    private static bool Confirmar(string rotulo)
    {
        var resposta = LerLinha(rotulo).ToUpperInvariant();
        return resposta is "S" or "SIM";
    }

    private static string Truncar(string texto, int tamanho) =>
        texto.Length <= tamanho ? texto : texto[..(tamanho - 1)] + "…";

    private sealed class EntradaCanceladaException : Exception;
}
