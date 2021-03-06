﻿using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using AuthService.Common.Enums;
using AuthService.Common.Models;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using AuthService.Common.Settings;
using AuthService.Common.Interfaces.Security;

namespace AuthService.Logic.Services
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly IEnumerable<ISecurityService> _securityServices;
        private readonly JwtSecurityTokenHandler _tokenHandler;
        private readonly SecuritySettings _securitySettings;

        public JwtTokenService(IEnumerable<ISecurityService> securityServices, IOptions<SecuritySettings> securitySettings)
        {
            _securityServices = securityServices;
            _tokenHandler = new JwtSecurityTokenHandler();
            _securitySettings = securitySettings.Value;
        }

        public TokenModel GenerateToken(string email)
        {
            var dateTimeNow = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds();
            var expiresDateTime = DateTime.Now.AddDays(100);
            var payload = new JwtPayload
            {
                { "sub", email },
                { "email", email },
                { "iat", dateTimeNow },
                { "nbf", dateTimeNow },
                { "exp", ((DateTimeOffset)expiresDateTime).ToUnixTimeSeconds() }
            };

            if (_securitySettings.Audience != null)
            {
                payload.Add("aud", _securitySettings.Audience);
            }

            if (_securitySettings.Issuer != null)
            {
                payload.Add("iss", _securitySettings.Issuer);
            }

            var securityService = GetSecurityService(_securitySettings.SecurityType);

            var token = securityService.GenerateToken(payload);

            return new TokenModel { Token = token, ExpiredAt = expiresDateTime };
        }

        public JwtSecurityToken Read(string token)
        {
            try
            {
                var jwtToken = _tokenHandler.ReadJwtToken(token);

                if (string.IsNullOrEmpty(jwtToken.Subject))
                {
                    throw new Exception("Token does not contain subject");
                }

                if (string.IsNullOrEmpty(jwtToken.Audiences.FirstOrDefault()))
                {
                    throw new Exception("Token does not contain audience");
                }

                return jwtToken;
            }
            catch
            {
                throw new Exception("Token does not contain audience");
            }

        }

        public ClaimsPrincipal Validate(string token)
        {
            var result = new ClaimsPrincipal();
            var type = _securitySettings.SecurityType;

            if (!Enum.IsDefined(typeof(SecurityTypeEnum), type))
            {
                throw new Exception("Invalid security type");
            }

            if (string.IsNullOrEmpty(_securitySettings.Audience))
            {
                throw new Exception("Setting Audience is null or empty");
            }

            try
            {
                var securityService = GetSecurityService(_securitySettings.SecurityType);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    RequireSignedTokens = true,
                    IssuerSigningKey = securityService.GetSecurityKey(),
                    ValidAudience = _securitySettings.Audience,
                    ValidIssuer = _securitySettings.Issuer,
                    ValidateAudience = true,
                    ValidateIssuer = true,
                    RequireExpirationTime = true,
                    ValidateLifetime = true
                };
                var principal = _tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
                if (validatedToken != null)
                {
                    return principal;
                }
            }
            catch (ArgumentException)
            {
                throw new Exception("Token validation failed");
            }
            catch (SecurityTokenExpiredException)
            {
                throw new Exception("Token expired");
            }
            catch (SecurityTokenInvalidSignatureException)
            {
                throw new Exception("Invalid signature");
            }
            catch (SecurityTokenInvalidIssuerException ex)
            {
                throw new Exception($"Invalid issuer: {ex.InvalidIssuer}");
            }
            catch (SecurityTokenInvalidAudienceException ex)
            {
                throw new Exception($"Invalid audience: {ex.InvalidAudience}");
            }

            return result;
        }

        private ISecurityService GetSecurityService(SecurityTypeEnum type)
        {
            var securityService = _securityServices.FirstOrDefault(s => s.Type == type);
            if (securityService == null)
            {
                throw new Exception($"Service for type '{type}' not found");
            }

            return securityService;
        }
    }
}
