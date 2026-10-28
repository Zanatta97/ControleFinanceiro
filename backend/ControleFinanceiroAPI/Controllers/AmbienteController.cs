using ControleFinanceiroAPI.Common.Extensions;
using ControleFinanceiroAPI.DTO.Ambiente;
using ControleFinanceiroAPI.DTO.Common;
using ControleFinanceiroAPI.DTO.Usuario;
using ControleFinanceiroAPI.Filters;
using ControleFinanceiroAPI.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControleFinanceiroAPI.Controllers
{
    /// <summary>
    /// Ambientes do usuário, membros e seleção do ambiente ativo. Diferente dos demais controllers, não usa
    /// o claim "ambiente_id": o ambiente vem da rota e a permissão é conferida no AmbienteService.
    /// Ações que alteram ambientes ou membros são bloqueadas no modo demonstração ([BloquearDemo]).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AmbienteController : ControllerBase
    {
        private readonly IAmbienteService _service;

        public AmbienteController(IAmbienteService service)
        {
            _service = service;
        }

        /// <summary>Todos os ambientes do sistema. Só Admin do sistema; 404 se não houver nenhum.</summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponseDTO<IEnumerable<AmbienteResponseDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAll()
        {
            var ambientes = await _service.GetAllAsync();

            if (!ambientes.Any())
                return StatusCode(StatusCodes.Status404NotFound,
                    ApiResponseDTO<IEnumerable<AmbienteResponseDTO>>.ErrorResponse("Nenhum ambiente encontrado.", StatusCodes.Status404NotFound));

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<IEnumerable<AmbienteResponseDTO>>.SuccessResponse(ambientes.ToDTOList()));
        }

        /// <summary>Ambientes dos quais o usuário logado é membro, com a lista de membros e papéis.</summary>
        [HttpGet("user-ambientes")]
        [ProducesResponseType(typeof(ApiResponseDTO<IEnumerable<AmbienteResponseDTO>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllByUsuario()
        {
            var userId = User.GetUserId();
            var ambientes = await _service.GetAllByUsuarioAsync(userId);

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<IEnumerable<AmbienteResponseDTO>>.SuccessResponse(ambientes.ToDTOList()));
        }

        /// <summary>Um ambiente do qual o usuário é membro; 404 se não existir ou se ele não for membro.</summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponseDTO<AmbienteResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var userId = User.GetUserId();
            var ambiente = await _service.GetAsync(id, userId);

            if (ambiente is null)
                return StatusCode(StatusCodes.Status404NotFound,
                    ApiResponseDTO<AmbienteResponseDTO>.ErrorResponse("Ambiente não encontrado.", StatusCodes.Status404NotFound));

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<AmbienteResponseDTO>.SuccessResponse(ambiente.ToResponseDTO()!));
        }

        /// <summary>
        /// Membros do ambiente (id, nome, e-mail). Comportamento atual: não confere se o usuário logado é
        /// membro do ambiente pedido.
        /// </summary>
        [HttpGet("{id}/membros")]
        [ProducesResponseType(typeof(ApiResponseDTO<IEnumerable<UsuarioResumoDTO>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMembros(Guid id)
        {
            var usuarios = await _service.GetAllByAmbienteAsync(id);
            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<IEnumerable<UsuarioResumoDTO>>.SuccessResponse(usuarios.Select(u => u.ToResumoDTO()!)));
        }

        /// <summary>Cria um ambiente com o usuário logado como Dono. Não troca o ambiente ativo.</summary>
        [HttpPost]
        [BloquearDemo]
        [ProducesResponseType(typeof(ApiResponseDTO<AmbienteResponseDTO>), StatusCodes.Status201Created)]
        public async Task<IActionResult> Create([FromBody] AmbienteRequestDTO dto)
        {
            var userId = User.GetUserId();
            var ambiente = await _service.AddAsync(dto.ToEntity()!, userId);

            return StatusCode(StatusCodes.Status201Created,
                ApiResponseDTO<AmbienteResponseDTO>.SuccessResponse(ambiente.ToResponseDTO()!, StatusCodes.Status201Created));
        }

        /// <summary>Renomeia o ambiente. Só Dono ou Admin do sistema (403 caso contrário).</summary>
        [HttpPut("{id}")]
        [BloquearDemo]
        [ProducesResponseType(typeof(ApiResponseDTO<AmbienteResponseDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update(Guid id, [FromBody] AmbienteRequestDTO dto)
        {
            var userId = User.GetUserId();
            var ambiente = await _service.UpdateAsync(id, dto.ToEntity()!, userId);

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<AmbienteResponseDTO>.SuccessResponse(ambiente.ToResponseDTO()!));
        }

        /// <summary>Exclui o ambiente. Só Dono ou Admin do sistema (403 caso contrário); 404 se não existir.</summary>
        [HttpDelete("{id}")]
        [BloquearDemo]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = User.GetUserId();
            var deletado = await _service.DeleteAsync(id, userId);

            if (!deletado)
                return StatusCode(StatusCodes.Status404NotFound,
                    ApiResponseDTO<object>.ErrorResponse("Ambiente não encontrado.", StatusCodes.Status404NotFound));

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<object>.SuccessResponse(null!, statusMessage: "Ambiente excluído com sucesso."));
        }

        /// <summary>Adiciona como "Membro" o usuário do e-mail informado na query. Só Dono ou Admin do ambiente.</summary>
        [HttpPost("{id}/membros")]
        [BloquearDemo]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> AdicionarMembro(Guid id, [FromQuery] string membroEmail)
        {
            var userId = User.GetUserId();
            await _service.AdicionarMembroAsync(id, userId, membroEmail);

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<object>.SuccessResponse(null!, statusMessage: "Membro adicionado com sucesso."));
        }

        /// <summary>Remove um membro (membroId = id do usuário). Só Dono ou Admin do ambiente; o Dono não sai (409).</summary>
        [HttpDelete("{id}/membros/{membroId}")]
        [BloquearDemo]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RemoverMembro(Guid id, string membroId)
        {
            var userId = User.GetUserId();
            await _service.RemoverMembroAsync(id, userId, membroId);

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<object>.SuccessResponse(null!, statusMessage: "Membro removido com sucesso."));
        }

        /// <summary>Transfere a posse do ambiente para o membro informado (ver IAmbienteService.AtribuirDonoAsync).</summary>
        [HttpPut("{id}/dono")]
        [BloquearDemo]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> AtribuirDono(Guid id, [FromQuery] string membroId)
        {
            var userId = User.GetUserId();
            await _service.AtribuirDonoAsync(id, userId, membroId);

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<object>.SuccessResponse(null!, statusMessage: "Dono atribuído com sucesso."));
        }

        /// <summary>
        /// Seleciona o ambiente ativo e devolve um novo par de tokens com o claim "ambiente_id".
        /// O frontend precisa trocar os dois tokens que guarda pelos devolvidos aqui.
        /// </summary>
        /// <remarks>
        /// Sem [BloquearDemo] (o frontend é que esconde a troca de ambiente no modo demo).
        /// 403 se o usuário não for membro.
        /// </remarks>
        [HttpPost("selecionar/{id}")]
        [ProducesResponseType(typeof(ApiResponseDTO<TokenDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SelecionarAmbiente(Guid id)
        {
            var userId = User.GetUserId();
            var token = await _service.SelecionarAmbienteAsync(id, userId);

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<TokenDTO>.SuccessResponse(token));
        }
    }
}
