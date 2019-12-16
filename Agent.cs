using System;

namespace tdp_update_agent
{
    class Agent
    {

        static void Main(string[] args)
        {
            
        }

        public void WriteLog(string message)
        {
            Console.WriteLine("[" + DateTime.Now + "] " + message);
        }

    }

    class DatabaseConnection
    {

    }

    class StatusCheck
    {
        public StatusCheck(string instrumentName, string UUID, string localaddress)
        {

        }
    }
}
