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
        public override void OnActionExecuting(ActionExecutingContext context)
        {
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
