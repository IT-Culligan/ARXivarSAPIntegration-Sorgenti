﻿//------------------------------------------------------------------------------
// <auto-generated>
//    Codice generato da un modello.
//
//    Le modifiche manuali a questo file potrebbero causare un comportamento imprevisto dell'applicazione.
//    Se il codice viene rigenerato, le modifiche manuali al file verranno sovrascritte.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ArxivarSapIntegration
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class BI_STGEntities1 : DbContext
    {
        public BI_STGEntities1()
            : base("name=BI_STGEntities1")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public DbSet<SFDC_Anagrafica_Clienti> SFDC_Anagrafica_Clienti { get; set; }
        public DbSet<FE_PDF_Arxivar_CHECK> FE_PDF_Arxivar_CHECK { get; set; }
        public DbSet<FE_PDF_Arxivar_CHECK_FRIZZY> FE_PDF_Arxivar_CHECK_FRIZZY { get; set; }
    }
}
