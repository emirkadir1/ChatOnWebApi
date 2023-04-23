using ChatOnWebApi.Models;

namespace ChatOnWebApi.Interfaces
{
    public interface INewMessage
    {
        User Sender { get; set; }
        User Reciver { get; set; }
        string Body { get; set; }
        /// <summary>
        /// When new message item created it gets DateTime.Now
        /// </summary>
        string CreatedTime { get; set; }
        /// <summary>
        /// 0 Reciver is offline, 1 Reciver is online but not looking message, 2 Reciver read the message. Default 0
        /// </summary>
        byte IsRecived { get; set; }
    }
}
