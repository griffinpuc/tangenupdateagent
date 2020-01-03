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
            //String uri = "https://localhost:44385/dataHub";
            //String uri = "http://localhost/dataHub";
            String uri = "http://192.168.1.13/dataHub";

            //List <int> openInstrumentsList = new List<int>();
            Dictionary<int, CancellationTokenSource> openInstruments = new Dictionary<int, CancellationTokenSource>();

            Console.WriteLine("Starting database update agent...");
            Console.WriteLine("SignalR Hub: {0}", uri);
            Console.WriteLine("Establishing connection to webserver...");

            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

            Console.WriteLine("----------------------------");

            while (true)
            {
                foreach (int closedInstrument in (openInstruments.Keys.ToArray().Except(_context.getInstrumentsIDS())))
                {
                    if (!openInstruments[closedInstrument].Token.IsCancellationRequested)
                    {
                        openInstruments[closedInstrument].Cancel();
                        try
                        {
                            Console.WriteLine("{0} >> Closed instrument with ID of: {1}", DateTime.Now, _context.getFromID(closedInstrument).name);
                        }
                        catch
                        {
                            Console.WriteLine("{0} >> Closed deleted instrument...", DateTime.Now);
                        }
                        
                    }
                }

                int[] rr = _context.getInstrumentsIDS();

                foreach (InstrumentMod instrument in _context.getInstruments())
                {
                    instrument.isActive = true;
                    if (!openInstruments.Keys.ToArray().Contains(instrument.ID))
                    {
                        CancellationTokenSource tokensrc = new CancellationTokenSource();

                        openInstruments.Add(instrument.ID, tokensrc);

                        Task.Run(() => {

                            Cycle cycle = new Cycle(instrument, uri);
                            while (true)
                            {
                                if (openInstruments[instrument.ID].Token.IsCancellationRequested)
                                {
                                    openInstruments.Remove(instrument.ID);
                                    instrument.status = "PAUSED";
                                    instrument.isActive = false;
                                    cycle.getConnection().InvokeAsync("updatestatus", "PAUSED", instrument.ID, "#FFA500");
                                    _context.updateInstrument(instrument);
                                    throw new OperationCanceledException();
                                }
                                cycle.StartTask();
                                Thread.Sleep(5000);
                            }



                        }, openInstruments[instrument.ID].Token);
                        Console.WriteLine("{0} [AGENT]: {1} thread opened!", DateTime.Now, instrument.name);
                    }
                }

                Thread.Sleep(3000);

            }

        }

    }
}
