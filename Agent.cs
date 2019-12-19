//using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tdp_update_agent.Models;

namespace tdp_update_agent
{
    class Agent
    {
        public static List<Thread> startedThreads = new List<Thread>();

        static async Task Main(string[] args)
        {

            databaseContext _context = new databaseContext();
            //String uri = "https://localhost:44385/dataHub";
            String uri = "http://localhost/dataHub";
            List<int> instrumentList = new List<int>();

            Console.WriteLine("Starting database update agent...");
            Console.WriteLine("SignalR Hub: {0}", uri);
            Console.WriteLine("Establishing connection to webserver...");

            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            Signal signal = new Signal();
            await signal.Start(uri);

            HubConnection _connection = signal.getConnection();

            Console.WriteLine("----------------------------");

            while (true)
            {
                foreach (InstrumentMod instrument in _context.getInstruments())
                {
                    Update update = new Update(instrument, _connection);
                    Thread thread = new Thread(new ThreadStart(update.Updater));
                    thread.Start();
                    startedThreads.Add(thread);
                }

                foreach (Thread thread in startedThreads)
                {
                    thread.Join();
                }

                Thread.Sleep(10000);
            }


        }

    }

    class Signal
    {
        HubConnection connection;

        public async Task Start(string uri)
        {
            try{ this.connection = new HubConnectionBuilder().WithUrl(uri).Build(); }
            catch(Exception ex){ Console.WriteLine(ex.GetType()); }
            await connection.StartAsync();
        }

        public HubConnection getConnection(){ return this.connection; }

    }

    class Update
    {

        InstrumentMod instrument;
        HubConnection _connection;
        databaseContext _context;

        string ln;

        public Update(InstrumentMod instrument, HubConnection connection)
        {
            this.instrument = instrument;
            this._context = new databaseContext();
            this._connection = connection;
            
        }

        public void Updater(){ CheckUpdate(); GetRuns(); }

        public void CheckUpdate()
        {
            Uri URI = new Uri("HTTP://" + this.instrument.localAddress + "/tdx/getInstrumentStatus");
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(URI);

            request.ContentType = "application/json; charset=utf-8";
            request.Method = "GET";
            request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes("admin:PASSWORD"));

            ln = new StringBuilder().AppendFormat(DateTime.Now + " [{0} GET STAT]", this.instrument.name).ToString();
            Console.WriteLine(ln);
            this._connection.InvokeAsync("streamline", ln);

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
                }
            }
            catch (Exception ex)
            {
                this.instrument.status = "OFFLINE";
                _context.setStatus(this.instrument);
                this._connection.InvokeAsync("updatestatus", this.instrument.status, this.instrument.ID, this.instrument.getColor());
                ln = new StringBuilder().AppendFormat("{0} [{2} ERROR]: {1}...", DateTime.Now, ex.Message, this.instrument.name).ToString();
                Console.WriteLine(ln);
                this._connection.InvokeAsync("streamline", ln);
            }

            Thread.Sleep(1000);

        }

        public void GetRuns()
        {
            if(this.instrument.status.Equals("IDLE"))
            {
                Console.WriteLine(DateTime.Now + " [{0} GET RUNS]", this.instrument.name);
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
                    ln = new StringBuilder().AppendFormat(DateTime.Now + " [{0} ERROR]: {1} ", this.instrument.name, ex.Message).ToString();
                    Console.WriteLine(ln);
                    this._connection.InvokeAsync("streamline", ln);
                }

            }


        }

        public RunMod GetRun(string uniqueid)
        {
            Uri URI = new Uri("HTTP://" + this.instrument.localAddress + "/tdx/getTestResult?id=" + uniqueid);
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(URI);

            Console.WriteLine(DateTime.Now + " [GET RUN]: " + uniqueid);

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
