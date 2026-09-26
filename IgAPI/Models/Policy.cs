namespace IgAPI.Models
{
    public class Policy
    
    {
        public int PolicyId { get; set; }
        public string PolicyName { get; set; } = string.Empty;
        public string PolicyType { get; set; } = string.Empty;

        public int ReferenceSystemId { get; set; }

        public List<string> AllowedIntents { get; set; } = new();
        public List<string> AllowedCommands { get;set; } = new();
        public List<string> AllowedTargets { get; set; } = new();
    }


  }

