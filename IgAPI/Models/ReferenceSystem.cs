namespace IgAPI.Models

{

    // This is the General Information for Displaying system status
    public interface ReferenceSystem
    {
        int ReferenceSystemId { get; set; }
        string SystemName { get; set; }
        string SystemType { get; set; }
        string SystemVersion { get; set; }
        string Description { get; set; }
        string Status { get; set; }
        DateTime CreatedAt { get; set; }



    }
}
