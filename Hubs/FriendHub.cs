using ChatOnWebApi.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.VisualBasic;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography.X509Certificates;
using Microsoft.EntityFrameworkCore;
using ChatOnWebApi.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Azure;
using System.Text;
using ChatOnWebApi.Tokens;
using ChatOnWebApi.Services;
using Newtonsoft.Json.Linq;

namespace ChatOnWebApi.Hubs
{

    public class FriendHub : Hub
    {
        private readonly UserDbContext _context;
        public FriendHub(UserDbContext context)
        {
            _context = context;

        }
        public  async Task GetUserName(string userName)
        {
            var user = _context.Users.FirstOrDefault(u => u.UserName == userName.Trim());
            var friendList = _context.Friends.Include(rt => rt.User).FirstOrDefault(u => u.User.Id == user.Id);
            if (user != null)
            {
                user.Online = true;
                user.ConnectionId = Context.ConnectionId;
            }
            await _context.SaveChangesAsync();
            await Clients.All.SendAsync("Users", _context.Users.ToList(),friendList.UsersFriendList.ToList());
        }
        public async Task AddFriend(string sender , string reciever)
        {
            await Clients.Caller.SendAsync("Add-Friend");
        }
        public async Task RemoveFriend(string sender, string reciever)
        {
            await Clients.Caller.SendAsync("Add-Friend");
        }
        public async Task AcceptFriend(string sender, string reciever)
        {
            await Clients.Caller.SendAsync("Add-Friend");
        }
        public async Task RemoveRequest(string sender, string reciever)
        {
            var a = "";
            await Clients.Caller.SendAsync("Add-Friend");
        }
    }
}
