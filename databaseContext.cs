﻿using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using tdp_update_agent.Models;
using System.Linq;

namespace tdp_update_agent
{
    class databaseContext : DbContext
    {

        String connection;
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(connection);
        }

        public String getConnection()
        {
            return this.connection;
        }

        public bool getPaused(int uniqueid)
        {
            bool status = (from InstrumentMod in InstrumentTable where InstrumentMod.ID == uniqueid select InstrumentMod.isActive).Single();

            if (status)
            {
                return true;
            }

            return false;
        }

        public DbSet<InstrumentMod> InstrumentTable { get; set; }
        public DbSet<RunMod> RunTable { get; set; }

        public InstrumentMod[] getInstruments()
        {
            return (from InstrumentMod in InstrumentTable where InstrumentMod.isActive select InstrumentMod).ToArray(); 
        }

        public int[] getInstrumentsIDS()
        {
            return (from InstrumentMod in InstrumentTable where InstrumentMod.isActive select InstrumentMod.ID).ToArray();
        }

        public int[] getInstrumentsID()
        {
            return (from InstrumentMod in InstrumentTable select InstrumentMod.ID).ToArray();
        }

        public InstrumentMod getFromID(int ID)
        {
            return (from InstrumentMod in InstrumentTable where InstrumentMod.ID == ID select InstrumentMod).FirstOrDefault();
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

        public string getLastUnique(int instrumentID)
        {
            string unique = (from RunMod in RunTable where RunMod.instrumentName.Equals(getFromID(instrumentID).name) select RunMod.uniqueId).FirstOrDefault() ?? null;
            return unique;
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
