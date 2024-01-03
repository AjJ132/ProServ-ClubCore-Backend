namespace ProServ_ClubCore_Server_API.DTO
{
    public class User_DTO
    {
        public string? User_ID { get; set; }
        public string Email { get; set; }
        public string First_Name { get; set; }
        public string Last_Name { get; set; }
        public bool isInTeam { get; set; }
        public string Team_Name { get; set; }
    }
}
