using System;
using System.Collections.Generic;
using Rug.Osc;
using System.Threading;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Net;

namespace VAOscPlugin
{

    public class VoiceAttackPlugin
    {

        const string C_APP_NAME = "Lerk's Osc Plugin";
        const string C_APP_VERSION = "v0.2";

        private static OscReceiver _receiver;
        private static Task _receiverTask;
        private static CancellationTokenSource _cancelSource;

        private static int _senderPort = 0;
        private static IPAddress _senderIpAddress;

        public static string AssemblyDirectory
        {
            get
            {
                string fullPath = Assembly.GetAssembly(typeof(VoiceAttackPlugin)).Location;
                return Path.GetDirectoryName(fullPath);
            }
        }

        public static string VA_DisplayName()
        {
            return $"{C_APP_NAME} {C_APP_VERSION}";
        }

        public static string VA_DisplayInfo()
        {
            return $"{C_APP_NAME} allows VoiceAttack to send and receive OSC messages";  
        }

        public static Guid VA_Id()
        {
            return new Guid("{874C92BC-1426-4EA6-9156-F24161796CB8}");  
        }

        public static void VA_StopCommand()
        {

        }

        public static void VA_Init1(dynamic vaProxy)
        {
            string settingsPath = Path.Combine(AssemblyDirectory, "oscsettings.txt");
            vaProxy.WriteToLog("Loading mappings from: " + settingsPath, "black");

            var commandDict = new Dictionary<string, string>();

            string line;
            // Read the file and display it line by line.  
            using (StreamReader file = new StreamReader(settingsPath)) {
                while ((line = file.ReadLine()) != null)
                {
                    if (!line.Contains(';')) continue;
                    var oscCommandSplit = line.Split(';');
                    var oscAddress = oscCommandSplit[0];
                    var vaCommand = line.Substring(oscCommandSplit[0].Length+1);
                    commandDict.Add(oscAddress, vaCommand);
                    vaProxy.WriteToLog("Mapping " + oscAddress + " to " + vaCommand);
                }
            };

            string portSettingPath = Path.Combine(AssemblyDirectory, "oscport.txt");
            int port = 0;
            using (StreamReader file = new StreamReader(portSettingPath))
            {
                string portString = file.ReadLine();
                port = int.Parse(portString);
            };

            // Create the receiver
            _receiver = new OscReceiver(port);
            _cancelSource = new CancellationTokenSource();
            _receiverTask = new Task(x =>
            {
                try
                {
                    while (_receiver.State != OscSocketState.Closed)
                    {
                        // if we are in a state to recieve
                        if (_receiver.State == OscSocketState.Connected)
                        {
                            // get the next message 
                            // this will block until one arrives or the socket is closed
                            OscPacket packet = _receiver.Receive();
                            var message = (OscMessage)packet;
                            if (commandDict.ContainsKey(message.Address))
                            {
                                vaProxy.Command.Execute(commandDict[message.Address]);
                            }
                            else
                            {
                                vaProxy.WriteToLog($"OSC address not found in config: {message.Address}", "red");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // if the socket was connected when this happens
                    // then tell the user
                    if (_receiver.State == OscSocketState.Connected)
                    {
                        vaProxy.WriteToLog("Exception in OSC listener loop: " + ex.ToString(), "red");
                    }
                }
            }, _cancelSource.Token, TaskCreationOptions.LongRunning);

            // Connect the receiver
            _receiver.Connect();

            // Start the listen thread
            _receiverTask.Start();


            string senderPath = Path.Combine(AssemblyDirectory, "oscsenderport.txt");
            using (StreamReader file = new StreamReader(senderPath))
            {
                string senderInfoString = file.ReadLine();
                var splitSenderInfo = senderInfoString.Split(':');
                _senderIpAddress = IPAddress.Parse(splitSenderInfo[0]);
                _senderPort = int.Parse(splitSenderInfo[1]);
            };

        }


        public static void VA_Exit1(dynamic vaProxy)
        {

            // close the Reciver 
            _receiver.Close();

            // Wait for the listen thread to exit
            _cancelSource.Cancel();

        }

        public static void VA_Invoke1(dynamic vaProxy)
        {

            try
            {
                string fullOscCommand = vaProxy.Context;
                string oscAddress = fullOscCommand.Contains(':') ? fullOscCommand.Split(':')[0] : fullOscCommand;
                string[] oscArgStrings = null;
                List<object> oscArgList = new List<object>();

                if (fullOscCommand.Contains(':'))
                {
                    oscArgStrings = fullOscCommand.Contains(';') 
                        ? fullOscCommand.Split(':')[1].Split(';') 
                        : (new string[] { fullOscCommand.Split(':')[1] });

                    foreach (string oscArgument in oscArgStrings)
                    {
                        if (oscArgument != null && (
                            oscArgument.StartsWith("i ", StringComparison.OrdinalIgnoreCase) || 
                            oscArgument.StartsWith("f ", StringComparison.OrdinalIgnoreCase) || 
                            oscArgument.StartsWith("b ", StringComparison.OrdinalIgnoreCase) || 
                            oscArgument.StartsWith("s ", StringComparison.OrdinalIgnoreCase)))
                        switch (oscArgument.Substring(0, 1))
                        {
                            case "i":
                                int intValue;
                                if (int.TryParse(oscArgument.Substring(2), out intValue))
                                    oscArgList.Add(intValue);
                                break;
                            case "f":
                                float floatValue;
                                if (float.TryParse(oscArgument.Substring(2), out floatValue))
                                    oscArgList.Add(floatValue);
                                break;
                            case "b":
                                bool boolValue;
                                if (bool.TryParse(oscArgument.Substring(2), out boolValue))
                                    oscArgList.Add(boolValue);
                                break;
                            case "s":
                                if (!string.IsNullOrEmpty(oscArgument.Substring(2)))
                                    oscArgList.Add(oscArgument.Substring(2));
                                break;
                        }
                    }
                }

                using (OscSender sender = new OscSender(_senderIpAddress, 0, _senderPort))
                {
                    sender.Connect();
                    if (oscArgList.Any())
                    {
                        sender.Send(new OscMessage(oscAddress, oscArgList.ToArray()));
                    } else
                    {
                        sender.Send(new OscMessage(oscAddress));
                    }                   
                }
            } 
            catch (Exception ex)
            {
                vaProxy.WriteToLog("Error sending OSC message: " + ex.ToString(), "red");
            }


        }

    }

}