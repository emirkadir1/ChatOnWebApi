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
        public ChatHub(UserDbContext context, IOpenAIService openAIService)
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
            }
            var friendList = await _context.Friends.Include(rt => rt.UsersFriendList).FirstOrDefaultAsync(u => u.User == user);
            if (friendList != null)
            {
                foreach (var item in friendList.UsersFriendList)
                {
                    if (item.ConnectionId != null)
                        await Clients.Clients(item.ConnectionId).SendAsync("IAmOffline", user.UserName);

                }
            }
        }
        //When user joins gets notfications
        public async Task GetNotifications(string userName)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u=>u.UserName== userName.Trim());
            if (user != null)
            {
                var notificationList = await _context.NotificationList.Include(rt => rt.UsersNotificationList).FirstOrDefaultAsync(u => u.User == user);
                await Clients.Caller.SendAsync("Notification", notificationList.UsersNotificationList.ToList());
            }
               
        }
        //When user clicks message room this method removes all message notifications
        public async Task DeleteMessageNotification(string userName)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            var notificationList = await _context.NotificationList.Include(rt=>rt.UsersNotificationList).FirstOrDefaultAsync(u => u.User == user);
            foreach (var item in notificationList.UsersNotificationList)
            {
                if (item.type == "message") 
                    notificationList.UsersNotificationList.Remove(item);
            }
            
            await Clients.Caller.SendAsync("GetNotification",notificationList.UsersNotificationList.ToList());
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

            }
            await _context.SaveChangesAsync();
            var friendList = await _context.Friends.Include(rt=>rt.UsersFriendList).FirstOrDefaultAsync(u => u.User == user);
            if (friendList != null)
            {
                //When user gets online this function makes all messages status 1.
                foreach (var item in friendList.UsersFriendList)
                {
                    List<Message> messages = new List<Message>();
                    {
                        foreach (var message in _context.Messages.ToList<Message>())
                        {

                            if (message.Sender.UserName == item.UserName && message.Reciver.UserName == user.UserName)
                            {
                                messages.Add(message);
                                if (message.IsRecived == 0)
                                    message.IsRecived = 1;
                            }
                            if (message.Sender.UserName == user.UserName && message.Reciver.UserName == item.UserName)
                            {
                                messages.Add(message);
                            }
                        }
                    }
                    
                    if (item.ConnectionId != null && item.LookingAt == user.UserName)
                    {
                        await Clients.Clients(item.ConnectionId).SendAsync("IAmOnline", user.UserName);
                        await Clients.Client(item.ConnectionId).SendAsync("ShowMessages", messages);
                    }

                    else if (item.ConnectionId != null)
                    {
                        await Clients.Clients(item.ConnectionId).SendAsync("IAmOnline", user.UserName);
                    }

                }
            }
            if (friendList != null)
                await Clients.Caller.SendAsync("Users", friendList.UsersFriendList.ToList());
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
                    CreatedTime = DateTime.Now.ToString("t")

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
                        Sender = _sender,
                        type = "message"
                    };
                    var notificationList = await _context.NotificationList.Include(rt=>rt.UsersNotificationList).FirstOrDefaultAsync(u => u.User == _reciver);
                    if(notificationList != null && _reciver.ConnectionId != null)
                    {
                        notificationList.UsersNotificationList.Add(notification);
                        await _context.SaveChangesAsync();
                        await Clients.Client(_reciver.ConnectionId).SendAsync("Notification", notificationList.UsersNotificationList.ToList());
                    }
                        
                }
                else
                {
                    message.IsRecived =0;
                    var notification = new Notification()
                    {
                        Sender = _sender,
                        type = "message"
                    };
                    var notificationList = await _context.NotificationList.FirstOrDefaultAsync(u => u.User == _reciver);
                    if (notificationList != null)
                        notificationList.UsersNotificationList.Add(notification);
                }
                _context.Messages.Add((Message)message);
                await _context.SaveChangesAsync();
                await Clients.Caller.SendAsync("ReceiveMessageMine", message.Sender, message.Body, message.CreatedTime, message.IsRecived,message.Translate );
                if (_reciver.Online && _reciver.ConnectionId is not null)
                {
                    await Clients.Client(_reciver.ConnectionId).SendAsync("ReceiveMessageFromOthers", message.Sender, message.Body, message.CreatedTime,message.Translate);
                }
                
            }
        }
        //When user clicks a user end gets a messages
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
                        item.IsRecived = 2;
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
        //User Profile
        public async Task GetUsersFriendList(string userName)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u =>u.UserName == userName.Trim());
            var friendlist = await _context.Friends.Include(rt=>rt.UsersFriendList).FirstOrDefaultAsync(u => u.User == user);
            if (friendlist != null)
                await Clients.Caller.SendAsync("FriendList", friendlist.UsersFriendList.ToList());
        }

    }
}
