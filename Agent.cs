//using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        static async Task Main(string[] args)
        {

            databaseContext _context = new databaseContext();
            //String uri = "https://localhost:44385/dataHub";
            //String uri = "http://localhost/dataHub";
            String uri = "http://192.168.1.13/dataHub";

            Dictionary<InstrumentMod, Thread> instrumentThreads = new Dictionary<InstrumentMod, Thread>();

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
                InstrumentMod[] openthreadKeys = instrumentThreads.Keys.ToArray();

                foreach (InstrumentMod instrument in _context.getInstruments())
                {
                    if (!openthreadKeys.Contains(instrument))
                    {
                        instrumentThreads.Add(instrument, new Thread(new ThreadStart(new Update(instrument, _connection).Updater)));
                        instrumentThreads[instrument].Start();
                        Console.WriteLine("{0} [AGENT]: {1} thread opened!", DateTime.Now, instrument.name);
                    }

                    foreach (InstrumentMod closedInstrument in (openthreadKeys.Except(_context.getInstruments())))
                    {
                        instrumentThreads[closedInstrument].Abort();
                        Console.WriteLine("{0} [AGENT]: {1} thread closed!", DateTime.Now, closedInstrument.name);
                    }
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

        public void Updater()
        {
            while (true)
            {
                GetStatus();
                GetRuns();
                Thread.Sleep(10000);
            }
        }

        public void GetStatus()
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
                    this.instrument.lastPing = DateTime.Now.ToString();
                    _context.updateInstrument(this.instrument);

                    this._connection.InvokeAsync("updatestatus", this.instrument.status, this.instrument.ID, this.instrument.getColor());
                }
            }
            catch (Exception ex)
            {
                this.instrument.status = "OFFLINE";
                _context.updateInstrument(this.instrument);
                this._connection.InvokeAsync("updatestatus", this.instrument.status, this.instrument.ID, this.instrument.getColor());
                ln = new StringBuilder().AppendFormat("{0} [{2} ERROR]: {1}...", DateTime.Now, ex.Message, this.instrument.name).ToString();
                Console.WriteLine(ln);
                this._connection.InvokeAsync("streamline", ln);
                Console.WriteLine();
            }
        }

        public void GetRuns()
        {
            if (this.instrument.status.Equals("IDLE"))
            {
                Console.WriteLine(DateTime.Now + " [{0} GET RUNS]", this.instrument.name);
                Uri URI = new Uri("HTTP://" + this.instrument.localAddress + "/tdx/getResults");
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(URI);
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

                        foreach (string id in this._context.getUniqueIds(ids.ToArray()))
                        {
                            string dirpointer = GetRaw(id);
                            if (dirpointer != null)
                            {
                                RunMod newRun = GetRun(id);
                                newRun.DirPointer = dirpointer;
                                _context.addRun(newRun);
                            }
                            else
                            {
                                Console.WriteLine("{0} [{1} ERROR] Run NOT SAVED due to run download error.", DateTime.Now, this.instrument.name);
                                Console.WriteLine("{0} [{1} ERROR] Exiting run/raw upload process...", DateTime.Now, this.instrument.name);
                                break;
                            }
                        }
                    }
                }

                catch (Exception ex)
                {
                    ln = new StringBuilder().AppendFormat(DateTime.Now + " [{0} ERROR]: {1} ", this.instrument.name, ex.Message).ToString();
                    Console.WriteLine(ln);
                    this._connection.InvokeAsync("streamline", ln);
                    Console.WriteLine();
                }

            }


        }

        public RunMod GetRun(string uniqueid)
        {
            Thread.Sleep(500);
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

        public string GetRaw(string uniqueid)
        {
            Thread.Sleep(1000);
            string retval = null;
            string filename = uniqueid + "_RAW.json";

            Uri URI = new Uri("HTTP://" + this.instrument.localAddress + "/tdx/getRawResult?id=" + uniqueid);
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(URI);

            Console.WriteLine(DateTime.Now + " [GET RAW FILE]: " + uniqueid);

            request.ContentType = "application/json; charset=utf-8";
            request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes("admin:PASSWORD"));
            var response = request.GetResponse() as HttpWebResponse;
            try
            {
                //var response = request.GetResponse() as HttpWebResponse;

                using (Stream responseStream = response.GetResponseStream())
                {
                    using (Stream s = File.Create("temp/" + filename))
                    {
                        responseStream.CopyTo(s);
                        retval = filename;
                        Console.WriteLine("{0} [{1} CP53] Run file saved: {2}", DateTime.Now, this.instrument.name, filename);
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("{0} [{1} ERROR] Run file download failed...", DateTime.Now, this.instrument.name);
                Console.WriteLine(ex.Message);
                Console.WriteLine();
            }

            return retval;

        }


    }
}
