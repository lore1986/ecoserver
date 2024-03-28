using Microsoft.AspNetCore.Mvc;


namespace webapi.Controllers 
{
    [Route("/service/[controller]")]
    [ApiController]
    public class BoatServiceController : ControllerBase
    {
        private readonly IActiveBoatTracker _activeBoatTracker;

        public BoatServiceController(IActiveBoatTracker activeBoatTracker)
        {
            _activeBoatTracker = activeBoatTracker;
        }


        [HttpPost("ActivateBoat")]
        public IActionResult ActivateBoat(string boatid)
        {
            
            //from boatid get database to fill this data below
            if(true)
            {
                
                EcodroneBoat? boat = ActivateBoatInternal(boatid);

                if(boat != null)
                {
                    boat.StartEcodroneBoatTasks();
                }
                
                return Ok();
            }
        
        }
        
        private EcodroneBoat? ActivateBoatInternal(string boatid)
        {
            
            EcodroneBoat? _ecodroneBoat = _activeBoatTracker.CreateAddBoat(boatid);

            return _ecodroneBoat;
        }
        

        [HttpGet("DeactivateBoat")]
        public async Task<IActionResult> Deactivate(string boatid)
        {
            EcodroneBoat? _ecodroneBoat = _activeBoatTracker.ReturnEcodroneBoatInstance(boatid);

            if(_ecodroneBoat != null)
            {
               bool var_result = _activeBoatTracker.RemoveEcodroneBoatInstance(boatid);

               if(!var_result)
               {throw new Exception("something wrong in closing");};
            }
            
            return Ok();
        }
        
        [HttpPost("ReactivateBoat")]
        public IActionResult ReactivateBoat(string boatid)
        {
            
            //from boatid get database to fill this data below
            if(true)
            {
                
                EcodroneBoat? boat = ActivateBoatInternal(boatid);

                if(boat != null)
                {
                    boat.StartEcodroneBoatTasks();
                }
                
                return Ok();
            }
        
        }
        


       


        

        #region toImplement
        private bool BufferIsComplete(EcoClient ecoClient)
        {
            // if(message_length_uint != (receiveResult.Count - 9))
            // {
            //     //message is partial
            //     if(ecoClient != null)
            //     {
            //         if(ecoClient.message_length == 0)
            //         {
            //             ecoClient.message_length = message_length_uint;
            //             ecoClient.buffer_bytes =  new byte[message_length_uint];
                        
            //             Array.Copy(byte_message, ecoClient.buffer_bytes, 0);
            //             ecoClient.bytecopied = (uint)byte_message.Length;

            //         }else
            //         {
            //             if(ecoClient.buffer_bytes != null)
            //             {
            //                 if(ecoClient.message_length <= (ecoClient.bytecopied + receiveResult.Count - 9))
            //                 uint bytetocopy = (uint)ecoClient.buffer_bytes.Length - ecoClient.bytecopied;
            //                 Array.Copy(byte_message, 0, ecoClient.buffer_bytes, ecoClient.bytecopied, bytetocopy);
            //             }
                        
            //         }
                    
            //     }
            // }

            return true;
        }
        #endregion
    }
        
}