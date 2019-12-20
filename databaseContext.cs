using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using tdp_update_agent.Models;
using System.Linq;

namespace tdp_update_agent
{
    class databaseContext : DbContext
    {

        String connection = @"Data Source=tangen-web-01;Initial Catalog=portal-pprd;User ID=SQL_TDP_USER;Password=November2019!;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(connection);
        }

        public String getConnection()
        {
            return this.connection;
        }

        public DbSet<InstrumentMod> InstrumentTable { get; set; }
        public DbSet<RunMod> RunTable { get; set; }

        public InstrumentMod[] getInstruments()
        {
            return (from InstrumentMod in InstrumentTable select InstrumentMod).ToArray(); 
        }

        public int[] getInstrumentsID()
        {
            return (from InstrumentMod in InstrumentTable select InstrumentMod.ID).ToArray();
        }

        public InstrumentMod[] getOnline()
        {
            return (from InstrumentMod in InstrumentTable where InstrumentMod.status.Equals("IDLE") select InstrumentMod).ToArray();
        }

        public string[] getUniqueIds(string[] ids)
        {
            string[] db_ids = (from RunMod in RunTable select RunMod.uniqueId).ToArray();

            return ids.Except(db_ids).ToArray();
        }
        

        public void updateInstrument(InstrumentMod instrument)
        {
            Update(instrument);
            SaveChanges();
        }

        public void addRun(RunMod run)
        {
            Add(run);
            SaveChanges();
        }

    }
}
