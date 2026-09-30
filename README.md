# Checkpoint 5 — Cadastro de Produtos (CRUD com ADO.NET)

Aplicação console desenvolvida em **C# (.NET 8)** para cadastrar e gerenciar produtos.
O acesso ao banco de dados é feito com **ADO.NET puro** (`Microsoft.Data.Sqlite`), sem ORM,
usando um banco **SQLite**.

## 👤 Responsável

| Nome | RM |
|---|---|
| Paulo Akira Okama | RM556840 |

---

## 📑 Sumário

1. [Objetivo](#-objetivo)
2. [Tecnologias utilizadas](#-tecnologias-utilizadas)
3. [Estrutura do projeto](#-estrutura-do-projeto)
4. [Banco de dados e script SQL](#-banco-de-dados-e-script-sql)
5. [Como configurar e executar](#-como-configurar-e-executar)
6. [Menu da aplicação](#-menu-da-aplicação)
7. [Arquitetura e classes](#-arquitetura-e-classes)
8. [Requisitos técnicos — como cada um foi atendido](#-requisitos-técnicos--como-cada-um-foi-atendido)
9. [Prevenção contra SQL Injection](#-prevenção-contra-sql-injection)
10. [Tratamento de exceções](#-tratamento-de-exceções)
11. [Registro de operações em arquivo (log)](#-registro-de-operações-em-arquivo-log)
12. [⭐ Bônus](#-bônus)
    - [Bônus 1 — Transações](#bônus-1--transações)
    - [Bônus 2 — Soft delete (exclusão lógica)](#bônus-2--soft-delete-exclusão-lógica)
    - [Bônus 3 — Testes unitários](#bônus-3--testes-unitários)
13. [Prints das operações](#-prints-das-operações)

---

## 🎯 Objetivo

A aplicação demonstra:

- **Conexão com o banco de dados** usando ADO.NET (`SqliteConnection`, `SqliteCommand`, `SqliteDataReader`);
- **Operações CRUD**: inserir, listar, buscar por ID, atualizar e excluir produtos;
- **Prevenção contra SQL Injection** com comandos parametrizados;
- **Tratamento de exceções** relacionadas ao banco de dados;
- **Registro em arquivo** de todas as operações realizadas.

---

## 🛠 Tecnologias utilizadas

| Tecnologia | Uso |
|---|---|
| C# / .NET 8 | Linguagem e plataforma da aplicação |
| ADO.NET (`Microsoft.Data.Sqlite` 8.0) | Conexão e execução de comandos SQL |
| SQLite | Banco de dados (arquivo local `produtos.db`) |
| `Microsoft.Extensions.Configuration.Json` | Leitura do `appsettings.json` |
| xUnit | Testes unitários (bônus) |

> **Por que SQLite?** O enunciado permite SQL Server, LocalDB ou SQLite. O SQLite foi escolhido
> porque é embutido no próprio pacote NuGet: **não é preciso instalar nenhum servidor de banco**,
> o projeto roda em Windows, Linux ou macOS apenas com o .NET SDK.

---

## 📁 Estrutura do projeto

```
C-Checkpoint-5---CRUD-com-ADO.NET/
├── CadastroProdutos.sln                   # Solução (aplicação + testes)
├── README.md
├── database/
│   └── criar_tabela_produtos.sql          # Script de criação da tabela Produtos
├── docs/
│   └── prints/                            # Prints das operações funcionando
├── src/
│   └── CadastroProdutos/                  # Aplicação console
│       ├── appsettings.json               # Connection string e configurações
│       ├── Program.cs                     # Ponto de entrada: configuração e inicialização
│       ├── Menu.cs                        # Menu interativo e leitura/validação da entrada
│       ├── Models/
│       │   └── Produto.cs                 # Classe Produto (entidade)
│       ├── Data/
│       │   ├── ProdutoRepository.cs       # Acesso ao banco (CRUD com ADO.NET)
│       │   ├── DatabaseInitializer.cs     # Executa o script .sql ao iniciar
│       │   └── DatabaseException.cs       # Exceção personalizada da camada de dados
│       └── Logging/
│           └── FileLogger.cs              # Registro das operações em arquivo
└── tests/
    └── CadastroProdutos.Tests/            # Testes unitários (xUnit)
        └── ProdutoRepositoryTests.cs
```

---

## 🗄 Banco de dados e script SQL

O script está em [`database/criar_tabela_produtos.sql`](database/criar_tabela_produtos.sql):

```sql
CREATE TABLE IF NOT EXISTS Produtos (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    Nome          TEXT    NOT NULL CHECK (length(trim(Nome)) > 0),
    Preco         NUMERIC NOT NULL CHECK (Preco >= 0),
    Estoque       INTEGER NOT NULL CHECK (Estoque >= 0),
    Categoria     TEXT    NOT NULL CHECK (length(trim(Categoria)) > 0),
    Ativo         INTEGER NOT NULL DEFAULT 1 CHECK (Ativo IN (0, 1)),
    DataCadastro  TEXT    NOT NULL DEFAULT (datetime('now', 'localtime')),
    DataExclusao  TEXT    NULL
);

CREATE INDEX IF NOT EXISTS IX_Produtos_Ativo ON Produtos (Ativo);
```

| Coluna | Tipo | Descrição |
|---|---|---|
| `Id` | INTEGER | Chave primária, gerada automaticamente |
| `Nome` | TEXT | Nome do produto (obrigatório, não pode ser vazio) |
| `Preco` | NUMERIC | Preço (não pode ser negativo) |
| `Estoque` | INTEGER | Quantidade em estoque (não pode ser negativa) |
| `Categoria` | TEXT | Categoria do produto (obrigatória) |
| `Ativo` | INTEGER | **Soft delete**: `1` = ativo, `0` = excluído logicamente |
| `DataCadastro` | TEXT | Data/hora do cadastro, preenchida automaticamente |
| `DataExclusao` | TEXT | Data/hora da exclusão lógica (`NULL` enquanto ativo) |

Os campos exigidos pelo enunciado são **Id, Nome, Preco, Estoque e Categoria**. As colunas
`Ativo`, `DataCadastro` e `DataExclusao` foram adicionadas para o bônus de **soft delete**.
As restrições `CHECK` garantem a integridade dos dados também no nível do banco.

**O script é executado automaticamente** pela aplicação na inicialização (classe
`DatabaseInitializer`). Como usa `CREATE TABLE IF NOT EXISTS`, pode ser executado várias
vezes sem apagar os dados.

---

## 🚀 Como configurar e executar

### Pré-requisitos

- [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0) ou superior.
- Nenhum servidor de banco é necessário (o SQLite vem no pacote NuGet).

Para conferir se o .NET está instalado:

```bash
dotnet --version
```

### 1. Clonar o repositório

```bash
git clone https://github.com/pauloakira05/C-Checkpoint-5---CRUD-com-ADO.NET.git
cd C-Checkpoint-5---CRUD-com-ADO.NET
```

### 2. Restaurar os pacotes

```bash
dotnet restore
```

### 3. Executar a aplicação

```bash
dotnet run --project src/CadastroProdutos
```

Na primeira execução a aplicação:

1. lê o `appsettings.json`;
2. cria o arquivo de banco `produtos.db`;
3. executa o script `criar_tabela_produtos.sql`;
4. cria o arquivo de log `logs/operacoes.log`;
5. exibe o menu.

> O banco (`produtos.db`) e o log (`logs/operacoes.log`) ficam na **pasta de saída do executável**,
> por exemplo: `src/CadastroProdutos/bin/Debug/net8.0/`.

Também é possível abrir o arquivo `CadastroProdutos.sln` no **Visual Studio** ou **Rider** e
executar o projeto `CadastroProdutos` (F5).

### 4. Executar os testes (bônus)

```bash
dotnet test
```

### Configuração — `appsettings.json`

A **connection string fica no `appsettings.json`**, e não no código:

```json
{
  "ConnectionStrings": {
    "ProdutosDb": "Data Source=produtos.db"
  },
  "Logging": {
    "ArquivoLog": "logs/operacoes.log"
  },
  "Database": {
    "ScriptCriacao": "database/criar_tabela_produtos.sql"
  }
}
```

| Chave | Descrição |
|---|---|
| `ConnectionStrings:ProdutosDb` | Connection string do SQLite. Para usar outro arquivo, altere o `Data Source` (caminho relativo à pasta do executável ou absoluto). |
| `Logging:ArquivoLog` | Caminho do arquivo de log das operações. |
| `Database:ScriptCriacao` | Caminho do script `.sql` executado na inicialização. |

### (Opcional) Criar a tabela manualmente

Se tiver o `sqlite3` instalado, é possível criar a tabela sem a aplicação:

```bash
sqlite3 produtos.db < database/criar_tabela_produtos.sql
```

---

## 📋 Menu da aplicação

```
========================================
        CADASTRO DE PRODUTOS
========================================
 1 - Inserir produto
 2 - Listar produtos
 3 - Buscar produto por ID
 4 - Atualizar produto
 5 - Excluir produto
 6 - Sair
========================================
Escolha uma opção:
```

| Opção | O que faz |
|---|---|
| **1 - Inserir** | Pede nome, preço, estoque e categoria, grava no banco e mostra o Id gerado. |
| **2 - Listar** | Mostra em tabela todos os produtos ativos, ordenados por Id. |
| **3 - Buscar por ID** | Mostra os detalhes de um produto ou avisa que não foi encontrado. |
| **4 - Atualizar** | Busca o produto, mostra os valores atuais e pede os novos. **ENTER mantém o valor atual** do campo. |
| **5 - Excluir** | Busca o produto, pede confirmação e pergunta se a exclusão é lógica (soft delete, padrão) ou definitiva. |
| **6 - Sair** | Encerra a aplicação. |

**Validação da entrada:** o preço aceita vírgula ou ponto (`19,90` ou `19.90`). Se o usuário
digitar texto onde se espera número, ou um valor negativo, a aplicação mostra uma mensagem e
pede o valor novamente, sem encerrar.

---

## 🧱 Arquitetura e classes

A aplicação é dividida em camadas com responsabilidades separadas:

```
 Program.cs ──► lê appsettings.json, cria logger, inicializa o banco
     │
     ▼
 Menu.cs ─────► interage com o usuário (entrada/saída no console)
     │
     ▼
 ProdutoRepository.cs ──► único ponto de acesso ao banco (ADO.NET)
     │                         │
     ▼                         ▼
 Produto.cs (entidade)     FileLogger.cs (log em arquivo)
```

### `Produto` — [`Models/Produto.cs`](src/CadastroProdutos/Models/Produto.cs)

Representa um registro da tabela `Produtos`:

```csharp
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
}
```

### `ProdutoRepository` — [`Data/ProdutoRepository.cs`](src/CadastroProdutos/Data/ProdutoRepository.cs)

Classe responsável por **todo o acesso ao banco**. Nenhuma outra parte da aplicação escreve SQL.

| Método | SQL | Comando ADO.NET | Retorno |
|---|---|---|---|
| `Inserir(Produto)` | `INSERT` + `SELECT last_insert_rowid()` | `ExecuteNonQuery` + `ExecuteReader` | Id gerado (`int`) |
| `Listar()` | `SELECT ... WHERE Ativo = 1` | `ExecuteReader` | `List<Produto>` |
| `BuscarPorId(int)` | `SELECT ... WHERE Id = @Id AND Ativo = 1` | `ExecuteReader` | `Produto?` (`null` se não existir) |
| `Atualizar(Produto)` | `UPDATE` | `ExecuteNonQuery` | `true` se atualizou |
| `Excluir(int, bool definitivo = false)` | `UPDATE` (soft delete) ou `DELETE` | `ExecuteNonQuery` | `true` se excluiu |

Antes de inserir ou atualizar, o repositório valida os dados (nome e categoria obrigatórios,
preço e estoque não negativos) e lança `ArgumentException` se algo estiver inválido.

### Outras classes

| Classe | Responsabilidade |
|---|---|
| `DatabaseInitializer` | Lê o arquivo `.sql` e o executa com `ExecuteNonQuery` para criar a tabela. |
| `DatabaseException` | Exceção da camada de dados, com mensagem amigável e a exceção original em `InnerException`. |
| `FileLogger` | Grava as operações em arquivo texto, com data/hora e nível (`INFO`, `AVISO`, `ERRO`). |
| `Menu` | Exibe o menu, lê e valida a entrada do usuário e chama o repositório. |

---

## ✅ Requisitos técnicos — como cada um foi atendido

| Requisito | Status | Como foi feito |
|---|:---:|---|
| Utilizar SQL Server, LocalDB ou SQLite | ✅ | SQLite via `Microsoft.Data.Sqlite` |
| Criar e entregar o script `.sql` | ✅ | `database/criar_tabela_produtos.sql` |
| Connection string no `appsettings.json` | ✅ | Chave `ConnectionStrings:ProdutosDb`, lida com `GetConnectionString` |
| Criar a classe `Produto` | ✅ | `Models/Produto.cs` |
| Criar a classe `ProdutoRepository` | ✅ | `Data/ProdutoRepository.cs` |
| Métodos `Inserir`, `Listar`, `BuscarPorId`, `Atualizar`, `Excluir` | ✅ | Todos implementados no repositório |
| `ExecuteNonQuery` em INSERT, UPDATE e DELETE | ✅ | `Inserir`, `Atualizar`, `Excluir` |
| `ExecuteReader` nas consultas SELECT | ✅ | `Listar`, `BuscarPorId` e leitura do Id gerado em `Inserir` |
| Mapeamento manual DataReader → `Produto` | ✅ | Método privado `Mapear(SqliteDataReader)` |
| Comandos SQL parametrizados em todas as operações | ✅ | Todos os valores passam por `Parameters.AddWithValue("@...")` |
| Tratar exceções relacionadas ao banco | ✅ | `SqliteException` → log + `DatabaseException` → mensagem no menu |
| Registrar em arquivo as operações | ✅ | `FileLogger` → `logs/operacoes.log` |

### Exemplo: `ExecuteNonQuery` (UPDATE)

```csharp
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
```

`ExecuteNonQuery` retorna o número de linhas afetadas. Se for `0`, o produto não existe
(ou já foi excluído) e o método retorna `false`.

### Exemplo: `ExecuteReader` (SELECT)

```csharp
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
```

### Mapeamento manual do DataReader

Cada coluna é lida pelo nome (`GetOrdinal`) e convertida para o tipo correto da propriedade.
Colunas que podem ser nulas são verificadas com `IsDBNull`:

```csharp
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
```

### Uso correto dos recursos (`using`)

Conexões, comandos, readers e transações são criados com `using`. Assim, são fechados e
liberados automaticamente ao final de cada operação, mesmo se ocorrer uma exceção.

---

## 🔒 Prevenção contra SQL Injection

**Nenhum valor digitado pelo usuário é concatenado na string SQL.** Todos os valores são enviados
como **parâmetros** (`@Id`, `@Nome`, `@Preco`...). O banco trata o parâmetro sempre como **dado**,
nunca como parte do comando.

❌ **Forma vulnerável (NÃO usada no projeto):**

```csharp
command.CommandText = "SELECT * FROM Produtos WHERE Nome = '" + nome + "'";
```

✅ **Forma segura (usada em todo o projeto):**

```csharp
command.CommandText = "SELECT ... FROM Produtos WHERE Id = @Id AND Ativo = 1;";
command.Parameters.AddWithValue("@Id", id);
```

**Demonstração:** foi cadastrado um produto com o nome malicioso
`X'); DROP TABLE Produtos; --`. Com concatenação, isso poderia apagar a tabela. Com os comandos
parametrizados, o texto é gravado **literalmente** como nome do produto e a tabela continua
intacta (veja o [print 6](#6-tentativa-de-sql-injection-bloqueada)). Há também um teste
unitário automatizado que comprova isso (`ComandosParametrizados_DevemImpedirSqlInjection`).

---

## ⚠️ Tratamento de exceções

O tratamento é feito em camadas:

**1. No repositório** — cada método captura `SqliteException` (erro do banco: tabela inexistente,
banco bloqueado, violação de restrição etc.), **registra o erro no log** e lança uma
`DatabaseException` com uma mensagem amigável, preservando a exceção original:

```csharp
catch (SqliteException ex)
{
    _logger.Erro("LISTAR - Falha ao listar os produtos.", ex);
    throw new DatabaseException("Erro ao listar os produtos.", ex);
}
```

**2. No menu** — o `Menu` captura as exceções, mostra a mensagem ao usuário e **continua
funcionando**, sem encerrar a aplicação:

```csharp
catch (DatabaseException ex)
{
    Console.WriteLine($"[ERRO DE BANCO] {ex.Message}");
    Console.WriteLine($"Detalhes: {ex.InnerException?.Message}");
}
catch (ArgumentException ex)
{
    Console.WriteLine($"[VALIDAÇÃO] {ex.Message}");
}
```

**3. Na inicialização** — o `Program.cs` trata a ausência ou erro no `appsettings.json`,
a falta da connection string, a ausência do script `.sql` e falhas ao criar a tabela,
mostrando uma mensagem clara e encerrando com código de erro.

**4. Na entrada de dados** — valores inválidos digitados (texto no lugar de número, preço ou
estoque negativos, nome vazio) são rejeitados e o valor é pedido novamente.

**5. No logger** — se não for possível gravar o arquivo de log (por exemplo, falta de permissão),
o erro é ignorado para não derrubar a aplicação.

---

## 📝 Registro de operações em arquivo (log)

A classe `FileLogger` grava **todas as operações** no arquivo `logs/operacoes.log`
(caminho configurável no `appsettings.json`). Cada linha contém data/hora, nível e descrição:

| Nível | Quando é usado |
|---|---|
| `INFO` | Operação realizada com sucesso (inserção, listagem, busca, atualização, exclusão, início/fim da aplicação) |
| `AVISO` | Operação sem efeito (produto não encontrado para atualizar/excluir) ou erro de validação |
| `ERRO` | Falha no banco de dados, com o tipo e a mensagem da exceção |

Trecho real do log gerado durante os testes:

```
[2026-09-29 23:02:37] [INFO] Aplicação iniciada.
[2026-09-29 23:02:39] [INFO] INSERIR - Produto inserido: #1 | Teclado Mecânico | R$ 249,90 | Estoque: 15 | Categoria: Periféricos
[2026-09-29 23:02:46] [INFO] ATUALIZAR - Produto atualizado: #2 | Mouse Gamer | R$ 119,90 | Estoque: 35 | Categoria: Periféricos
[2026-09-29 23:02:48] [INFO] EXCLUIR - Produto Id 4 excluído - exclusão lógica (soft delete).
[2026-09-29 23:02:48] [INFO] BUSCAR - Produto Id 4 não encontrado.
```

A gravação usa `lock` para evitar conflito caso duas operações escrevam ao mesmo tempo.

---

## ⭐ Bônus

Os **três bônus** sugeridos no enunciado foram implementados: **transações**, **soft delete** e
**testes unitários**.

### Bônus 1 — Transações

#### O que é

Uma **transação** agrupa um ou mais comandos SQL em uma unidade de trabalho **"tudo ou nada"**:

- se tudo der certo → `Commit()` confirma as alterações no banco;
- se algo falhar → `Rollback()` desfaz tudo o que foi feito dentro da transação.

Isso garante que o banco **nunca fique em um estado inconsistente** (por exemplo, um produto
gravado pela metade).

#### Onde foi usado

As três operações que alteram dados — **`Inserir`**, **`Atualizar`** e **`Excluir`** — rodam
dentro de uma transação.

O caso mais importante é o **`Inserir`**, que executa **dois comandos** que precisam acontecer juntos:

1. o `INSERT` do produto (com `ExecuteNonQuery`);
2. o `SELECT last_insert_rowid()` para obter o Id gerado (com `ExecuteReader`).

Como os dois estão na mesma transação e na mesma conexão, o Id lido é garantidamente o do
produto que acabou de ser inserido. Se qualquer um dos dois falhar, o `INSERT` é desfeito.

#### Como foi implementado

```csharp
using var connection = CriarConexao();
using var transaction = connection.BeginTransaction();   // 1. inicia a transação

try
{
    using (var insert = connection.CreateCommand())
    {
        insert.Transaction = transaction;                  // 2. comando ligado à transação
        insert.CommandText = @"
            INSERT INTO Produtos (Nome, Preco, Estoque, Categoria)
            VALUES (@Nome, @Preco, @Estoque, @Categoria);";
        // ... parâmetros ...
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

    transaction.Commit();                                  // 3. tudo certo: confirma
    return novoId;
}
catch
{
    transaction.Rollback();                                // 4. deu erro: desfaz tudo
    throw;                                                 //    e repassa o erro
}
```

Passo a passo:

1. `BeginTransaction()` abre a transação na conexão;
2. cada comando recebe `command.Transaction = transaction`, para fazer parte dela;
3. se todos os comandos executarem sem erro, `Commit()` grava definitivamente;
4. se qualquer comando lançar exceção, o `catch` chama `Rollback()`, que desfaz as alterações,
   e o `throw` repassa o erro para ser registrado no log e exibido ao usuário.

`Atualizar` e `Excluir` seguem o mesmo padrão (`BeginTransaction` → `ExecuteNonQuery` →
`Commit`, ou `Rollback` em caso de erro).

---

### Bônus 2 — Soft delete (exclusão lógica)

#### O que é

No **hard delete** (exclusão física), o comando `DELETE` apaga o registro do banco para sempre.

No **soft delete** (exclusão lógica), o registro **não é apagado**: ele apenas é **marcado como
excluído**. Para a aplicação o produto "some", mas os dados continuam no banco.

**Vantagens:**

- permite **recuperar** um produto excluído por engano;
- mantém o **histórico** (auditoria: o que foi excluído e quando);
- preserva a integridade caso outras tabelas (ex.: vendas) façam referência ao produto.

#### Como foi implementado

**1. Colunas na tabela** — o script SQL tem duas colunas para isso:

```sql
Ativo         INTEGER NOT NULL DEFAULT 1 CHECK (Ativo IN (0, 1)),
DataExclusao  TEXT    NULL
```

- `Ativo = 1` → produto ativo (valor padrão ao inserir);
- `Ativo = 0` → produto excluído logicamente;
- `DataExclusao` → data e hora em que foi excluído.

Também foi criado um índice na coluna `Ativo` (`IX_Produtos_Ativo`), pois todas as consultas
filtram por ela.

**2. Exclusão = `UPDATE`, não `DELETE`** — por padrão, o método `Excluir` faz um `UPDATE`:

```csharp
UPDATE Produtos
SET Ativo = 0,
    DataExclusao = @DataExclusao
WHERE Id = @Id AND Ativo = 1;
```

**3. Consultas ignoram os excluídos** — `Listar`, `BuscarPorId` e `Atualizar` filtram por
`Ativo = 1`. Assim, um produto excluído não aparece na listagem, não é encontrado na busca e
não pode ser atualizado:

```sql
SELECT ... FROM Produtos WHERE Id = @Id AND Ativo = 1;
```

**4. Exclusão definitiva continua disponível** — o método aceita um parâmetro opcional:

```csharp
public bool Excluir(int id, bool definitivo = false)
```

- `Excluir(id)` → **soft delete** (`UPDATE ... SET Ativo = 0`);
- `Excluir(id, definitivo: true)` → **hard delete** (`DELETE FROM Produtos WHERE Id = @Id`).

No menu (opção 5), após confirmar a exclusão, o usuário escolhe:

```
Confirma a exclusão? (S/N): S
Excluir DEFINITIVAMENTE do banco? (S = DELETE físico / N = soft delete): N
Produto excluído com sucesso (soft delete).
```

Assim o requisito de usar `ExecuteNonQuery` com `DELETE` continua atendido, e o soft delete
é o comportamento padrão.

**5. Como ver/recuperar os produtos excluídos** — direto no banco:

```sql
-- Ver os produtos excluídos logicamente
SELECT * FROM Produtos WHERE Ativo = 0;

-- Recuperar um produto excluído
UPDATE Produtos SET Ativo = 1, DataExclusao = NULL WHERE Id = 4;
```

**Demonstração:** no [print 5](#5-excluir-produto-soft-delete), o produto de Id 4 é excluído
com soft delete, desaparece da listagem e a busca pelo Id 4 retorna "não encontrado" — mas o
registro continua na tabela com `Ativo = 0`.

---

### Bônus 3 — Testes unitários

#### O que foi feito

Foi criado o projeto **`tests/CadastroProdutos.Tests`** com **xUnit**, contendo **15 testes**
que verificam automaticamente o funcionamento do `ProdutoRepository`.

#### Como os testes funcionam

- Antes de **cada** teste, é criado um **banco SQLite temporário** em uma pasta nova, e a tabela
  é criada executando o **mesmo script `.sql`** entregue no projeto. Assim, o teste valida também o script.
- Cada teste roda **isolado**, com o banco vazio, sem interferir nos outros nem no banco real da aplicação.
- Ao final de cada teste, a pasta temporária é apagada (`IDisposable.Dispose`).
- Os testes seguem o padrão **Arrange / Act / Assert** (preparar, executar, verificar).

```csharp
public ProdutoRepositoryTests()
{
    _diretorio = Path.Combine(Path.GetTempPath(), "cadastro-produtos-tests-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(_diretorio);
    // ... connection string apontando para o banco temporário ...
    DatabaseInitializer.Inicializar(_connectionString, script, _logger);
    _repository = new ProdutoRepository(_connectionString, _logger);
}
```

#### Testes implementados

| # | Teste | O que verifica |
|---|---|---|
| 1 | `Inserir_DeveGerarIdEPersistirOsDados` | O `Inserir` gera um Id e todos os campos são gravados corretamente |
| 2 | `Listar_DeveRetornarTodosOsProdutosAtivosOrdenadosPorId` | A listagem retorna os produtos ativos em ordem de Id |
| 3 | `BuscarPorId_Inexistente_DeveRetornarNull` | Buscar um Id que não existe retorna `null` |
| 4 | `Atualizar_DeveAlterarOsCampos` | O `Atualizar` altera nome, preço, estoque e categoria |
| 5 | `Atualizar_Inexistente_DeveRetornarFalse` | Atualizar um produto que não existe retorna `false` |
| 6 | `Excluir_SoftDelete_DeveOcultarProdutoMasManterRegistroNoBanco` | **Soft delete**: o produto some da busca/listagem, mas o registro continua no banco com `Ativo = 0` e `DataExclusao` preenchida |
| 7 | `Excluir_JaExcluido_DeveRetornarFalse` | Excluir de novo um produto já excluído retorna `false` |
| 8 | `Excluir_Definitivo_DeveRemoverRegistroFisicamente` | **Hard delete**: com `definitivo: true` o registro é apagado da tabela |
| 9 | `ComandosParametrizados_DevemImpedirSqlInjection` | O nome `X'); DROP TABLE Produtos; --` é gravado como texto e a tabela continua existindo |
| 10–13 | `Inserir_ComDadosInvalidos_DeveLancarArgumentException` (4 casos) | Nome vazio, preço negativo, estoque negativo e categoria vazia são rejeitados e nada é gravado |
| 14 | `ErroDoBanco_DeveSerConvertidoEmDatabaseExceptionERegistradoNoLog` | Apaga a tabela de propósito e verifica que o erro vira `DatabaseException` (com a `SqliteException` original) e é registrado no log como `[ERRO]` |
| 15 | `Operacoes_DevemSerRegistradasNoArquivoDeLog` | As operações INSERIR, LISTAR, BUSCAR e EXCLUIR aparecem no arquivo de log |

Exemplo de teste (soft delete):

```csharp
[Fact]
public void Excluir_SoftDelete_DeveOcultarProdutoMasManterRegistroNoBanco()
{
    // Arrange
    var id = _repository.Inserir(NovoProduto());

    // Act
    Assert.True(_repository.Excluir(id));

    // Assert
    Assert.Null(_repository.BuscarPorId(id));          // não é mais encontrado
    Assert.Empty(_repository.Listar());                // não aparece na listagem
    Assert.Equal(0L, ContarRegistros("SELECT Ativo FROM Produtos WHERE Id = " + id));   // Ativo = 0
    Assert.Equal(1L, ContarRegistros("SELECT COUNT(*) FROM Produtos WHERE DataExclusao IS NOT NULL"));
}
```

#### Como executar

Na raiz do repositório:

```bash
dotnet test
```

Para ver o nome de cada teste:

```bash
dotnet test --logger "console;verbosity=normal"
```

Resultado:

```
Test Run Successful.
Total tests: 15
     Passed: 15
```

---

## 📸 Prints das operações

Os prints abaixo foram gerados a partir de uma execução real da aplicação.

### 1. Inserir produto
![Inserir produto](docs/prints/01-inserir-produto.png)

### 2. Listar produtos
![Listar produtos](docs/prints/02-listar-produtos.png)

### 3. Buscar produto por ID
![Buscar produto por ID](docs/prints/03-buscar-produto-por-id.png)

### 4. Atualizar produto
Pressionar ENTER mantém o valor atual do campo. Depois da atualização, a listagem mostra o novo preço e estoque.

![Atualizar produto](docs/prints/04-atualizar-produto.png)

### 5. Excluir produto (soft delete)
O produto 4 é excluído logicamente: some da listagem e a busca pelo Id 4 não o encontra mais.

![Excluir produto](docs/prints/05-excluir-produto.png)

### 6. Tentativa de SQL Injection bloqueada
O texto malicioso é gravado literalmente como nome, e a tabela continua existindo.

![SQL Injection bloqueado](docs/prints/06-sql-injection-bloqueado.png)

### 7. Arquivo de log
![Arquivo de log](docs/prints/07-arquivo-de-log.png)

### 8. Testes unitários
![Testes unitários](docs/prints/08-testes-unitarios.png)

---

Desenvolvido por **Paulo Akira Okama — RM556840**.
