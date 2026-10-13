namespace RaceDay.API.Models
{
    public class Result
    {
        public int ResultID { get; set; }
        public int EnrolmentID { get; set; }
        public TimeSpan CompletionTime { get; set; }
        public int RankPosition { get; set; }
        public DateTime RecordedAt { get; set; } = DateTime.Now;

        public Enrolment Enrolment { get; set; } = null!;
    }
}