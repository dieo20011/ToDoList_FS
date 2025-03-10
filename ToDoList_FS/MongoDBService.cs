﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ToDoList_FS.Controllers;
using ToDoList_FS.Model;

namespace ToDoList_FS
{
    public class MongoDBService 
    {
        private readonly IMongoCollection<TodoItem> _todoItems;
        private readonly IMongoCollection<User> _users;
        private readonly string _jwtSecret = "banhxeo0210_abc1234567890abcdef";  // 128 bits (16 bytes)

        public MongoDBService(IMongoClient mongoClient)
        {
            var database = mongoClient.GetDatabase("Todolist_Paging");
            _todoItems = database.GetCollection<TodoItem>("Paging");
            _users = database.GetCollection<User>("Users");
        }
        public async Task<User?> GetUserById(string UserId)
        {
            return await _users.Find(u => u.Id == UserId).FirstOrDefaultAsync();
        }
        public async Task<bool> UpdateUser(string id, string newFullname, string newUsername)
        {
            var user = await _users.Find(u => u.Id == id).FirstOrDefaultAsync();
            if (user == null)
            {
                return false;
            }
            var existingUserName = await _users.Find(u => u.UserName == newUsername && u.Id != id).FirstOrDefaultAsync();
            if(existingUserName != null)
            {
                return false;
            }
            var update = Builders<User>.Update
                .Set(u => u.FullName, newFullname)
                .Set(u => u.UserName, newUsername);
            var result = await _users.UpdateOneAsync(u => u.Id == id, update);
            return result.ModifiedCount > 0;
        }
        public async Task<bool> UpdatePassword(string id, string oldPassword, string newPassword)
        {
            var user = await _users.Find(u => u.Id == id).FirstOrDefaultAsync();
            if (user == null) return false;
            if(!BCrypt.Net.BCrypt.Verify(oldPassword, user.Password))
            {
                return false;
            }

            string hashPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);
            var update = Builders<User>.Update.Set(u => u.Password, hashPassword);
            var result = await _users.UpdateOneAsync(u => u.Id == id, update);
            return result.ModifiedCount > 0;
        }
        public async Task<string> RegisterUser(string username, string password, string fullname)
        {
            var existingUser = await _users.Find(u => u.UserName == username).FirstOrDefaultAsync();
            if (existingUser != null)
            {
                return "Username đã tồn tại";
            }

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
            var user = new User { UserName = username, Password = hashedPassword, FullName = fullname };
            await _users.InsertOneAsync(user);

            return GenerateJwtToken(user);
        }
        public async Task<string> Login(string username, string password)
        {
            var existingUser = await _users.Find(u => u.UserName == username).FirstOrDefaultAsync();
            if(existingUser == null || !BCrypt.Net.BCrypt.Verify(password, existingUser.Password)) {
                return "Invalid username or password";
            }
            return GenerateJwtToken(existingUser);
        }

        private string GenerateJwtToken(User user)
        {
            var key = _jwtSecret.Length < 32
                ? Encoding.UTF8.GetBytes(PadOrGenerateKey(_jwtSecret))
                : Encoding.UTF8.GetBytes(_jwtSecret);

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) // Add user ID for more context
        }),
                Expires = DateTime.UtcNow.AddHours(3),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string PadOrGenerateKey(string originalKey)
        {
            if (string.IsNullOrEmpty(originalKey))
            {
                return Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
            }

            if (originalKey.Length < 32)
            {
                return originalKey.PadRight(32, 'X');
            }

            return originalKey;
        }
        //Task
        public async Task<List<TodoItem>> GetTodoList(string UserId)
        {
            return await _todoItems.Find(todo => todo.UserId == UserId).ToListAsync();
        }

        public async Task AddTask(TodoItem item)
        {
            await _todoItems.InsertOneAsync(item);
        }
        public async Task UpdateTask(string id, TodoItem item)
        {
            var filter = Builders<TodoItem>.Filter.Eq(todo => todo.Id, id);
            var request = Builders<TodoItem>.Update
                .Set(todo => todo.Title, item.Title)
                .Set(todo => todo.Status, item.Status)
                .Set(todo => todo.Description, item.Description)
                .Set(todo => todo.FromDate, item.FromDate)
                .Set(todo => todo.ToDate, item.ToDate);
            await _todoItems.UpdateOneAsync(filter, request);
        }
        public async Task DeleteTask(string id)
        {
           var filter = Builders<TodoItem>.Filter.Eq(todo => todo.Id, id);
           await _todoItems.DeleteOneAsync(filter);
        }
    }
}
