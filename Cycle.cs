using System;
using System.Text;
using System.Threading;
using tdp_update_agent.Models;
using Microsoft.AspNetCore.SignalR.Client;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace tdp_update_agent
{
    //Object that checks status and handles all API calls
    class Cycle
    {

        InstrumentMod instrument;
        HubConnection connection;
        databaseContext context = new databaseContext();
        CancellationToken token;
        string uri;
        string filepath;

        private void logMessage(string msg)
        {
            string ln = new StringBuilder().AppendFormat("{0}:{1} >> {2}", DateTime.Now, this.instrument.name, msg).ToString();
            Console.WriteLine(ln);
            this.connection.InvokeAsync("streamline", ln);
        }

        public Cycle(InstrumentMod instrument, string uri, CancellationToken tok)
        {
            this.instrument = instrument;
            this.context = new databaseContext();
            this.uri = uri;
            this.token = tok;

            Signal signal = new Signal();
            signal.Start(this.uri);

            this.connection = signal.getConnection();

            StartTask();
        }

        private void StartTask()
        {
            while (true)
            {


                if (this.token.IsCancellationRequested)
                {
                    this.connection.InvokeAsync("updatestatus", "PAUSED", this.instrument.ID, "#FFA500");
                    this.instrument.isActive = false;
                    this.instrument.status = "PAUSED";
                    this.context.updateInstrument(this.instrument);
                    this.token.ThrowIfCancellationRequested();
                }

                if (this.connection.State.Equals(HubConnectionState.Disconnected))
                {
                    Signal signal = new Signal();
                    signal.Start(uri);
                    this.connection = signal.getConnection();
                }

                if (GetStatus())
                {
                    foreach (string id in this.context.getUniqueIds(GetRuns() ?? Enumerable.Empty<string>().ToArray()))
                    {
                        this.filepath = (@"C:\datadump\" + id.Substring(0, 6));

                        if (!new DirectoryInfo(this.filepath).Exists)
                        {
                            Directory.CreateDirectory(this.filepath);
                        }

                        if (GetRaw(id))
                        {
                            GetLog(id);
                            RunMod newrun = GetRun(id);
                            newrun.directoryPath = this.filepath;
                            newrun.fileName = id + "_RAW.txt";

                            this.context.addRun(newrun);
                        }
                    }
                }

                Thread.Sleep(10000);
            }


        }

        private bool GetStatus()
        {
            Uri URI = new Uri("HTTP://" + this.instrument.localAddress + "/tdx/getInstrumentStatus");
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(URI);

            request.ContentType = "application/json; charset=utf-8";
            request.Method = "GET";
            request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes("admin:PASSWORD"));

            logMessage("Getting status...");

            if (!this.context.getPaused(this.instrument.ID))
            {
                return false;
            }

            try
            {
                var response = request.GetResponse() as HttpWebResponse;

                using (Stream responseStream = response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
                    var jobj = JObject.Parse(reader.ReadToEnd());

                    this.instrument.status = jobj.GetValue("instrumentStatus").ToString();
                    this.instrument.lastPing = DateTime.Now.ToString();
                    context.updateInstrument(this.instrument);

                    this.connection.InvokeAsync("updatestatus", this.instrument.status, this.instrument.ID, this.instrument.getColor());
                }

                return true;
            }

            catch (Exception ex)
            {
                this.instrument.status = "OFFLINE";
                context.updateInstrument(this.instrument);
                this.connection.InvokeAsync("updatestatus", this.instrument.status, this.instrument.ID, this.instrument.getColor());
                logMessage(ex.Message);
                return false;
            }
        }

        private string[] GetRuns()
        {
                Console.WriteLine(DateTime.Now + " [{0} GET RUNS]", this.instrument.name);
                Uri URI = new Uri("HTTP://" + this.instrument.localAddress + "/tdx/getResults");
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(URI);
                request.ContentType = "application/json; charset=utf-8";
                request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes("admin:PASSWORD"));

                try
                {
                    var response = request.GetResponse() as HttpWebResponse;

                    if (response.StatusCode.Equals(HttpStatusCode.OK))
                    {
                        using (Stream responseStream = response.GetResponseStream())
                        {
                            StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
                            JsonSerializer serializer = new JsonSerializer();

                            List<string> ids = new List<string>();
                            Array.ForEach(JsonConvert.DeserializeObject<Result[]>(reader.ReadToEnd()), res => ids.Add(res.uniqueId));
                            return ids.ToArray();
                        }
                    }
                    else
                    {
                        logMessage("Got code other than 200- aborting.");
                        return null;
                    }


                }

                catch (Exception ex)
                {
                    logMessage(ex.Message);
                    return null;
                }

        }

        public bool GetRaw(string uniqueid)
        {
            string filename = uniqueid + "_RAW.json";

            Uri URI = new Uri("HTTP://" + this.instrument.localAddress + "/tdx/getRawResult?id=" + uniqueid);
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(URI);

            Console.WriteLine(DateTime.Now + " [GET RAW FILE]: " + uniqueid);

            request.ContentType = "application/json; charset=utf-8";
            request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes("admin:PASSWORD"));
            var response = request.GetResponse() as HttpWebResponse;

            if (response.StatusCode.Equals(HttpStatusCode.OK))
            {
                try
                {

                    using (Stream responseStream = response.GetResponseStream())
                    {
                        using (Stream s = File.Create(this.filepath + "\\" + filename))
                        {
                            responseStream.CopyTo(s);

                            ProcessStartInfo info = new ProcessStartInfo(@"C:\Users\griff\Workspace\tangendataconvert\tangendataconvert\bin\Release\netcoreapp3.0\tangendataconvert.exe");
                            info.Arguments = "-p " + this.filepath + "\\" + filename;
                            Process.Start(info);

                            logMessage(filename);
                        }
                    }

                    return true;
                }

                catch (Exception ex)
                {
                    logMessage(ex.Message);
                    return false;
                }
            }
            else
            {
                logMessage("Not 200");
                return false;
            }


        }

        public bool GetLog(string uniqueid)
        {
            string logfilename = uniqueid + "_LOG.txt";

            Uri URI = new Uri("HTTP://" + this.instrument.localAddress + "/tdx/getLogResult?id=" + uniqueid);
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(URI);

            request.ContentType = "application/json; charset=utf-8";
            request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes("admin:PASSWORD"));
            var response = request.GetResponse() as HttpWebResponse;
            try
            {

                using (Stream responseStream = response.GetResponseStream())
                {
                    using (Stream s = File.Create(this.filepath + "\\" + logfilename))
                    {
                        responseStream.CopyTo(s);
                        logMessage(logfilename);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                logMessage(ex.Message);
                return false;
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
