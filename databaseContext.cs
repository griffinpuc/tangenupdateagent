using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using tdp_update_agent.Models;

namespace tdp_update_agent
{
    class databaseContext : DbContext
    {

        public databaseContext(DbContextOptions<databaseContext> options)
        : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Data Source=tangen-web-01;Initial Catalog=portal-pprd;User Id=SQL_TDP_USER;Password=November2019!;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=Falsee");
        }

        public DbSet<InstrumentMod> instrumentsTable { get; set; }

    }
}
