using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FaceAccess.Models;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Configuration;

namespace FaceAccess.Context
{
    public class MemberInfoContext : DbContext
    {
        public MemberInfoContext()
            : base(ConfigurationManager.ConnectionStrings["MemberInfoConnection"].ConnectionString)
        {
        }
        public DbSet<MemberInfo> MemberInfos { get; set; }
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
        }
    }

}
