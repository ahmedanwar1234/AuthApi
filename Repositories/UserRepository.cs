using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Crypto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserAccountAPI.DTOs;
using UserAccountAPI.Data;
using UserAccountAPI.Models;
using UserAccountAPI.Repositories.Interfaces;

namespace UserAccountAPI.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;

        public UserRepository(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IMapper mapper)
        {
            _context = context;
            _userManager = userManager;
            _mapper = mapper;
        }

        public async Task<UserDTO> GetUserByIdAsync(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            return _mapper.Map<UserDTO>(user);
        }

        public async Task<UserDTO> UpdateUserAsync(int userId, UpdateUserDTO model)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return null;
            }

            user.FirstName = model.FirstName ?? user.FirstName;
            user.LastName = model.LastName ?? user.LastName;

            await _userManager.UpdateAsync(user);
            return _mapper.Map<UserDTO>(user);
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return false;
            }

            user.IsActive = false;
            var result = await _userManager.UpdateAsync(user);

            return result.Succeeded;
        }

        public async Task<List<UserDTO>> GetAllUsersAsync(int page = 1, int pageSize = 10)
        {
            var users = await _userManager.Users
                .Where(u => u.IsActive)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return _mapper.Map<List<UserDTO>>(users);
        }
    }
}
