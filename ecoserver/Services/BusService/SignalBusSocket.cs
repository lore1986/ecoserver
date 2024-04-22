using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace webapi
{
    public class SignalBusSocket : ISignalBusSocket
    {
        private readonly List<Tuple<Action<SignalBusMessage>, EcoClient>> _subscribers = new List<Tuple<Action<SignalBusMessage>, EcoClient>>();
        public List<Tuple<string, EcoClient>> messageQue = new List<Tuple<string, EcoClient>>();
        private List<string> allAllMessages;
        public readonly EcodroneBoat instance_boat;
        public SignalBusSocket(EcodroneBoat ecodroneBoat)
        {
            instance_boat = ecodroneBoat;
            allAllMessages = new List<string>()
            {
                "ImuData"
            };
        }

        public void AddMessageToQueue(EcoClient client, string idcontainer)
        {
            messageQue.Add(Tuple.Create(idcontainer, client));
        }

        public bool IsMessageForAll(string idmessage)
        {
            return allAllMessages.Contains(idmessage);
        }

        public EcoClient? ReturnClientWhoRequested(string idcommand)
        {
            return messageQue.First(x => x.Item1 == idcommand).Item2;
        }
        public void RemoveClientCommandMessage(string idclient)
        {
            messageQue.Remove(messageQue.First(x => x.Item2.IdClient == idclient));
        }

        public void Publish(SignalBusMessage eventMessage, string? idclient = null )
        {
            if(eventMessage.data_command != null)
            {
                instance_boat.teensySocketInstance.command_task_que.Add(new TeensyMessageContainer(eventMessage.message_id, eventMessage.data_command));
            }
            foreach(var subscrib in _subscribers)
            {
                subscrib.Item1.Invoke(eventMessage);
            }
            // if(allAllMessages.Contains(eventMessage.message_id))
            // {
            //     foreach(var subscrib in _subscribers)
            //     {
            //         if(subscrib.Item2.appState == ClientCommunicationStates.SENSORS_DATA)
            //         {
            //             subscrib.Item1.Invoke(eventMessage);
            //         }
            //     }

            // }else
            // {
            //     if(idclient != null)
            //     {
            //         _subscribers.Find(x => x.Item2.IdClient == idclient)?.Item1.Invoke(eventMessage);
            //     }
                
            // }
            
        }


        public void Subscribe(Action<SignalBusMessage> action, EcoClient client)
        {
            var userTuple = new Tuple<Action<SignalBusMessage>, EcoClient>(action, client);
            _subscribers.Add(userTuple);  
        }

        public void Unsubscribe(Action<SignalBusMessage> action, EcoClient client)
        {
            var userTuple = new Tuple<Action<SignalBusMessage>, EcoClient>(action, client);
            _subscribers.Remove(userTuple);
        }

        public bool IsASubscriber(string id)
        {
            return  _subscribers.Any(x => x.Item2.IdClient == id);
            
        }

    }
}

   