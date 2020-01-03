//using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tdp_update_agent.Models;

namespace tdp_update_agent
{
    class Agent
    {
        static async Task Main(string[] args)
        {

            databaseContext _context = new databaseContext();
            String uri = "https://localhost:44385/dataHub";
            //String uri = "http://localhost/dataHub";
            //String uri = "http://192.168.1.13/dataHub";

            List <int> openInstrumentsList = new List<int>();

            Console.WriteLine("Starting database update agent...");
            Console.WriteLine("SignalR Hub: {0}", uri);
            Console.WriteLine("Establishing connection to webserver...");

            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

            Console.WriteLine("----------------------------");


            while (true)
            {

                int[] openInstruments = openInstrumentsList.ToArray();

                foreach (int closedInstrument in (openInstruments.Except(_context.getInstrumentsIDS())))
                {
                    closedInstrument.token.Cancel();
                    openInstrumentsList.Remove(closedInstrument);
                    Console.WriteLine("{0} [AGENT]: {1} thread closed!", DateTime.Now, closedInstrument.name);
                }

                foreach (InstrumentMod instrument in _context.getInstruments())
                {
                    if (!openInstruments.Contains(instrument))
                    {
                        openInstrumentsList.Add(instrument);
                        instrument.token = new CancellationTokenSource();
                        Task.Run(() => new Cycle(instrument, uri, instrument.token.Token), instrument.token.Token);
                        Console.WriteLine("{0} [AGENT]: {1} thread opened!", DateTime.Now, instrument.name);
                    }
                }

                Thread.Sleep(5000);
            }


        }

    }
}
