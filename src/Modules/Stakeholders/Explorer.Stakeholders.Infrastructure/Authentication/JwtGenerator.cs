﻿using Explorer.Stakeholders.API.Dtos;
using Explorer.Stakeholders.Core.Domain;
using Explorer.Stakeholders.Core.UseCases;
using FluentResults;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Explorer.Stakeholders.Infrastructure.Authentication;

public class JwtGenerator : ITokenGenerator
{
    private readonly string _key = Environment.GetEnvironmentVariable("JWT_KEY") ?? "explorer_secret_key";
    private readonly string _issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "explorer";
    private readonly string _audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "explorer-front.com";

    public Result<AuthenticationTokensDto> GenerateAccessToken(User user, long personId)
    {
        var authenticationResponse = new AuthenticationTokensDto();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("id", user.Id.ToString()),
            new("username", user.Username),
            new("personId", personId.ToString()),
            new(ClaimTypes.Role, user.GetPrimaryRoleName())
        };

        var jwt = CreateToken(claims, 60 * 24 * 100);
        authenticationResponse.Id = user.Id;
        authenticationResponse.AccessToken = jwt;

        return authenticationResponse;
    }

    public Result<ResetPasswordTokenDto> GenerateResetPasswordToken(long id, string email)
    {
        var resetPasswordResponse = new ResetPasswordTokenDto();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("email", email)
        };

        var jwt = CreateToken(claims, 15);
        resetPasswordResponse.ResetPasswordToken = jwt;

        return resetPasswordResponse;
    }

    private string CreateToken(IEnumerable<Claim> claims, double expirationTimeInMinutes)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            _issuer,
            _audience,
            claims,
            expires: DateTime.Now.AddMinutes(expirationTimeInMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public Result<ResetPasswordTokenDto> GenerateRegistrationConfirmationToken(User user)
    {
        var confirmRegistrationToken = new ResetPasswordTokenDto();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("id", user.Id.ToString()),
            new("username", user.Username),
            new("confirm", "true")
        };

        string token = CreateToken(claims, 15);
        confirmRegistrationToken.ResetPasswordToken = token;
        return confirmRegistrationToken;
    }
}