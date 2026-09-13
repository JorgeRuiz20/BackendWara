using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using WARA.Domain.Ports;

namespace WARA.Infrastructure.Security
{
    /// <summary>
    /// Configuración necesaria para generar y validar tokens JWT.
    /// </summary>
    public class JwtSettings
    {
        public string SecretKey { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int ExpiracionMinutos { get; set; } = 120;
    }

    /// <summary>
    /// Adaptador de salida que implementa ITokenGenerator usando JWT.
    /// </summary>
    public class JwtTokenGenerator : ITokenGenerator
    {
        private readonly JwtSettings _settings;

        public JwtTokenGenerator(JwtSettings settings)
        {
            _settings = settings;
        }

        public string GenerarToken(int usuarioId, string nombreUsuario)
        {
            var claves = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
            var credenciales = new SigningCredentials(claves, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuarioId.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, nombreUsuario),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_settings.ExpiracionMinutos),
                signingCredentials: credenciales);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
