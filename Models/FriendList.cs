namespace ChatOnWebApi.Models
{
    public class FriendList
    {
        public int Id { get; set; }
        public User FromUserId { get; set; } = new User();
        public User ToUserId { get; set; } = new User();
        public DateTime reqDate { get; set; }
        public DateTime startDate { get; set; }
        public bool isAccepted { get; set; } // istek geldi, istek gönderildi,istek red edildi,...


    }
}
