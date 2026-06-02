# Controle Financeiro

Aplicação web full-stack de controle financeiro pessoal, com suporte a múltiplos usuários e ambientes compartilháveis. O backend foi desenvolvido manualmente pelo autor, e o frontend foi construído com auxílio do Claude Code (Anthropic).

---

## Visão Geral

O **Controle Financeiro** permite que cada usuário gerencie suas finanças de forma isolada, podendo criar **ambientes** (espaços financeiros compartilháveis) para organizar gastos pessoais, familiares ou empresariais. Cada ambiente possui suas próprias contas, categorias, transações e orçamentos — completamente separados dos demais.

### Conceitos Centrais

- **Usuário**: cada pessoa tem sua própria conta com autenticação JWT.
- **Ambiente**: espaço financeiro com membros — semelhante a um "workspace". Um usuário pode pertencer a vários ambientes (pessoal, família, empresa etc.).
- **Conta**: conta bancária, carteira ou cartão de crédito dentro de um ambiente.
- **Categoria**: agrupamento de transações (ex: Alimentação, Transporte).
- **Transação**: receita ou despesa associada a uma conta e categoria.
- **Orçamento**: limite mensal de gastos por categoria, com acompanhamento de progresso.

---

## Stack Tecnológica

### Backend
| Tecnologia | Versão |
|---|---|
| .NET / ASP.NET Core Web API | 10 |
| Entity Framework Core | 10 |
| SQL Server LocalDB | Windows Auth |
| ASP.NET Core Identity | — |
| JWT Bearer | — |
| Scalar (OpenAPI) | — |
| X.PagedList | — |

### Frontend
| Tecnologia | Versão |
|---|---|
| React | 19 |
| TypeScript | 5 |
| Vite | 6 |
| React Router | 7 |
| TanStack Query | 5 |
| Tailwind CSS | 3 |
| Axios | — |
| Iconify | 6 |

---

## Arquitetura

### Backend

```
Model → Repository (IRepository<T> + UnityOfWork) → Service → Controller → DTO (Request/Response)
```

**Padrões em uso:**
- **Repository + Unit of Work**: `IUnityOfWork` agrega todos os repositórios; `SaveChangesAsync()` é chamado uma única vez por operação.
- **DTO Pattern**: DTOs separados para Request e Response. `DTOMapper.cs` usa métodos de extensão estáticos (`ToEntity()` / `ToResponseDTO()`).
- **ApiResponseDTO\<T\>**: wrapper padrão para todas as respostas da API.
- **Middlewares**: `ErrorHandlingMiddleware` (erros JSON padronizados) + `RequestLoggingMiddleware` (log de requisições).

**Estrutura de pastas:**

```
backend/ControleFinanceiroAPI/
├── Context/          # AppDbContext (herda IdentityDbContext<Usuario>)
├── Controllers/      # Controllers da API
├── DTO/              # Request/Response DTOs + ApiResponseDTO + DTOMapper
├── Enums/            # TipoTransacao, TipoConta, StatusOrcamento
├── Interfaces/       # IRepository<T>, IUnityOfWork, interfaces de serviço
├── Logging/          # IApiLogService, ApiLogService, LogSettingsManager
├── Middleware/       # ErrorHandlingMiddleware, RequestLoggingMiddleware
├── Migrations/       # EF Core migrations
├── Model/            # Entidades EF Core
├── Repositories/     # Repository<T>, implementações concretas, UnityOfWork
└── Services/         # Lógica de negócio
```

### Frontend

```
frontend/
└── src/
    ├── api/          # Módulos Axios por entidade (auth, conta, transacao…)
    ├── components/   # Componentes reutilizáveis (Layout, Navbar, ui/*)
    ├── context/      # AuthContext, ThemeContext
    ├── hooks/        # usePagination e outros hooks customizados
    ├── pages/        # Páginas da aplicação
    ├── types/        # Tipos TypeScript
    └── utils/        # Utilitários
```

---

## Funcionalidades

### Autenticação e Autorização
- Registro e login com JWT
- Refresh token
- Roles (Admin)
- Claims de ambiente embutidas no token após seleção

### Ambientes
- Criação de ambientes financeiros
- Convite de membros por e-mail
- Troca de ambiente retorna novo JWT com claim do ambiente selecionado
- Isolamento total de dados entre ambientes

### Contas
- Cadastro de contas bancárias, carteiras e cartões
- Extrato por conta com filtro de período
- Saldo mensal por conta

### Categorias
- CRUD de categorias por ambiente
- Ícone customizável

### Transações
- Registro de receitas e despesas
- Paginação e ordenação
- Filtros por conta, categoria, tipo e período

### Orçamentos
- Limite mensal por categoria
- Acompanhamento de progresso (gasto vs. limite)
- Status: Ativo, Excedido, Concluído

### Relatórios
| Endpoint | Descrição |
|---|---|
| `GET /api/Relatorio/resumo-mensal` | Receitas vs. despesas do mês |
| `GET /api/Relatorio/extrato-conta` | Extrato por conta e período |
| `GET /api/Relatorio/gasto-categoria` | Gastos agrupados por categoria |
| `GET /api/Relatorio/evolucao-mensal` | Evolução de saldo ao longo dos meses |
| `GET /api/Relatorio/orcamentos-status` | Status dos orçamentos ativos |

### Logging em Tempo de Execução
- `LogSettingsManager` (Singleton) controla o nível de log sem reiniciar a API
- Padrão: loga apenas erros (4xx/5xx) sem body (proteção de dados)
- Modo verbose: loga todas as requisições com bodies
- Endpoints: `POST /api/logs/ativar`, `POST /api/logs/desativar`, `GET /api/logs/status`

---

## Fluxo de Autenticação e Seleção de Ambiente

```
1. POST /api/Auth/login          → JWT sem claim de ambiente
2. GET  /api/Ambiente/user-ambientes → lista de ambientes do usuário
3. POST /api/Ambiente/selecionar/{id} → novo JWT com ambiente embutido
4. Todas as demais requisições usam o token com ambiente
```

---

## Como Executar

### Pré-requisitos
- .NET 10 SDK
- SQL Server LocalDB (instalado com o Visual Studio ou separadamente)
- Node.js 20+

### Backend

```powershell
cd backend/ControleFinanceiroAPI

# Aplicar migrations e criar o banco
dotnet ef database update

# Rodar a API (perfil HTTPS — necessário para o frontend)
dotnet run --launch-profile https
```

A API sobe em:
- `https://localhost:7234` (HTTPS — padrão)
- `http://localhost:5284` (HTTP)

Documentação interativa: `https://localhost:7234/scalar/v1`

### Frontend

```powershell
cd frontend

npm install
npm run dev
```

O Vite proxia `/api` para `https://localhost:7234`. Acesse em `http://localhost:5173`.

---

## Endpoints Principais

| Módulo | Método | Rota |
|---|---|---|
| **Auth** | POST | `/api/Auth/login` |
| **Auth** | POST | `/api/Auth/register` |
| **Auth** | POST | `/api/Auth/refresh-token` |
| **Ambiente** | GET | `/api/Ambiente/user-ambientes` |
| **Ambiente** | POST | `/api/Ambiente` |
| **Ambiente** | POST | `/api/Ambiente/selecionar/{id}` |
| **Ambiente** | POST | `/api/Ambiente/{id}/membros` |
| **Conta** | GET/POST | `/api/Conta` |
| **Conta** | PUT/DELETE | `/api/Conta/{id}` |
| **Conta** | GET | `/api/Conta/{id}/extrato` |
| **Categoria** | GET/POST | `/api/Categoria` |
| **Categoria** | PUT/DELETE | `/api/Categoria/{id}` |
| **Transacao** | GET/POST | `/api/Transacao` |
| **Transacao** | PUT/DELETE | `/api/Transacao/{id}` |
| **Orcamento** | GET/POST | `/api/Orcamento` |
| **Orcamento** | GET | `/api/Orcamento/{id}/progresso` |
| **Relatorio** | GET | `/api/Relatorio/resumo-mensal` |
| **Logs** | GET | `/api/Logs/status` |
| **Logs** | POST | `/api/Logs/ativar` |

---

## Formato Padrão de Resposta

Todas as respostas da API seguem o wrapper `ApiResponseDTO<T>`:

```json
{
  "sucesso": true,
  "dados": { },
  "mensagem": "Operação realizada com sucesso.",
  "statusCode": 200
}
```

---

## Desenvolvimento

O backend foi desenvolvido manualmente pelo autor, com domínio completo de cada camada: modelos, repositórios, serviços, controllers, middlewares e autenticação. O frontend foi construído com auxílio do Claude Code (Anthropic), cobrindo a integração com a API, gerenciamento de estado, roteamento e interface visual com suporte a tema claro/escuro.
