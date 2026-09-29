namespace CadastroProdutos.Models;

/// <summary>
/// Representa um registro da tabela Produtos.
/// </summary>
public class Produto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public decimal Preco { get; set; }
    public int Estoque { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
    public DateTime DataCadastro { get; set; }
    public DateTime? DataExclusao { get; set; }

    public override string ToString() =>
        $"#{Id} | {Nome} | R$ {Preco:N2} | Estoque: {Estoque} | Categoria: {Categoria}";
}
