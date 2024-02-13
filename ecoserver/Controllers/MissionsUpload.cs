using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;

namespace webapi
{
    [Route("service/[controller]")]
    [ApiController]
    public class MissionsUploadController : Controller
    {
        /*public IActionResult Index()
        {
            return View();
        }*/
        

        [HttpPost("Receive")]
        public IActionResult ReceiveMissionData([FromBody] MissionDataPayload payload)
        {
            // Handle the payload here
            // For example:
            var missionParam = payload.MissionParam;
            var pointsList = payload.PointsList;

            var data = JsonConvert.SerializeObject(payload);
            Debug.WriteLine(data.Length);
            // ... do something with the data

            return Ok(new { Message = "Data received successfully!" });
        }
    }


    public class MissionDataPayload
    {
        public SMissionParam MissionParam { get; set; }
        public List<Point> PointsList { get; set; }
    }

    public class SMissionParam
    {
        public string IdMission { get; set; }
        public UInt16 MissionNumber { get; set; }
        public UInt16 TotalWayPoint { get; set; }
        public UInt16 WpStart { get; set; }
        public UInt16 Cycles { get; set; }
        public UInt16 WpEnd { get; set; }
        public ushort NMmode { get; set; }
        public UInt16 NMnum { get; set; }
        public UInt16 NMstart { get; set; }
        public string IdMissionNext { get; set; }
        public float StandRadius { get; set; }

    }


    public class Point
    {
        public double Lng { get; set; }
        public double Lat { get; set; }
        public ushort Navmode { get; set; }
        public ushort PointType { get; set; }
        public ushort Mon {  get; set; }
        public ushort Amode { get; set; }
        public float Wrad {  get; set; }
    }
}




/*public string idMission = ""; //Mission ID
public UInt16 nMission; //Mission number
public UInt16 total_mission_nWP; //Number of waypoints of current mission
public UInt16 wpStart;//Start waypoint of repetition cycle
public UInt16 cycles; //How many execution cycles of the current mission
public UInt16 wpEnd;  //Final waypoint of repetition cycle
public byte NMmode; //What to do next (0,1,2,3,4) - 0: nessuna missione; 1: da fine cicla vai alla successiva;
                    //  2: dopo tutti i waypoint vai alla successiva; 3: all'ultimo WP del ciclo stazioni in un certo raggio fino a nuovo ordine
                    // 4: staziona all'ultimo WP della missione
public UInt16 NMnum;  //Number of the next mission
public UInt16 NMStartInd; //Start waypoint of next mission

public string idMissionNext = ""; //String with the path of the mission
public float standRadius; //WP standing mode radius
*/



