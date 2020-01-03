using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace tdp_update_agent
{
    class Signal
    {
        HubConnection connection;

        public async Task Start(string uri)
        {
            try { this.connection = new HubConnectionBuilder().WithUrl(uri).Build(); }
            catch (Exception ex) { Console.WriteLine(ex.GetType()); }
            await connection.StartAsync();
        }

        public HubConnection getConnection() { return this.connection; }

    }
}
