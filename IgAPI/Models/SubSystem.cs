namespace IgAPI.Models
{
    public interface SubSystem
    
    {
        string SubSystemId { get; set; }
        string SubSystemName { get; set; }
        string ReferenceSystemId { get; set; }
        string SubSystemDescription { get; set; }
        bool SubSystemStatus { get; set; }   



    }
}
