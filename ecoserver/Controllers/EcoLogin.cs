using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using webapi.Services.DatabaseService;
using webapi.Services;
using webapi.Utilities;
using System.Diagnostics;

namespace webapi.Controllers 
{
    [Route("service/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase 
    {
        //private readonly IDatabaseService _serviceDatabase;
        //private readonly TeensyBackgroundService _teensyService;
        private readonly IServiceProvider _serviceProvider;

        public AuthController(/*IDatabaseService databaseService,*/ IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            //_serviceDatabase = databaseService;
        }

        [HttpGet("test")]
        public async Task<IActionResult> Test(string? test)
        {
            Debug.WriteLine("called");
            Debug.WriteLine("string");
            return Ok();
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel loginModel)
        {
            //List<EcodroneUsers> ecodrones = await _serviceDatabase.FetchData();

            /*if (ecodrones.Count == 0)
            {
                return BadRequest();
            }*/

            /*EcodroneUsers ecodrone = ecodrones.SingleOrDefault(x => x.Identification == loginModel.Username && x.ReturnHashedPassword(loginModel.Password), new EcodroneUsers(0));

            if (ecodrone.Id != 0)
            {
                var genKey = TokenValidation.Generate256BitKey();

                var token = TokenValidation.GenerateJwtToken(ecodrone.Identification, genKey);

                await _serviceDatabase.SaveUserToken(ecodrone.Identification, token, genKey);

                return Ok(new { Token = token });

            }else
            {
                return BadRequest();
            }*/

            return Ok(new { Token = "ecodrone" });
            
        }

        


    }
}
