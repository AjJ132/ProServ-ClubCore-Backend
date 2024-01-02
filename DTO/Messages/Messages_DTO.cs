namespace ProServ_ClubCore_Server_API.DTO
{

    public class DirectMessage_DTO
    {
        public Guid? Conversation_ID { get; set; } //nullable because it is not required when sending data to the client //client cant receive messages from more than one conversation at a time/ Will be used when sending messages to the server
        public string Sender_ID { get; set; }
        public string? Sender_Name { get; set; }
        public string Message { get; set; }
        public DateTimeOffset Timestamp { get; set; }

        //TODO : Add a property for attachments not sure how I will do this
    }

    public class DirectConversation_DTO
    {
        public string? Conversation_Type { get; set; }
        public Guid? Conversation_ID { get; set; }
        public string User2_ID { get; set; }
        public string? User2_Name { get; set; }
        public string? LastMessageTimestamp { get; set; }
    }

    public class UserLookup_DTO
    {
        public string User_ID { get; set; }
        public string Name { get; set; }
    }
}
