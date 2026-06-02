# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Perfil do Desenvolvedor
- Iniciante em APIs C#, conhecimento sólido da linguagem
- Prefere resumos concisos com detalhes sob demanda
- Comunicação direta e prática

## Stack
- **.NET 10** — ASP.NET Core Web API
- **EF Core 10** + **SQL Server LocalDB** (Windows Auth, sem senha)
- **ASP.NET Core Identity** + **JWT Bearer**
- **Scalar** para documentação OpenAPI (abre em `/scalar/v1`)

## Comandos Comuns

Todos os comandos devem ser executados em `backend/ControleFinanceiroAPI/`.

```powershell
# Rodar a API
dotnet run

# Build
dotnet build

# Aplicar migrations pendentes
dotnet ef database update

# Criar nova migration
dotnet ef migrations add <NomeDaMigration>

# Remover última migration (antes de aplicar ao banco)
dotnet ef migrations remove
```

A API sobe em `http://localhost:5284` (perfil `http`) ou `https://localhost:7234` (perfil `https`, padrão do Scalar/v1.json). O banco de dados é o SQL Server LocalDB (`ControleFinanceiro`).

O front-end Vite proxia `/api` para `https://localhost:7234` — rode o backend com o perfil `https` (ou `dotnet run --launch-profile https`).

## Arquitetura

```
Model → Repository (IRepository<T> + UnityOfWork) → Controller → DTO (Request/Response)
```

**Fluxo de uma requisição:**
1. `RequestLoggingMiddleware` (externo) — captura request/response para log
2. `ErrorHandlingMiddleware` — captura exceções não tratadas, retorna JSON padronizado
3. Controller → UnityOfWork → Repository → DbContext

**Padrões em uso:**
- **Repository + Unity of Work**: `IUnityOfWork` agrega todos os repositórios; `SaveChangesAsync()` é chamado uma única vez por operação.
- **DTO Pattern**: DTOs separados para Request e Response. `DTOMapper.cs` tem métodos de extensão estáticos `ToEntity()` e `ToResponseDTO()`.
- **ApiResponseDTO<T>**: wrapper padrão para todas as respostas. Use `ApiResponseDTO<T>.SuccessResponse(data, statusCode)` e `ApiResponseDTO<T>.ErrorResponse("msg", statusCode)`.

## Logging

`LogSettingsManager` (Singleton) controla o modo de logging em tempo de execução:
- `LogAllRequests = false` (padrão): loga apenas erros (4xx/5xx), sem body (segurança)
- `LogAllRequests = true`: loga todas as requisições com bodies

Endpoints de controle: `POST /api/logs/ativar`, `POST /api/logs/desativar`, `GET /api/logs/status`.

`IApiLogService` é Singleton mas cria um novo escopo DI internamente para cada escrita no banco — padrão necessário para evitar o problema Singleton → Scoped.

## Estrutura de Pastas Relevante

```
backend/ControleFinanceiroAPI/
├── Context/          # AppDbContext (herda IdentityDbContext<Usuario>)
├── Controllers/      # Controllers da API
├── DTO/              # Request/Response DTOs + ApiResponseDTO + DTOMapper
│   └── Common/
├── Enums/            # TipoTransacao, TipoConta, StatusOrcamento
├── Interfaces/       # IRepository<T>, IUnityOfWork, interfaces de repositório
├── Logging/          # IApiLogService, ApiLogService, LogSettingsManager
├── Middleware/       # ErrorHandlingMiddleware, RequestLoggingMiddleware
├── Migrations/
├── Model/            # Entidades EF Core
└── Repositories/     # Repository<T>, implementações concretas, UnityOfWork
```

## Ambientes

`Ambiente` é o conceito de espaço financeiro compartilhável (ex: pessoal, família, empresa). Cada usuário pode pertencer a vários ambientes.

**Fluxo de troca de ambiente:**
1. Após o login, o JWT ainda **não tem** claim de ambiente.
2. Chamar `POST /api/Ambiente/selecionar/{id}` retorna um novo `TokenDTO` (accessToken + refreshToken) com o ambiente embutido no claim.
3. A partir daí, todas as requisições de Conta/Categoria/Transação/Orçamento operam sobre o ambiente selecionado.

O front-end deve armazenar o ambiente ativo e trocar o token ao selecionar outro ambiente.

## O que já está implementado
- Models, Enums, DbContext, Migrations
- DTOs (Request/Response) para todas as entidades + DTOMapper
- Repository pattern completo (genérico + especializados)
- Unity of Work
- ApiResponseDTO (wrapper padrão)
- PaginationParameters (usando X.PagedList)
- ErrorHandlingMiddleware + RequestLoggingMiddleware
- LogsController (`POST /api/logs/ativar`, `POST /api/logs/desativar`, `GET /api/logs/status`)
- Services para todas as entidades
- Controllers completos (Auth, Ambiente, Conta, Categoria, Transacao, Orcamento, Relatorio)
- Endpoints de Relatórios (resumo mensal, extrato por conta, gasto por categoria, evolução mensal, status de orçamentos)

## Próximos Passos (ver PLANEJAMENTO.md)
- Front-end React (Issue #18 — branch `feature/issue-frontend-react`)
