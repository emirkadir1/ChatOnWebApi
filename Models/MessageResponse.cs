namespace ChatOnWebApi.Models
{
    public class MessageResponse
    {
        public string Sender { get; set; } =string.Empty;
        public string Reciver { get; set; } =string.Empty;
        public string Body { get; set; } = string.Empty;
        public string CreatedTime { get; set; } =string.Empty;
        public string Translate { get; set; } = string.Empty;
        public byte IsRecived { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
    }
}
