using Microsoft.AspNet.SignalR.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using tdp_update_agent.Models;

namespace tdp_update_agent
{
    class Agent
    {

        static void Main(string[] args)
        {

            databaseContext _context = new databaseContext();
            Console.WriteLine("Starting database update agent...");

            var hubConnection = new HubConnection("http://192.168.1.13");


            foreach (InstrumentMod instrument in _context.getInstruments())
            {
                StatusCheck status = new StatusCheck(instrument, _context);
            }

            foreach(InstrumentMod instrument in _context.getOnline())
            {
                RunCheck check = new RunCheck(instrument, _context);
            }

        }

    }

    class StatusCheck
    {

        InstrumentMod instrument;
        databaseContext _context;

        public StatusCheck(InstrumentMod instrument, databaseContext context)
        {
            this.instrument = instrument;
            this._context = context;
            Thread thread = new Thread(new ThreadStart(CheckUpdate));
            thread.Start();
        }

        public void CheckUpdate()
        {
            while (true)
            {
                Uri URI = new Uri("HTTP://" + this.instrument.localAddress + "/tdx/getInstrumentStatus");
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(URI);

                request.ContentType = "application/json; charset=utf-8";
                request.Method = "GET";
                request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes("admin:PASSWORD"));

                Console.Write(DateTime.Now + " [GET STATUS]: ");
                Console.WriteLine("From " + this.instrument.name);

                try
                {
                    var response = request.GetResponse() as HttpWebResponse;

                    using (Stream responseStream = response.GetResponseStream())
                    {
                        StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
                        if (reader.ReadToEnd().Contains("IDLE"))
                        {
                            this._context.setStatus(this.instrument, "IDLE");
                        }
                        else if (reader.ReadToEnd().Contains("OFFLINE"))
                        {
                            this._context.setStatus(this.instrument, "OFFLINE");
                        }
                        else if (reader.ReadToEnd().Contains("BUSY"))
                        {
                            this._context.setStatus(this.instrument, "BUSY");
                        }
                        else
                        {
                            this._context.setStatus(this.instrument, "ERROR");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Write(DateTime.Now + " [ERROR STATUS]: ");
                    Console.WriteLine(ex.Message);
                }

                Thread.Sleep(10000);

            }

           
        }
    }

    class RunCheck
    {

        InstrumentMod instrument;
        databaseContext _context;

        public RunCheck(InstrumentMod instrument, databaseContext context)
        {
            this.instrument = instrument;
            this._context = context;
            Thread thread = new Thread(new ThreadStart(GetRuns));
            thread.Start();
        }

        public void GetRuns()
        {
            while (true)
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
                        //foreach(string id in this._context.getUniqueIds(ids.ToArray())) { Console.WriteLine(id); }
                        foreach (string id in this._context.getUniqueIds(ids.ToArray())) { _context.addRun(GetRun(id)); }
                    }
                }

                catch (Exception ex)
                {
                    Console.Write(DateTime.Now + " [ERROR GETRUNS]: ");
                    Console.WriteLine(ex.Message);
                }

                Thread.Sleep(10000);

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
