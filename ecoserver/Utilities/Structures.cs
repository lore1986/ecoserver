using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Channels;
namespace webapi
{

    struct TeensyMessage
    {
        public byte lengCmd = 0;
        public byte sorgCmd = 0;
        public byte destCmd = 0;
        public byte id_dCmd = 0;
        public byte Cmd1 = 0;
        public byte Cmd2 = 0;
        public byte Cmd3 = 0;

        public TeensyMessage(byte[] data)
        {
            lengCmd = data[0];
            sorgCmd = data[1];
            destCmd = data[2];
            id_dCmd = data[3];
            Cmd1 = data[4];
            Cmd2 = data[5];
            Cmd3 = data[6];
        }
    }

    struct ParamId
    {
        public byte[] Boat;

        // Constructor to initialize the Boat array
        public ParamId()
        {
            Boat = new byte[] { 0x10, 0x11, 0x12 };
        }
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    class  ImuData
    {
        public float Yaw { get; set; } = 0;
        public float Pitch { get; set; } = 0;
        public float Roll { get; set; } = 0;
        public float Ax { get; set; } = 0;
        public float Ay { get; set; } = 0;
        public float Az { get; set; } = 0;
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct MissionParamIn
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public char[] idMission; //Mission ID
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
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public char[] idMissionNext; //String with the path of the mission
        public float standRadius; //WP standing mode radius
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct WayPoint
    {
        public UInt16 Nmissione;// Nome del file contenente il waypoint 
        public UInt16 IndexWP;// Indice dell'attuale waypoint
        public float Latitude;// Latitudine attuale WP
        public float Longitude;// Latitudine attuale WP
        public byte NavMode;// [0 - 9]: definita come velocità eprm base all'indice [NavMode] dell'array di 10 velocità -> modifica erpm_base dei motori
                              //************* // [10 - 19]: definita come velocità gps all'indice [NavMode] dell'array di 10 velocità -> modifica rif. di velocità gps
                              //************* // [20 - 29]: definita come compensazione di energia all'indice [NavMode] dell'array di 10 livelli -> modifica erpm_max/min dei motori
        public byte PointType; // 0: fermo; 1: non fermo; 2: fa parte di un'area; 3: stazionamento in un raggio
        public byte MonitoringOp;//[1 - 125]: ti fermi ed effettui monitoraggio; 126 - 255: non ti fermi ed effettui monitoraggio; 0: non ti fermi e vai avanti
        public byte ArriveMode;// [0 - 49]: senza vincoli intermedi
                                 //*****************// [50 - 99]: algoritmo punti intermedi con retta cartesiana
                                 //*****************// [100 - 149]: algoritmo punti intermedi con emisenoverso
        public float WaypointRadius; //Raggio entro il quale passare al WP successivo
                              //16.600.000 waypoint totali sulla flash
    }

    public class MissionParam
    {
        public string idMission = ""; //Mission ID char idMission[32]; //Mission ID
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

        public string idMissionNext = ""; //String with the path of the mission char idMission[32]; //Mission ID
        public float standRadius; //WP standing mode radius 
    }

    public struct UploadMissionHeader
    {
        public string IdMission;
        public ushort MissionNumber;
        public ushort TotalWayPoint; 
        public ushort WpStart; 
        public ushort Cycles; 
        public ushort WpEnd; 
        public byte NMmode; 
        public ushort NMnum; 
        public ushort NMstart; 
        public string IdMissionNext;
        public float StandRadius; 
    }
    public struct UploadWaypoint
    {
        public float lng {get; set;}
        public float lat {get; set;}
        public byte navmode {get; set;}
        public byte pointype {get; set;}
        public byte mon {get; set;}
        public byte amode {get; set;}
        public float wrad {get; set;}
    }

    public class UploadMissionData
    {
        public UploadMissionHeader missionParam {get; set;} = new UploadMissionHeader();
        public List<UploadWaypoint> pointslist {get; set;} = new List<UploadWaypoint>();
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    class  NavigationData
    {
        public byte[] buttons { get; set; } = new byte[17];
        public UInt16 POV { get; set; } = 0;
        public float axisX { get; set; } = 0;
        public float axisY { get; set; } = 0;
        public float wheel { get; set; } = 0;
        public float throttle { get; set; } = 0;
    }


}
