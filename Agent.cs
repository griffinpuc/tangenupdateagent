//using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
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

            Console.WriteLine("Starting database update agent...");
            Console.WriteLine("SignalR Hub: {0}", uri);
            Console.WriteLine("Establishing connection to webserver...");

            Signal signal = new Signal();
            await signal.Start(uri);

            HubConnection _connection = signal.getConnection();

            Console.WriteLine("----------------------------");

            foreach (InstrumentMod instrument in _context.getInstruments())
            {
                Update update = new Update(instrument, _context, _connection);
            }

        }

    }

    class Signal
    {
        HubConnection connection;

        public async Task Start(string uri)
        {
            this.connection = new HubConnectionBuilder().WithUrl(uri).Build();

            await connection.StartAsync();

            if (connection.State.Equals(HubConnectionState.Connected))
            {
                Console.WriteLine("Established connection to webserver with SignalR");
            }
            else
            {
                Console.WriteLine("Failed to establish connection to webserver with SignalR");
                Console.WriteLine("You may continue to start Update Agent, though real-time page updates will be disabled.");
                Console.WriteLine("Press any key to continue, or exit...");
                Console.ReadLine();
            }

        }

        public HubConnection getConnection()
        {
            return this.connection;
        }

    }

    class Update
    {

        InstrumentMod instrument;
        HubConnection _connection;
        databaseContext _context;

        public Update(InstrumentMod instrument, databaseContext context, HubConnection connection)
        {
            this.instrument = instrument;
            this._context = context;
            this._connection = connection;


            Thread thread = new Thread(new ThreadStart(Updater));
            thread.Start();
            

        }

        public void Updater()
        {
            while (true)
            {
                CheckUpdate();
                GetRuns();

                Thread.Sleep(10000);
            }

        }

        public void CheckUpdate()
        {
            Uri URI = new Uri("HTTP://" + this.instrument.localAddress + "/tdx/getInstrumentStatus");
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(URI);

            request.ContentType = "application/json; charset=utf-8";
            request.Method = "GET";
            request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes("admin:PASSWORD"));

            Console.Write(DateTime.Now + " [GET STATUS]: ");
            Console.Write("From {0}: ", this.instrument.name);

            try
            {
                var response = request.GetResponse() as HttpWebResponse;

                using (Stream responseStream = response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
                    var jobj = JObject.Parse(reader.ReadToEnd());

                    this.instrument.status = jobj.GetValue("instrumentStatus").ToString();
                    _context.setStatus(this.instrument);

                    this._connection.InvokeAsync("updatestatus", this.instrument.status, this.instrument.ID, this.instrument.getColor());

                    Console.WriteLine(this.instrument.status);
                }
            }
            catch (Exception ex)
            {
                this.instrument.status = "OFFLINE";
                Console.WriteLine("OFFLINE");
                _context.setStatus(this.instrument);
                this._connection.InvokeAsync("updatestatus", this.instrument.status, this.instrument.ID, this.instrument.getColor());
                Console.WriteLine("{0} [ERROR STATUS]: {1}...", DateTime.Now, ex.Message.Substring(0, 100));
            }

            Thread.Sleep(1000);

        }

        public void GetRuns()
        {
            if(this.instrument.status.Equals("IDLE"))
            {
                Console.Write(DateTime.Now + " [GET RUN LIST]: ");
                Console.WriteLine("From " + this.instrument.name);
                Uri URI = new Uri("HTTP://" + this.instrument.localAddress + "/tdx/getResults");
                System.Net.HttpWebRequest request = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(URI);
                request.ContentType = "application/json; charset=utf-8";
                request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes("admin:PASSWORD"));

                try
                {
                    var response = request.GetResponse() as HttpWebResponse;
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
                        JsonSerializer serializer = new JsonSerializer();

                        List<string> ids = new List<string>();
                        Array.ForEach(JsonConvert.DeserializeObject<Result[]>(reader.ReadToEnd()), res => ids.Add(res.uniqueId));
                        foreach (string id in this._context.getUniqueIds(ids.ToArray())) { _context.addRun(GetRun(id)); }
                    }
                }

                catch (Exception ex)
                {
                    Console.Write(DateTime.Now + " [ERROR GETRUNS]: ");
                    Console.WriteLine(ex.Message);
                }

                Thread.Sleep(1000);

            }


        }

        public RunMod GetRun(string uniqueid)
        {
            Uri URI = new Uri("HTTP://" + this.instrument.localAddress + "/tdx/getTestResult?id=" + uniqueid);
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(URI);

            Console.Write(DateTime.Now + " [GET RUN]: ");
            Console.WriteLine(uniqueid);

            request.ContentType = "application/json; charset=utf-8";
            request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes("admin:PASSWORD"));

            var response = request.GetResponse() as HttpWebResponse;

            using (Stream responseStream = response.GetResponseStream())
            {
                StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
                JsonSerializer serializer = new JsonSerializer();

                return JsonConvert.DeserializeObject<RunMod>(reader.ReadToEnd());
            }


        }

    }
}
