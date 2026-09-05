namespace IgAPI.Models
{
    public interface Policy
    
    {
        int PolicyId { get; set; }
        string PolicyName { get; set; }

        int ReferenceSystemId { get; set; }

        int SubSystemId { get; set; }

        string Operation { get; set; }
        string[] CoreParameters { get; set; }

        string DecisionAction { get; set; }

        int Priority { get; set; }

        bool Status { get; set; }
    }
}
