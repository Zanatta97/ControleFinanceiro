using ControleFinanceiroAPI.DTO.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ControleFinanceiroAPI.Filters
{
    /// <summary>
    /// Bloqueia o acesso de usuários na Role "Demo" a endpoints sensíveis
    /// (troca de senha, gestão de ambientes, controle de logs).
    /// Retorna 403 com o ApiResponseDTO padronizado.
    /// </summary>
    public class BloquearDemoAttribute : ActionFilterAttribute
    {
        /// <summary>Interrompe a ação com 403 quando o usuário tem a role "Demo" no token.</summary>
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Só os endpoints marcados com o atributo são bloqueados (os "sensíveis" do PLANEJAMENTO.md,
            // seção Modo Demonstração). Os cadastros do ambiente demo continuam editáveis e são recriados
            // no reset diário (DemoService).
            // Atribuir context.Result curto-circuita: a action não é executada.
            if (context.HttpContext.User.IsInRole("Demo"))
            {
                context.Result = new ObjectResult(
                    ApiResponseDTO<object>.ErrorResponse(
                        "Ação indisponível no modo demonstração.",
                        StatusCodes.Status403Forbidden))
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            base.OnActionExecuting(context);
        }
    }
}
