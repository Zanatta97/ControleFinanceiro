using ControleFinanceiroAPI.DTO.Common;
using ControleFinanceiroAPI.DTO.Usuario;
using ControleFinanceiroAPI.Model;
using ControleFinanceiroAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ControleFinanceiroAPI.Interfaces.Services
{
    public interface IUsuarioService
    {
        Task<ApiResponseDTO<object>> CreateRole(string roleName);
        Task<ApiResponseDTO<object>> AssignUserToRole(string email, string roleName);
        Task<ApiResponseDTO<object>> Login([FromBody] UsuarioLoginRequestDTO usuario);
        Task<ApiResponseDTO<object>> Register([FromBody] UsuarioRegisterDTO dto);
        Task<ApiResponseDTO<object>> RefreshToken(TokenDTO tokenDTO);
        Task<ApiResponseDTO<object>> Revoke(string username);
    }
}
