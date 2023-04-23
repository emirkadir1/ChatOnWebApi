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

    public class ChatHub : Hub
    {
        private readonly UserDbContext _context;
        public ChatHub(UserDbContext context)
        {
            _context = context;

        }

        public  async Task GetUserName(string userName)
        {
            var user = _context.Users.FirstOrDefault(u => u.UserName == userName.Trim());
            if (user != null)
            {
                user.Online = true;
                user.ConnectionId = Context.ConnectionId;
            }
            await _context.SaveChangesAsync();

            await Clients.All.SendAsync("Users", _context.Users.ToList());
        }

       public async Task SendMessage(string userName, string reciver, string text)
        {
            var _sender = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName.Trim());

            var _reciver = await _context.Users.FirstOrDefaultAsync(u => u.UserName == reciver.Trim());
            if (_sender != null && _reciver != null)
            {
                INewMessage message = new Message()
                {
                    Sender = _sender,
                    Reciver = _reciver,
                    Body = text,

                };
                _context.Messages.Add((Message)message);
                await Clients.Caller.SendAsync("ReceiveMessageMine", message.Sender, message.Body, message.CreatedTime);
                if (_reciver.Online && _reciver.ConnectionId is not null)
                {
                    await Clients.Client(_reciver.ConnectionId).SendAsync("ReceiveMessageFromOthers", message.Sender, message.Body, message.CreatedTime);
                }
                await _context.SaveChangesAsync();
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var user = _context.Users.FirstOrDefault(u => u.ConnectionId == Context.ConnectionId);
            if (user != null)
            {
                user.Online = false;
            }
            await Clients.All.SendAsync("users", _context.Users.ToList());
            await _context.SaveChangesAsync();
        }
        public async Task GetMessages(string myMessages, string hisMessages)
        {
            var _me = _context.Users.FirstOrDefault(u => u.UserName == myMessages.Trim());
            var _him = _context.Users.FirstOrDefault(t => t.UserName == hisMessages.Trim());
            List<Message> messages = new List<Message>();
            if (_me is not null && _him is not null)
            {
                _me.LookingAt = _him.UserName;
                foreach (var item in _context.Messages.ToList<Message>())
                {
                    if (item.Sender.UserName == _me.UserName && item.Reciver.UserName == _him.UserName)
                    {
                        messages.Add(item);
                    }
                    if (item.Sender.UserName == _him.UserName && item.Reciver.UserName == _me.UserName)
                    {
                        messages.Add(item);
                    }
                }
            }
            await Clients.Caller.SendAsync("ShowMessages", messages);
            await _context.SaveChangesAsync();
        }

    }
}
