using ControleFinanceiroAPI.DTO.Ambiente;
using ControleFinanceiroAPI.DTO.Categoria;
using ControleFinanceiroAPI.DTO.Conta;
using ControleFinanceiroAPI.DTO.Orcamento;
using ControleFinanceiroAPI.DTO.Transacao;
using ControleFinanceiroAPI.DTO.Usuario;
using ControleFinanceiroAPI.Model;
using System.Net.NetworkInformation;

namespace ControleFinanceiroAPI.DTO.Common
{
    /// <summary>
    /// Conversões entre entidades e DTOs. Os ToEntity recebem o ambiente e o usuário do token, nunca do
    /// corpo da requisição: é o que impede gravar dados em um ambiente que não é o selecionado.
    /// Todos devolvem null quando a origem é null.
    /// </summary>
    public static class DTOMapper
    {
        /// <summary>Dados públicos do usuário (id, nome, e-mail), usados nas listas de membros.</summary>
        public static UsuarioResumoDTO? ToResumoDTO(this Model.Usuario entity)
        {
            if (entity == null) return null;
            return new UsuarioResumoDTO
            {
                Id = entity.Id,
                Nome = entity.Nome,
                Email = entity.Email
            };
        }

        /// <summary>
        /// Copia só o nome. Id, criador e data de criação são definidos pelo AmbienteService.AddAsync.
        /// </summary>
        public static Model.Ambiente? ToEntity(this AmbienteRequestDTO dto)
        {
            if (dto == null) return null;
            return new Model.Ambiente
            {
                Nome = dto.Nome
            };
        }

        public static AmbienteResponseDTO? ToResponseDTO(this Model.Ambiente entity)
        {
            if (entity == null) return null;
            return new AmbienteResponseDTO
            {
                Id = entity.Id,
                Nome = entity.Nome,
                DataCriacao = entity.DataCriacao,
                Membros = entity.Membros?.Select(m => m.ToResponseDTO()!).ToList() ?? []
            };
        }

        public static IEnumerable<AmbienteResponseDTO> ToDTOList(this IEnumerable<Model.Ambiente> ambientes)
        {
            if (ambientes is null || !ambientes.Any())
                return new List<AmbienteResponseDTO>();

            return ambientes.Select(a => a.ToResponseDTO()!).ToList();
        }

        public static AmbienteMembroResponseDTO? ToResponseDTO(this AmbienteMembro entity)
        {
            if (entity == null) return null;
            return new AmbienteMembroResponseDTO
            {
                Usuario = entity.Usuario?.ToResumoDTO(),
                Role = entity.Role
            };
        }

        public static Model.Categoria? ToEntity(this CategoriaRequestDTO dto, Guid ambienteId, string userId)
        {
            if (dto == null) return null;

            return new Model.Categoria
            {
                Nome = dto.Nome,
                Cor = dto.Cor,
                UrlIcone = dto.UrlIcone,
                AmbienteId = ambienteId,
                UsuarioId = userId
            };
        }

        public static CategoriaResponseDTO? ToResponseDTO(this Model.Categoria entity)
        {
            if (entity == null) return null;

            return new CategoriaResponseDTO
            {
                Id = entity.Id,
                Nome = entity.Nome,
                Cor = entity.Cor,
                UrlIcone = entity.UrlIcone,
                UsuarioId = entity.UsuarioId
            };
        }

        public static IEnumerable<CategoriaResponseDTO> ToDTOList(this IEnumerable<Model.Categoria> categorias)
        {
            if (categorias is null || !categorias.Any())
                return new List<CategoriaResponseDTO>();

            return categorias.Select(c => c.ToResponseDTO()!).ToList();
        }

        public static Model.Conta? ToEntity(this ContaRequestDTO dto, Guid ambienteId, string userId)
        {
            if (dto == null) return null;
            return new Model.Conta
            {
                Nome = dto.Nome,
                TipoConta = dto.TipoConta,
                Saldo = dto.Saldo,
                AmbienteId = ambienteId,
                UsuarioId = userId
            };
        }

        public static ContaResponseDTO? ToResponseDTO(this Model.Conta entity)
        {
            if (entity == null) return null;
            return new ContaResponseDTO
            {
                Id = entity.Id,
                Nome = entity.Nome,
                TipoConta = entity.TipoConta,
                Saldo = entity.Saldo,
                UsuarioId = entity.UsuarioId
            };
        }

        public static IEnumerable<ContaResponseDTO> ToDTOList(this IEnumerable<Model.Conta> contas)
        {
            if (contas is null || !contas.Any())
                return new List<ContaResponseDTO>();

            return contas.Select(c => c.ToResponseDTO()!).ToList();
        }

        public static Model.Orcamento? ToEntity(this OrcamentoRequestDTO dto, Guid ambienteId, string userId)
        {
            if (dto == null) return null;
            return new Model.Orcamento
            {
                Nome = dto.Nome,
                Descricao = dto.Descricao,
                ValorLimite = dto.ValorLimite,
                DataLimite = dto.DataLimite,
                StatusOrcamento = dto.StatusOrcamento,
                CategoriaId = dto.CategoriaId,
                AmbienteId = ambienteId,
                UsuarioId = userId
            };
        }

        public static OrcamentoResponseDTO? ToResponseDTO(this Model.Orcamento entity)
        {
            if (entity == null) return null;
            return new OrcamentoResponseDTO
            {
                Id = entity.Id,
                Nome = entity.Nome,
                Descricao = entity.Descricao,
                ValorLimite = entity.ValorLimite,
                DataLimite = entity.DataLimite,
                StatusOrcamento = entity.StatusOrcamento,
                CategoriaId = entity.CategoriaId,
                UsuarioId = entity.UsuarioId
            };
        }

        public static IEnumerable<OrcamentoResponseDTO> ToDTOList(this IEnumerable<Model.Orcamento> orcamentos)
        {
            if (orcamentos is null || !orcamentos.Any())
                return new List<OrcamentoResponseDTO>();

            return orcamentos.Select(o => o.ToResponseDTO()!).ToList();
        }

        /// <summary>
        /// Cria a transação (ou uma das parcelas) a partir do DTO, no ambiente e com o autor do token.
        /// </summary>
        /// <param name="dto">Dados enviados pelo frontend.</param>
        /// <param name="ambienteId">Ambiente ativo do token.</param>
        /// <param name="userId">Usuário do token, gravado como autor.</param>
        /// <param name="parcelaIndex">
        /// Posição da parcela, começando em 0. O controller chama uma vez por parcela quando
        /// <c>dto.Parcelas</c> &gt; 1; nos demais casos fica 0.
        /// </param>
        /// <returns>A entidade a gravar, ou null se o DTO for null.</returns>
        /// <remarks>
        /// Cada parcela recebe o <c>dto.Valor</c> inteiro (o valor informado é o da parcela, não o total
        /// da compra) e a mesma Data da compra; só a competência e a observação mudam.
        /// </remarks>
        public static Model.Transacao? ToEntity(this TransacaoRequestDTO dto, Guid ambienteId, string userId, int parcelaIndex = 0)
        {
            if (dto == null) return null;

            // Parcela i cai i meses depois da competência informada (parcela 1 = competência do DTO)
            var mesCompetencia = dto.MesCompetencia.AddMonths(parcelaIndex);

            string? observacao = dto.Observacao;
            if (dto.Parcelas > 1)
            {
                // Não há campo de parcela no modelo: o prefixo "Parcela N/M" na observação é a única marca
                // de que a transação é parcela. TransacaoExtension.TryGetParcela e o pré-filtro do
                // TransacaoRepository leem exatamente este formato — mudar aqui quebra os relatórios de parcelas.
                var parcela = $"Parcela {parcelaIndex + 1}/{dto.Parcelas}";
                observacao = string.IsNullOrWhiteSpace(observacao) ? parcela : $"{parcela} — {observacao}";
            }

            return new Model.Transacao
            {
                Descricao = dto.Descricao,
                Valor = dto.Valor,
                Data = dto.Data,
                Observacao = observacao,
                TipoTransacao = dto.TipoTransacao,
                CategoriaId = dto.CategoriaId,
                ContaId = dto.ContaId,
                ContaDestinoId = dto.ContaDestinoId,
                AmbienteId = ambienteId,
                UsuarioId = userId,
                MesCompetencia = mesCompetencia
            };
        }

        /// <summary>
        /// Converte a transação para resposta. CategoriaNome, ContaNome e ContaDestinoNome só vêm preenchidos
        /// quando a consulta carregou as navegações (Include); em GetById, criação e edição vêm nulos.
        /// </summary>
        public static TransacaoResponseDTO? ToResponseDTO(this Model.Transacao entity)
        {
            if (entity == null) return null;
            return new TransacaoResponseDTO
            {
                Id = entity.Id,
                Descricao = entity.Descricao,
                Valor = entity.Valor,
                Data = entity.Data,
                Observacao = entity.Observacao,
                TipoTransacao = entity.TipoTransacao,
                CategoriaId = entity.CategoriaId,
                CategoriaNome = entity.Categoria?.Nome,
                ContaId = entity.ContaId,
                ContaNome = entity.Conta?.Nome,
                ContaDestinoId = entity.ContaDestinoId,
                ContaDestinoNome = entity.ContaDestino?.Nome,
                UsuarioId = entity.UsuarioId,
                MesCompetencia = entity.MesCompetencia
            };
        }

        public static IEnumerable<TransacaoResponseDTO> ToDTOList(this IEnumerable<Model.Transacao> transacoes)
        {
            if (transacoes is null || !transacoes.Any())
                return new List<TransacaoResponseDTO>();

            return transacoes.Select(t => t.ToResponseDTO()!).ToList();
        }

        /// <summary>
        /// Converte o usuário em <see cref="UsuarioResponseDTO"/>. Sem chamadas no código atual.
        /// Atenção: <c>Token</c> recebe o refresh token gravado no usuário.
        /// </summary>
        public static UsuarioResponseDTO? ToResponseDTO(this Model.Usuario entity)
        {
            if (entity == null) return null;
            return new UsuarioResponseDTO
            {
                Id = entity.Id,
                Nome = entity.Nome,
                Email = entity.Email,
                DtaCriacao = entity.DtaCriacao,
                Token = entity.RefreshToken
            };
        }
    }
}
