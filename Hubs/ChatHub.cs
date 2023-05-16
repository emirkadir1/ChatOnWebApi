using ChatOnWebApi.Models;
using Microsoft.AspNetCore.SignalR;
using ChatOnWebApi.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.ObjectModels.ResponseModels;
using OpenAI.GPT3.Managers;

namespace ChatOnWebApi.Hubs
{
    public class ChatHub : Hub
    {
        private readonly UserDbContext _context;
        private readonly IOpenAIService _openAIService;
        public ChatHub(UserDbContext context,UserDbContext context2, IOpenAIService openAIService)
        {
            _context = context;
            _openAIService = openAIService;

        }
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.ConnectionId == Context.ConnectionId);
            if (user != null) 
            {
                user.LookingAt = "";
                user.ConnectionId = "";
                user.Online = false;
                await _context.SaveChangesAsync();
                foreach (var item in _context.Friends)
                {
                    if (user == item.FromUserId && item.isAccepted)
                    {
                        var friend = await _context.Users.FirstOrDefaultAsync(u => u == item.ToUserId);
                        if (friend != null) 
                            await Clients.Clients(friend.ConnectionId).SendAsync("IAmOffline", user.UserName);
                    }
                    if (user == item.ToUserId && item.isAccepted)
                    {
                        var friend = await _context.Users.FirstOrDefaultAsync(u => u == item.FromUserId);
                        if (friend != null)
                            await Clients.Clients(friend.ConnectionId).SendAsync("IAmOffline", user.UserName);
                    }
                }
                
            }

        }
        //When user joins gets notfications
        public async Task GetNotifications(string userName)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u=>u.UserName== userName.Trim());
            if (user != null)
            {
                user.Online = true;
                user.ConnectionId = Context.ConnectionId;
                var notificationList = await _context.NotificationList.Include(rt => rt.UsersNotificationList).FirstOrDefaultAsync(u => u.User == user);
                await Clients.Caller.SendAsync("Notification", notificationList.UsersNotificationList.ToList());
                foreach (var item in _context.Messages)
                {
                    if (item.Reciver == user)
                        if (item.IsRecived == 0)
                            item.IsRecived = 1;
                }
                await _context.SaveChangesAsync();
                foreach (var item in _context.Users)
                {
                    if (item.LookingAt == user.UserName)
                        await Clients.Client(item.ConnectionId).SendAsync("TheUserLookingAtGetsOnline");
                }
            }
               
        }
        //When user clicks message room this method removes all message notifications
        public async Task DeleteMessageNotification(string userName,string targetUser)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            var notificationList = await _context.NotificationList.Include(rt=>rt.UsersNotificationList).FirstOrDefaultAsync(u => u.User == user);
            for (int i = notificationList.UsersNotificationList.Count - 1; i >= 0; i--)
            {
                if (notificationList.UsersNotificationList[i].type == "message"
                    && notificationList.UsersNotificationList[i].Sender == targetUser.Trim())
                {
                    notificationList.UsersNotificationList.RemoveAt(i);
                }
            }
            await _context.SaveChangesAsync();
            await Clients.Caller.SendAsync("Notification", notificationList.UsersNotificationList.ToList());
            await Clients.Caller.SendAsync("DeleteNotification", targetUser);
        }
        //When user clicks add friend room this method removes all message notifications
        public async Task DeleteFriendNotification(string userName)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            var notificationList = await _context.NotificationList.Include(rt => rt.UsersNotificationList).FirstOrDefaultAsync(u => u.User == user);
            foreach (var item in notificationList.UsersNotificationList)
            {
                if (item.type == "friend")
                    notificationList.UsersNotificationList.Remove(item);
            }
            await Clients.Caller.SendAsync("GetNotification",notificationList.UsersNotificationList.ToList());
        }
        //When user joins a message room
        public  async Task GetUserName(string userName)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName.Trim());
            if (user != null)
            {
                user.Online = true;
                user.ConnectionId = Context.ConnectionId;
                user.LookingAt="";

            }
            List<User> friends = new List<User>();
            if (user != null)
            {
                foreach (var item in _context.Friends.Include(rt => rt.FromUserId).Include(rt => rt.ToUserId))
                {
                    if (user == item.ToUserId && item.isAccepted)
                    {
                     friends.Add(item.FromUserId);
                    }
                    if (user == item.FromUserId && item.isAccepted)
                    {
                     friends.Add(item.ToUserId);
                    }
                }
                if (friends != null)
                    await Clients.Caller.SendAsync("FriendList", friends.ToList());
            }
            var notificationList = await _context.NotificationList.Include(rt => rt.UsersNotificationList).FirstOrDefaultAsync(u => u.User == user);
            if (friends != null)
                await Clients.Caller.SendAsync("Users", friends.ToList(),notificationList.UsersNotificationList.ToList());
        }
       public async Task SendMessage(string userName, string reciver, string text,bool isTranslate)
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
                    CreatedTime = DateTime.Now

                };
                //Send message to chat gpt
                if (isTranslate)
                {
                    var question = $"${message.Body}. Which language is this sentence? Return only language code";
                    CompletionCreateResponse result = await _openAIService.Completions.CreateCompletion(new CompletionCreateRequest()
                    {
                        Prompt = question,
                        MaxTokens = 10
                    }, OpenAI.GPT3.ObjectModels.Models.TextDavinciV3);
                    string str = result.Choices[0].Text;
                    str = str.Replace("\n", ""); // "\nen"
                    str = str.Replace("\n", ""); // "en"
                    if (_reciver.LanguageCode.ToLower() != str.ToLower())
                    {
                        var translate = $"${message.Body}. Translate this sentence to {_reciver.LanguageCode}. Return only sentence";
                        CompletionCreateResponse result2 = await _openAIService.Completions.CreateCompletion(new CompletionCreateRequest()
                        {
                            Prompt = translate,
                            MaxTokens = 100
                        }, OpenAI.GPT3.ObjectModels.Models.TextDavinciV3);
                        message.Translate = result2.Choices[0].Text;
                    }
                }                
                if (_reciver.Online && _reciver.LookingAt==_sender.UserName)
                {
                    message.IsRecived = 2;
                }
                else if (_reciver.Online)
                {
                    message.IsRecived = 1;
                    var notification = new Notification()
                    {
                        Sender = _sender.UserName,
                        type = "message"
                    };
                    var notificationList = await _context.NotificationList.Include(rt=>rt.UsersNotificationList).FirstOrDefaultAsync(u => u.User == _reciver);
                    if(notificationList != null && _reciver.ConnectionId != null)
                    {
                        notificationList.UsersNotificationList.Add(notification);
                        await _context.SaveChangesAsync();
                        await Clients.Client(_reciver.ConnectionId).SendAsync("Notification", notificationList.UsersNotificationList.ToList());
                        await Clients.Client(_reciver.ConnectionId).SendAsync("GetNotification", _sender.UserName);
                    }
                        
                }
                else
                {
                    message.IsRecived =0;
                    var notification = new Notification()
                    {
                        Sender = _sender.UserName,
                        type = "message"
                    };
                    var notificationList = await _context.NotificationList.FirstOrDefaultAsync(u => u.User == _reciver);
                    if (notificationList != null)
                        notificationList.UsersNotificationList.Add(notification);
                }
                _context.Messages.Add((Message)message);
                await _context.SaveChangesAsync();
                await Clients.Caller.SendAsync("ReceiveMessageMine", message.Sender, message.Body, message.CreatedTime.ToString("t"), message.IsRecived,message.Translate );
                if (_reciver.Online && _reciver.ConnectionId is not null)
                {
                    await Clients.Client(_reciver.ConnectionId).SendAsync("ReceiveMessageFromOthers", message.Sender, message.Body, message.CreatedTime.ToString("t"),message.Translate);
                }
                
            }
        }
        //When user clicks a user end gets a messages
        public async Task GetMessages(string myMessages, string hisMessages)
        {
            var _me = _context.Users.FirstOrDefault(u => u.UserName == myMessages.Trim());
            var _him = _context.Users.FirstOrDefault(t => t.UserName == hisMessages.Trim());
            List<MessageResponse> messages = new List<MessageResponse>();
            if (_me is not null && _him is not null)
            {
                _me.LookingAt = _him.UserName;
                foreach (var item in _context.Messages.ToList<Message>())
                {
                    if (item.Sender.UserName == _me.UserName && item.Reciver.UserName == _him.UserName)
                    {
                        MessageResponse response = new MessageResponse();
                        response.Sender = item.Sender.UserName;
                        response.Body=item.Body;
                        response.Reciver = item.Reciver.UserName;
                        response.Translate = item.Translate;
                        response.IsRecived= item.IsRecived;
                        response.CreatedTime = item.CreatedTime.ToString("t");
                        response.ImageUrl = _me.ImageUrl;
                        messages.Add(response);
                    }
                    if (item.Sender.UserName == _him.UserName && item.Reciver.UserName == _me.UserName)
                    {                        
                        item.IsRecived = 2;
                        MessageResponse response = new MessageResponse();
                        response.Sender = item.Sender.UserName;
                        response.Body = item.Body;
                        response.Reciver = item.Reciver.UserName;
                        response.Translate = item.Translate;
                        response.IsRecived = item.IsRecived;
                        response.CreatedTime = item.CreatedTime.ToString("t");
                        response.ImageUrl = _him.ImageUrl;
                        messages.Add(response);
                    }
                }
                await _context.SaveChangesAsync();
                await Clients.Caller.SendAsync("ShowMessages", messages);
                if (_him.ConnectionId != null && _him.Online)
                    await Clients.Client(_him.ConnectionId).SendAsync("ShowMessages", messages);
            }

        }
        //Add friend
        public async Task GetUserList(string userName)
        {
            var user = _context.Users.FirstOrDefault(u => u.UserName == userName.Trim());
            if (user != null)
            {
                user.Online = true;
                user.ConnectionId = Context.ConnectionId;
            }
            await _context.SaveChangesAsync();
            List<User> friends = new List<User>();
            List<User>sendedRequests = new List<User>();
            List<User>recievedRequests = new List<User>();
            if (user != null)
            {
                foreach (var item in _context.Friends.Include(rt=>rt.FromUserId).Include(rt=>rt.ToUserId))
                {
                    if (user == item.ToUserId && item.isAccepted)
                    {
                        friends.Add(item.FromUserId);
                    }
                    else if(user==item.ToUserId && !item.isAccepted)
                    {
                        recievedRequests.Add(item.FromUserId);
                    }
                    if (user == item.FromUserId && item.isAccepted)
                    {
                        friends.Add(item.ToUserId);
                    }
                    else if (user == item.FromUserId && !item.isAccepted)
                    {
                        sendedRequests.Add(item.ToUserId);
                    }
                }
                if (friends != null)
                    await Clients.Caller.SendAsync("FriendList", friends.ToList());
            }
            if(friends != null)
            await Clients.Caller.SendAsync("Users", _context.Users.ToList(),friends.ToList(),sendedRequests.ToList(),recievedRequests.ToList());
        }
        public async Task AddFriend(string sender, string reciever)
        {
            var _sender = await _context.Users.FirstOrDefaultAsync(u => u.UserName == sender.Trim());
            var _reciever = await _context.Users.FirstOrDefaultAsync(u => u.UserName == reciever.Trim());
            //Find Both user and create new friend request
            if (_sender != null && _reciever != null)
            {
                FriendList friendRequest = new FriendList()
                {
                    FromUserId = _sender,
                    ToUserId = _reciever,
                    reqDate = DateTime.Now,
                    isAccepted = false
                };
                _context.Friends.Add(friendRequest);
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
            if(_sender!= null && _reciever != null)
            {
                var friendRequest = await _context.Friends.FirstOrDefaultAsync(u => u.FromUserId == _sender && u.ToUserId == _reciever);
                if (friendRequest != null)
                {
                    friendRequest.isAccepted = true;
                    await _context.SaveChangesAsync();
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
                var friendRequest = await _context.Friends.FirstOrDefaultAsync(u => u.FromUserId == _sender&& u.ToUserId == _reciever);
                if (friendRequest != null)
                {
                    _context.Friends.Remove(friendRequest);
                    await _context.SaveChangesAsync();
                }
                if (_reciever.ConnectionId != null)
                        await Clients.Client(_reciever.ConnectionId).SendAsync("RemoveFriend", _sender.UserName);
                
            }

        }

        public async Task RemoveRequest(string sender, string reciever)
        {
            var _sender = await _context.Users.FirstOrDefaultAsync(u => u.UserName == sender.Trim());
            var _reciever = await _context.Users.FirstOrDefaultAsync(u => u.UserName == reciever.Trim());
            //Find Friend Request
            if (_sender != null && _reciever != null)
            {
                var friendRequest = await _context.Friends.FirstOrDefaultAsync(u => u.FromUserId == _sender && u.ToUserId == _reciever);
                if (friendRequest != null)
                {
                    _context.Friends.Remove(friendRequest);
                    await _context.SaveChangesAsync();
                }
                if (_reciever.ConnectionId != null)
                    await Clients.Client(_reciever.ConnectionId).SendAsync("RemoveRequest", _sender.UserName);
            }                      
        }
        //User Profile
        public async Task GetUsersFriendList(string userName)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u =>u.UserName == userName.Trim());
            List<User> friends = new List<User>();
            if(user != null)
            {
                foreach (var item in _context.Friends.Include(rt => rt.FromUserId).Include(rt => rt.ToUserId))
                {
                    if (user == item.ToUserId && item.isAccepted)
                    {
                    friends.Add(item.FromUserId);    
                    }
                    if (user == item.FromUserId && item.isAccepted)
                    {
                    friends.Add(item.ToUserId);
                    }
                }
                if (friends != null)
                    await Clients.Caller.SendAsync("FriendList", friends.ToList());
            }
     
        }

    }
}
