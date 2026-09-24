using System.Formats.Asn1;

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


        // embedded_x => defence airborne  => Policy_1 => all patterns of enemy aircraft - POLICY IGNITEAUTH - intents, commands, targets

        // Cloud => Give resources across internet => Polcicy_2 4-10-20 => IgniteAUth => POLICY -> intents, commands, targets 

        // OS => 

        


       
    }
}
