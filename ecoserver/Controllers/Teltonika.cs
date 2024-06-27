using Microsoft.AspNetCore.Mvc;

public class RequestData
{
    public string HeaderData { get; set; }
}


namespace webapi
{
    [Route("service/[controller]")]
    [ApiController]
    public class Teltonika : ControllerBase
    {
        [HttpPost]
        public IActionResult Post([FromBody] RequestData requestData)
        {
            if (requestData == null)
            {
                return BadRequest("Invalid request data.");
            }

            // Process the request data here
            var headerData = requestData.HeaderData;

            // For now, just return the received data
            return Ok(new { received = headerData });
        }
    }
}
