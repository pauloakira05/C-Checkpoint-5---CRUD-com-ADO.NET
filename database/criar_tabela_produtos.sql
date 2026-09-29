-- =============================================================
-- Script de criação da tabela Produtos (SQLite)
-- Checkpoint 5 - CRUD com ADO.NET
-- =============================================================
-- A coluna "Ativo" implementa o SOFT DELETE (bônus):
--   1 = produto ativo | 0 = produto excluído logicamente.
-- "DataExclusao" registra quando o produto foi excluído.
-- =============================================================

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
