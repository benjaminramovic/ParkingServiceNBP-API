﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using ParkEasyNBP.Application.DTOs;
//using ParkEasyNBP.Application.Models;
using ParkEasyNBP.Domain.Models;
using System.Data.Entity;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;
using Microsoft.Data.SqlClient;
using AutoMapper;

namespace ParkEasyNBP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> userManager; 
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly IMapper mapper;

        public AuthController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager,IMapper mapper)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.mapper = mapper;
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }


        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Register(CredentialModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // Logovanje započinjanja registracije
                Console.WriteLine("Započeta registracija korisnika.");

                ApplicationUser user = new ApplicationUser 
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = model.Username,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Address = model.Address,
                    Phone = model.Phone,
                    Role = model.Role,
                };

                // Logovanje korisničkih podataka
                Console.WriteLine($"Kreiranje korisnika sa ID: {user.Id}, Username: {user.UserName}");
                //user.PasswordHash = userManager.PasswordHasher.HashPassword(user, model.Password);
                var result = await userManager.CreateAsync(user, model.Password);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return BadRequest(new { Message = errors });
                }

                // Proverite da li uloga "User" postoji
                if (!await roleManager.RoleExistsAsync("User"))
                {
                    var roleResult = await roleManager.CreateAsync(new IdentityRole("User"));
                    if (!roleResult.Succeeded)
                    {
                        var roleErrors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                        return BadRequest(new { Message = roleErrors });
                    }
                }

                // Logovanje dodeljivanja uloge korisniku
                Console.WriteLine($"Dodeljivanje uloge 'User' korisniku sa ID: {user.Id}");

              /*  var addToRoleResult = await userManager.AddToRoleAsync(user, "User");
                if (!addToRoleResult.Succeeded)
                {
                    var roleErrors = string.Join(", ", addToRoleResult.Errors.Select(e => e.Description));
                    return BadRequest(new { Message = roleErrors });
                }*/

                return Ok(user);
            }
            catch (DbUpdateException dbEx)
            {
                var sqlEx = dbEx.InnerException as SqlException;
                if (sqlEx != null)
                {
                    // Specifične SQL greške
                    Console.WriteLine($"SQL Error: {sqlEx.Message}, ErrorCode: {sqlEx.Number}");
                    return BadRequest(new { Message = sqlEx.Message, ErrorCode = sqlEx.Number });
                }
                Console.WriteLine($"DbUpdateException: {dbEx.Message}");
                return BadRequest(new { Message = dbEx.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                return BadRequest(new { Message = ex.Message });
            }
        }




        [HttpPost]
        [Route("register-controller")]
        public async Task<IActionResult> RegisterController(CredentialModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                ApplicationUser user = new ApplicationUser 
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = model.Username,
                    Email = model.Email,
                    FirstName = model.FirstName, 
                    LastName = model.LastName,
                    Address = model.Address,
                    Phone = model.Phone,
                    Role = "Controllor"
                };

                var result = await userManager.CreateAsync(user, model.Password);
                if (!result.Succeeded)
                    return BadRequest(result.Errors.FirstOrDefault());

                if (!await roleManager.RoleExistsAsync("Controllor"))
                {
                    await roleManager.CreateAsync(new IdentityRole("Controllor"));
                }
               // await userManager.AddToRoleAsync(user, "Controllor");

                return Ok(user);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] LoginUser model)
        {
            var user = await userManager.FindByNameAsync(model.Username);
            if (user == null)
                return NotFound();
            if (await userManager.CheckPasswordAsync(user, model.Password))
            {
                // Generate JWT token
                var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("78fUjkyzfLz56gTq78fUjkyzfLz56gTq"));
                var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                };

                var userRoles = await userManager.GetRolesAsync(user);
                foreach (var role in userRoles)
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, role));
                }

                var token = new JwtSecurityToken(
                    expires: DateTime.Now.AddMinutes(30),
                    claims: authClaims,
                    audience: "https://localhost:5001/",
                    issuer: "https://localhost:5001/",
                    signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
                );

                var tokenKey = new JwtSecurityTokenHandler().WriteToken(token);

                if (user.RefreshTokens == null)
                {
                    user.RefreshTokens = new List<RefreshToken>();
                }

                var refreshToken = GenerateRefreshToken();
                user.RefreshTokens.Add(new RefreshToken
                {
                    
                    Token = refreshToken,
                    Expires = DateTime.UtcNow.AddDays(7),
                    Created = DateTime.UtcNow
                });
                await userManager.UpdateAsync(user);

                return Ok(
                    new
                    {
                        user = mapper.Map<serDTO>(user),
                        token = tokenKey,
                        refreshToken,
                        expiration = token.ValidTo,
                        
                    });
            }
            return Unauthorized("Wrong username or password");
        
    }
        #region
        [HttpPost]
        [Route("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] TokenModel model)
        {
            var principal = GetPrincipalFromExpiredToken(model.Token);
            var username = principal.Identity.Name;
            var user = await userManager.FindByNameAsync(username);

            if (user == null || !user.RefreshTokens.Any(t => t.Token == model.RefreshToken && t.IsActive))
                return BadRequest("Invalid token");

            var refreshToken = user.RefreshTokens.First(t => t.Token == model.RefreshToken);
            if (refreshToken.IsExpired) return Unauthorized("Refresh token has expired");

            refreshToken.Revoked = DateTime.UtcNow;
            var newRefreshToken = GenerateRefreshToken();
            user.RefreshTokens.Add(new RefreshToken
            {
                Token = newRefreshToken,
                Expires = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow
            });
            await userManager.UpdateAsync(user);

            var newJwtToken = GenerateJwtToken(principal.Claims.ToList());
            return Ok(new
            {
                token = newJwtToken,
                refreshToken = newRefreshToken,
                expiration = DateTime.UtcNow.AddHours(2)
            });
        }

        private string GenerateJwtToken(List<Claim> claims)
        {
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("78fUjkyzfLz56gTq78fUjkyzfLz56gTq"));
            var token = new JwtSecurityToken(
                expires: DateTime.Now.AddHours(2),
                claims: claims,
                audience: "https://localhost:5001/",
                issuer: "https://localhost:5001/",
                signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("78fUjkyzfLz56gTq78fUjkyzfLz56gTq")),
                ValidateLifetime = false // here we are saying that we don't care about the token's expiration date
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            var jwtSecurityToken = securityToken as JwtSecurityToken;
            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");

            return principal;
        }
        #endregion




    }
}
