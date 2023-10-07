﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PUC.PosGraduacao.BookStore.Domain.DTO;
using PUC.PosGraduacao.BookStore.Domain.Interfaces.Services;
using PUC.PosGraduacao.BookStore.Domain.Models.Identity;
using System.Security.Claims;

namespace PUC.PosGraduacao.BookStore.API.Controllers
{
  public class AccountController : BaseApiController
  {
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly ITokenService _tokenService;
    public AccountController(UserManager<AppUser> userManager, 
                             SignInManager<AppUser> signInManager, 
                             ITokenService tokenService)
    {
      _signInManager = signInManager;
      _userManager = userManager;
      _tokenService = tokenService;
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<UserDTO>> GetCurrentUserAsync()
    {
      var email = User.FindFirstValue(ClaimTypes.Email);
      var user = await _userManager.FindByEmailAsync(email);

      return new UserDTO
      {
        Email = user.Email,
        Token = _tokenService.CreateToken(user),
        DisplayName = user.DisplayName,
      };
    }

    [Authorize]
    [HttpGet("emailExists")]
    public async Task<ActionResult<bool>> CheckEmailExistsAsync([FromQuery] string email)
    {
      return await _userManager.FindByEmailAsync(email) != null;
    }

    [HttpGet("address")]
    public async Task<ActionResult<Address>> GetUserAddressAsync()
    {
      var email = User.FindFirstValue(ClaimTypes.Email);
      var user = await _userManager.FindByEmailAsync(email);

      return user.Address;
    }

    [HttpPost("login")]
    public async Task<ActionResult<UserDTO>> Login(LoginDTO loginDTO)
    {
      var user = await _userManager.FindByEmailAsync(loginDTO.Email);
      if (user == null) return Unauthorized(new ApiResponse(401));

      var result = await _signInManager.CheckPasswordSignInAsync(user, loginDTO.Password, false);
      if (!result.Succeeded) return Unauthorized(new ApiResponse(401));

      return new UserDTO
      {
        Email = user.Email,
        Token = _tokenService.CreateToken(user),
        DisplayName = user.DisplayName,
      };
    }

    [HttpPost("register")]
    public async Task<ActionResult<UserDTO>> Register(RegisterDTO registerDTO)
    {
      var user = new AppUser
      {
        DisplayName = registerDTO.DisplayName,
        Email = registerDTO.Email,
        UserName = registerDTO.Email
      };

      var result = await _userManager.CreateAsync(user, registerDTO.Password);
      if (!result.Succeeded) return BadRequest(new ApiResponse(400));

      return new UserDTO
      {
        DisplayName = user.DisplayName,
        Email = user.Email,
        Token = _tokenService.CreateToken(user)
      };
    }
  }
}