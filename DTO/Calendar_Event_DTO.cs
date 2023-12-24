namespace ProServ_ClubCore_Server_API.DTO
{
    //SA stands for Single Assign. MA will be for multiple assign. GA will be for group assign. TA will be for team assign.
    public class Calendar_Event_SA_DTO
    {
        public string Event_ID { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public DateTimeOffset startDate { get; set; }
        public DateTimeOffset endDate { get; set; }
        public string color { get; set; }
    }

    public class Calendar_Event_MA_DTO
    {
        public string Event_ID { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public DateTimeOffset startDate { get; set; }
        public DateTimeOffset endDate { get; set; }
        public string color { get; set; }
        public List<string> users { get; set; } //will contain user ids
    }

    public class Calendar_Event_GA_DTO
    {
        public string Event_ID { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public DateTimeOffset startDate { get; set; }
        public DateTimeOffset endDate { get; set; }
        public string color { get; set; }
        public List<string> groups { get; set; } //will contain group ids
    }

    public class Calendar_Event_TA_DTO
    {
        public string Event_ID { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public DateTimeOffset startDate { get; set; }
        public DateTimeOffset endDate { get; set; }
        public string color { get; set; }
        public string team { get; set; } //will contain team id
    }
}
