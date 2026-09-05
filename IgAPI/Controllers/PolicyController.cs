using Microsoft.AspNetCore.Mvc;
using System.Reflection.Metadata.Ecma335;
[ApiController]
[Route("api/[controller]")]

public class PolicyController : ControllerBase
{

   public PolicyController()
   {
       Console.WriteLine("PolicyController initialized.");
   }

}