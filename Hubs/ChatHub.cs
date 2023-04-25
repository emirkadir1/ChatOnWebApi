using ChatOnWebApi.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.VisualBasic;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography.X509Certificates;
using ChatOnWebApi.Interfaces;
using System.Text;
using ChatOnWebApi.Tokens;
using ChatOnWebApi.Services;
using Microsoft.EntityFrameworkCore;

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
            var friendList = await _context.Friends.Include(rt=>rt.UsersFriendList).FirstOrDefaultAsync(u => u.User == user);
            if(friendList != null)
                await Clients.Caller.SendAsync("Users", friendList.UsersFriendList.ToList());
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

        /*public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var user = _context.Users.FirstOrDefault(u => u.ConnectionId == Context.ConnectionId);
            if (user != null)
            {
                user.Online = false;
            }
            await Clients.All.SendAsync("users", _context.Users.ToList());
            await _context.SaveChangesAsync();
        }*/
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
        public async Task GetUserList(string userName)
        {
            var user = _context.Users.FirstOrDefault(u => u.UserName == userName.Trim());
            if (user != null)
            {
                user.Online = true;
                user.ConnectionId = Context.ConnectionId;
            }
            await _context.SaveChangesAsync();
            var friendList = await _context.Friends.FirstOrDefaultAsync(u => u.User == user);
            var friendRequests = await _context.FriendRequests.Where(u => u.Sender == user || u.Reciever == user).ToListAsync();

            await Clients.Caller.SendAsync("Users", _context.Users.ToList(), friendList.UsersFriendList.ToList(), friendRequests);
        }
        public async Task AddFriend(string sender, string reciever)
        {
            var _sender = await _context.Users.FirstOrDefaultAsync(u => u.UserName == sender.Trim());
            var _reciever = await _context.Users.FirstOrDefaultAsync(u => u.UserName == reciever.Trim());
            //Find Both user and create new friend request
            if (_sender != null && _reciever != null)
            {
                FriendRequests friendRequest = new FriendRequests()
                {
                    Sender = _sender,
                    Reciever = _reciever,
                };
                _context.FriendRequests.Add(friendRequest);
                await _context.SaveChangesAsync();
                //Send sender to 
                if (_reciever.ConnectionId != null)
                    await Clients.Client(_reciever.ConnectionId).SendAsync("AcceptButton", _sender.UserName);
            }
        }
        public async Task AcceptFriend(string sender, string reciever)
        {
            var _sender = await _context.Users.FirstOrDefaultAsync(u => u.UserName == sender.Trim());
            var _reciever = await _context.Users.FirstOrDefaultAsync(u => u.UserName == reciever.Trim());
            //Find Friend Request
            var friendRequest = await _context.FriendRequests.FirstOrDefaultAsync(u => u.Sender == _sender && u.Reciever == _reciever);
            if (friendRequest != null && _sender != null && _reciever != null)
            {
                //Delete Request because we dont need anymore
                _context.FriendRequests.Remove(friendRequest);
                //Find Both users friend list and add their friendlist eachother
                var senderFriendList = await _context.Friends.FirstOrDefaultAsync(u => u.User == _sender);
                var recieverFriendList = await _context.Friends.FirstOrDefaultAsync(u => u.User == _reciever);
                if (senderFriendList != null && recieverFriendList != null)
                {
                    senderFriendList.UsersFriendList.Add(_reciever);
                    recieverFriendList.UsersFriendList.Add(_sender);
                    await _context.SaveChangesAsync();
                    //Send  client to AcceptFriend method
                    if (_sender.ConnectionId != null)
                        await Clients.Client(_sender.ConnectionId).SendAsync("AcceptFriend", _reciever.UserName);
                }

            }

        }
        public async Task RemoveFriend(string sender, string reciever)
        {
            var _sender = await _context.Users.FirstOrDefaultAsync(u => u.UserName == sender.Trim());
            var _reciever = await _context.Users.FirstOrDefaultAsync(u => u.UserName == reciever.Trim());
            //Find Both users friend list and remove their friendlist eachother
            if (_sender != null && _reciever != null)
            {
                var senderFriendList = await _context.Friends.FirstOrDefaultAsync(u => u.User == _sender);
                var recieverFriendList = await _context.Friends.FirstOrDefaultAsync(u => u.User == _reciever);
                if (senderFriendList != null && recieverFriendList != null)
                {
                    senderFriendList.UsersFriendList.Remove(_reciever);
                    recieverFriendList.UsersFriendList.Remove(_sender);
                    await _context.SaveChangesAsync();
                    if (_reciever.ConnectionId != null)
                        await Clients.Client(_reciever.ConnectionId).SendAsync("RemoveFriend", _sender.UserName);
                }
            }

        }

        public async Task RemoveRequest(string sender, string reciever)
        {
            var _sender = await _context.Users.FirstOrDefaultAsync(u => u.UserName == sender.Trim());
            var _reciever = await _context.Users.FirstOrDefaultAsync(u => u.UserName == reciever.Trim());
            //Find Friend Request
            var friendRequest = await _context.FriendRequests.FirstOrDefaultAsync(u => u.Sender == _sender && u.Reciever == _reciever);
            if (friendRequest != null && _sender != null && _reciever != null)
            {
                _context.FriendRequests.Remove(friendRequest);
                await _context.SaveChangesAsync();
                await Clients.Caller.SendAsync("RemoveRequest");
                if (_reciever.ConnectionId != null)
                    await Clients.Client(_reciever.ConnectionId).SendAsync("RemoveRequest", _sender.UserName);
            }

        }
    }
}
