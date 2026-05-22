# Planejamento - API de Controle Financeiro

**Stack:** .NET 10 · Entity Framework Core · SQL Server · Scalar (OpenAPI)

---

## Entidades do Domínio

| Entidade       | Descrição                                      |
|----------------|------------------------------------------------|
| `Usuario`      | Dono da conta (autenticação futura)            |
| `Conta`        | Conta bancária, carteira, cartão etc.          |
| `Categoria`    | Classificação da transação (Alimentação, etc.) |
| `Transacao`    | Receita ou despesa vinculada a uma conta       |
| `Orcamento`    | Meta de gasto por categoria em um período      |

---

## Etapas de Desenvolvimento

### 1. Enums
**Pasta:** `Enums/`

- [ ] `TipoTransacao.cs` → `Receita = 1`, `Despesa = 2`
- [ ] `TipoConta.cs` → `Corrente`, `Poupanca`, `Carteira`, `Cartao`
- [ ] `StatusOrcamento.cs` → `Ativo`, `Encerrado`

---

### 2. Models (Entidades)
**Pasta:** `Model/`

- [ ] `Usuario.cs`
  - `Id`, `Nome`, `Email`, `SenhaHash`, `DataCriacao`
- [ ] `Conta.cs`
  - `Id`, `UsuarioId`, `Nome`, `TipoConta`, `Saldo`, `DataCriacao`
- [ ] `Categoria.cs`
  - `Id`, `UsuarioId`, `Nome`, `Cor` (hex), `Icone`
- [ ] `Transacao.cs`
  - `Id`, `ContaId`, `CategoriaId`, `Descricao`, `Valor`, `TipoTransacao`, `Data`, `Observacao`
- [ ] `Orcamento.cs`
  - `Id`, `UsuarioId`, `CategoriaId`, `ValorLimite`, `MesAno` (ex: `2026-05`), `Status`

> Dica: Use `decimal` para valores monetários, nunca `double`.

---

### 3. DbContext
**Pasta:** `Context/`

- [ ] `AppDbContext.cs`
  - Herdar de `DbContext`
  - Criar `DbSet<T>` para cada entidade
  - Configurar string de conexão no `appsettings.json`
  - Registrar o contexto no `Program.cs` com `AddDbContext`

---

### 4. Migrations (EF Core)
No terminal, dentro da pasta do projeto:

```bash
# Criar a primeira migration
dotnet ef migrations add InitialCreate

# Aplicar no banco
dotnet ef database update
```

- [ ] Criar migration inicial
- [ ] Verificar se as tabelas foram geradas corretamente no banco

---

### 5. DTOs
**Pasta:** `DTO/`

Para cada entidade crie pelo menos dois DTOs:
- `XxxRequestDto` — dados que chegam do cliente (POST/PUT)
- `XxxResponseDto` — dados que a API retorna

- [ ] `UsuarioRequestDto.cs` / `UsuarioResponseDto.cs`
- [ ] `ContaRequestDto.cs` / `ContaResponseDto.cs`
- [ ] `CategoriaRequestDto.cs` / `CategoriaResponseDto.cs`
- [ ] `TransacaoRequestDto.cs` / `TransacaoResponseDto.cs`
- [ ] `OrcamentoRequestDto.cs` / `OrcamentoResponseDto.cs`

---

### 6. Interfaces dos Repositórios
**Pasta:** `Interfaces/`

- [ ] `IRepositorioBase.cs` — operações genéricas (GetAll, GetById, Add, Update, Delete)
- [ ] `IUsuarioRepositorio.cs`
- [ ] `IContaRepositorio.cs`
- [ ] `ICategoriaRepositorio.cs`
- [ ] `ITransacaoRepositorio.cs`
- [ ] `IOrcamentoRepositorio.cs`

---

### 7. Repositórios (Implementações)
**Pasta:** `Repositories/`

- [ ] `RepositorioBase.cs` — implementação genérica usando o `AppDbContext`
- [ ] `UsuarioRepositorio.cs`
- [ ] `ContaRepositorio.cs`
- [ ] `CategoriaRepositorio.cs`
- [ ] `TransacaoRepositorio.cs` — incluir filtro por período e por tipo
- [ ] `OrcamentoRepositorio.cs`

---

### 8. Interfaces dos Services
**Pasta:** `Interfaces/`

- [ ] `IUsuarioService.cs`
- [ ] `IContaService.cs`
- [ ] `ICategoriaService.cs`
- [ ] `ITransacaoService.cs`
- [ ] `IOrcamentoService.cs`

---

### 9. Services (Regras de Negócio)
**Pasta:** `Services/`

- [ ] `UsuarioService.cs`
- [ ] `ContaService.cs` — atualizar saldo ao registrar transação
- [ ] `CategoriaService.cs`
- [ ] `TransacaoService.cs` — validar saldo disponível em despesas
- [ ] `OrcamentoService.cs` — calcular percentual gasto do orçamento

---

### 10. Controllers
**Pasta:** `Controllers/`

Cada controller expõe os endpoints REST para sua entidade.

- [ ] `UsuariosController.cs`
- [ ] `ContasController.cs`
- [ ] `CategoriasController.cs`
- [ ] `TransacoesController.cs`
- [ ] `OrcamentosController.cs`

**Endpoints mínimos por controller:**

| Verbo  | Rota              | Ação                    |
|--------|-------------------|-------------------------|
| GET    | `/api/[entidade]` | Listar todos (paginado) |
| GET    | `/api/[entidade]/{id}` | Buscar por ID      |
| POST   | `/api/[entidade]` | Criar novo              |
| PUT    | `/api/[entidade]/{id}` | Atualizar          |
| DELETE | `/api/[entidade]/{id}` | Remover            |

---

### 11. Common (Utilitários)
**Pasta:** `Common/`

- [ ] `ApiResponse.cs` — wrapper padrão de resposta
  ```csharp
  // Exemplo de estrutura
  { "sucesso": true, "dados": { ... }, "mensagem": "..." }
  ```
- [ ] `PaginacaoParametros.cs` — `Pagina`, `TamanhoPagina`

---

### 12. Middleware
**Pasta:** `Middleware/`

- [ ] `ExceptionMiddleware.cs` — capturar exceções não tratadas e retornar resposta padronizada
- [ ] Registrar o middleware no `Program.cs` com `app.UseMiddleware<ExceptionMiddleware>()`

---

### 13. Logging
**Pasta:** `Logging/`

- [ ] Configurar o `ILogger` injetado nos Services
- [ ] Logar erros no `ExceptionMiddleware`
- [ ] (Opcional) Configurar nível de log no `appsettings.json`

---

### 14. Registro de Dependências no Program.cs

- [ ] `AddDbContext<AppDbContext>`
- [ ] Registrar cada repositório: `AddScoped<IXxxRepositorio, XxxRepositorio>`
- [ ] Registrar cada service: `AddScoped<IXxxService, XxxService>`
- [ ] `AddControllers()`
- [ ] `AddOpenApi()` + `MapScalarApiReference()`

---

## Ordem Sugerida de Implementação

```
Enums → Models → DbContext → Migration → DTOs
→ Interfaces Repos → Repositórios → Interfaces Services
→ Services → Controllers → Common → Middleware → Program.cs
```

---

## Endpoints Extras (após o CRUD básico)

- [ ] `GET /api/transacoes/resumo?mes=2026-05` — total de receitas e despesas do mês
- [ ] `GET /api/contas/{id}/extrato` — histórico de transações de uma conta
- [ ] `GET /api/orcamentos/{id}/progresso` — quanto foi gasto vs. limite do orçamento

---

## Checklist Final

- [ ] Todos os endpoints testados no Scalar (`/scalar`)
- [ ] Migrations aplicadas e banco consistente
- [ ] Nenhum `null reference` não tratado
- [ ] Respostas sempre usando `ApiResponse` padronizado
- [ ] Erros retornam código HTTP correto (400, 404, 500)
