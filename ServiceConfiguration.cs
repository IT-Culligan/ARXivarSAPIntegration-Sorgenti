using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.IO;

namespace ArxivarSAPIntegration
{
    public class ServiceConfiguration : Abletech.Utility.Configurazione
    {
        #region Impostazioni log

        private System.Diagnostics.EventLogEntryType _logLevel;
        private string _logPath;

        [CategoryAttribute("Impostazioni log"), DescriptionAttribute("Livello di log")]
        public System.Diagnostics.EventLogEntryType LogLevel
        {
            get { return _logLevel; }
            set { _logLevel = value; }
        }
        [CategoryAttribute("Impostazioni log"), DescriptionAttribute("Cartella per la scrittura del log")]
        public string LogPath
        {
            get { return @"c:\temp"; } // _logPath
            set { _logPath = value; }
        }

        #endregion

        #region Impostazioni connessione

        private string _wcfUrl;
        private string _wcfUsername;
        private string _wcfPassword;
        private string _clientId;
        private string _clientSecret;

        [CategoryAttribute("Impostazioni connessione"), DescriptionAttribute("Indirizzo di connessione al servizio WCF")]
        public string WcfUrl
        {
            get { return _wcfUrl; }
            set { _wcfUrl = value; }
        }
        [CategoryAttribute("Impostazioni connessione"), DescriptionAttribute("Username utente ARXivar da utilizzare")]
        public string WcfUsername
        {
            get { return _wcfUsername; }
            set { _wcfUsername = value; }
        }
        [CategoryAttribute("Impostazioni connessione"), DescriptionAttribute("Password utente ARXivar da utilizzare"), PasswordPropertyText(true)]
        public string WcfPassword
        {
            get { return _wcfPassword; }
            set { _wcfPassword = value; }
        }

        [CategoryAttribute("Impostazioni connessione"), DescriptionAttribute("Client Id connessione"), PasswordPropertyText(true)]
        public string ClientId
        {
            get { return _clientId; }
            set { _clientId = value; }
        }
        [CategoryAttribute("Impostazioni connessione"), DescriptionAttribute("Client Secret"), PasswordPropertyText(true)]
        public string ClientSecret
        {
            get { return _clientSecret; }
            set { _clientSecret = value; }
        }

        private string _SapUsername;

        [CategoryAttribute("Impostazioni SAP"), DescriptionAttribute("Username utente Autenticazione SAP")]
        public string SapUsername
        {
            get { return _SapUsername; }
            set { _SapUsername = value; }
        }

        private string _SapPassword;

        [CategoryAttribute("Impostazioni SAP"), DescriptionAttribute("Password utente Autenticazione SAP"), PasswordPropertyText(true)]
        public string SapPassword
        {
            get { return _SapPassword; }
            set { _SapPassword = value; }
        }
        
        private int _secondi;

        [CategoryAttribute("Parametri"), DescriptionAttribute("Secondi per ciclo")]
        public int Secondi
        {
            get { return _secondi; }
            set { _secondi = value; }
        }

        private string _classeDocDDTOut;

        [CategoryAttribute("Classi Documentali"), DescriptionAttribute("DDT Out")]
        public string ClasseDocDDTOut
        {
            get { return _classeDocDDTOut; }
            set { _classeDocDDTOut = value; }
        }

        private string _classeDocDDTIn;

        [CategoryAttribute("Classi Documentali"), DescriptionAttribute("DDT In")]
        public string ClasseDocDDTIn
        {
            get { return _classeDocDDTIn; }
            set { _classeDocDDTIn = value; }
        }

        private string _classeDocInvoiceIn;

        [CategoryAttribute("Classi Documentali"), DescriptionAttribute("Invoice In")]
        public string ClasseDocInvoiceIn
        {
            get { return _classeDocInvoiceIn; }
            set { _classeDocInvoiceIn = value; }
        }

        private string _classeDocInvoiceOut;

        [CategoryAttribute("Classi Documentali"), DescriptionAttribute("Invoice Out")]
        public string ClasseDocInvoiceOut
        {
            get { return _classeDocInvoiceOut; }
            set { _classeDocInvoiceOut = value; }
        }

        private string _classeDocOV;

        [CategoryAttribute("Classi Documentali"), DescriptionAttribute("Ordini di Vendita")]
        public string ClasseDocOV
        {
            get { return _classeDocOV; }
            set { _classeDocOV = value; }
        }

        private string _Aoo;

        [CategoryAttribute("Parametri Import"), DescriptionAttribute("AOO")]
        public string Aoo
        {
            get { return _Aoo; }
            set { _Aoo = value; }
        }

        private string _StatoImport;

        [CategoryAttribute("Parametri Import"), DescriptionAttribute("Stato")]
        public string StatoImport
        {
            get { return _StatoImport; }
            set { _StatoImport = value; }
        }

        private string _rubricaClienti;

        [CategoryAttribute("Parametri Import"), DescriptionAttribute("Rubrica Clienti")]
        public string RubricaClienti
        {
            get { return _rubricaClienti; }
            set { _rubricaClienti = value; }
        }

        private string _rubricaFornitori;

        [CategoryAttribute("Parametri Import"), DescriptionAttribute("Rubrica Fornitori")]
        public string RubricaFornitori
        {
            get { return _rubricaFornitori; }
            set { _rubricaFornitori = value; }
        }

        private string _rubricaAgenti;

        [CategoryAttribute("Parametri Import"), DescriptionAttribute("Rubrica Agenti")]
        public string RubricaAgenti
        {
            get { return _rubricaAgenti; }
            set { _rubricaAgenti = value; }
        }

        private bool _stampaBarcode;

        [CategoryAttribute("Parametri Import"), DescriptionAttribute("Abilita la stampa Barcode")]
        public bool StampaBarcode
        {
            get { return _stampaBarcode; }
            set { _stampaBarcode = value; }
        }

        private string _targetUrl;

        [CategoryAttribute("Parametri Import Documenti"), DescriptionAttribute("Target Url")]
        public string TargetUrl
        {
            get { return _targetUrl; }
            set { _targetUrl = value; }
        }

        private string _usrInt;

        [CategoryAttribute("Parametri Import Documenti"), DescriptionAttribute("USRINT")]
        public string UsrInt
        {
            get { return _usrInt; }
            set { _usrInt = value; }
        }

        private bool _prioritario;

        [CategoryAttribute("Parametri Import Documenti"), DescriptionAttribute("Servizio Prioritario")]
        public bool Prioritario
        {
            get { return _prioritario; }
            set { _prioritario = value; }
        }

        private bool _ImportDDtIn;

        [CategoryAttribute("Parametri Import Documenti"), DescriptionAttribute("Import DDTIN")]
        public bool ImportDDtIn
        {
            get { return _ImportDDtIn; }
            set { _ImportDDtIn = value; }
        }

        private bool _ImportDDtOut;

        [CategoryAttribute("Parametri Import Documenti"), DescriptionAttribute("Import DDtOut")]
        public bool ImportDDtOut
        {
            get { return _ImportDDtOut; }
            set { _ImportDDtOut = value; }
        }

        private bool _ImportInvoiceOut;

        [CategoryAttribute("Parametri Import Documenti"), DescriptionAttribute("Import Invoice Out")]
        public bool ImportInvoiceOut
        {
            get { return _ImportInvoiceOut; }
            set { _ImportInvoiceOut = value; }
        }

        private bool _ImportOv;

        [CategoryAttribute("Parametri Import Documenti"), DescriptionAttribute("Import Ov")]
        public bool ImportOv
        {
            get { return _ImportOv; }
            set { _ImportOv = value; }
        }

        private bool _ImportPregresso;

        [CategoryAttribute("Parametri Import Documenti"), DescriptionAttribute("Import Pregresso")]
        public bool ImportPregresso
        {
            get { return _ImportPregresso; }
            set { _ImportPregresso = value; }
        }

        private DateTime _DataInizioRecuperoPregresso;

        [CategoryAttribute("Parametri Import Documenti"), DescriptionAttribute("Data Inizio Import Pregresso")]
        public DateTime DataInizioRecuperoPregresso
        {
            get { return _DataInizioRecuperoPregresso; }
            set { _DataInizioRecuperoPregresso = value; }
        }

        private int _PrimoGiroGiorni;

        [CategoryAttribute("Parametri Import Documenti"), DescriptionAttribute("Giorni da prendere in considerazione al primo giro")]
        public int primoGiroGiorni
        {
            get { return _PrimoGiroGiorni; }
            set { _PrimoGiroGiorni = value; }
        }
        #endregion

        public ServiceConfiguration()
        {
            // costruttore base
        }

        public ServiceConfiguration(string Class_Id, string Codice, string PathConfiguration)
            : base(Class_Id, Codice, PathConfiguration)
        {
            string assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

            LogLevel = System.Diagnostics.EventLogEntryType.Warning;
            LogPath = Path.Combine(Path.GetDirectoryName(assemblyLocation), "Log");
            //WcfUrl = "net.tcp://localhost:8740/Arxivar/Push";
            WcfUrl = "net.tcp://10.227.52.68:8740/Arxivar/Push";
            WcfUsername = "Admin";
            WcfPassword = "culli.123";
            SapUsername = "S0019098664";
            SapPassword = "Culligan18";
            ClientId = "CulliganCustomPlugin";
            ClientSecret = "E09C56781F004AF69753CE9D622AAD03";
            Prioritario = true;

            primoGiroGiorni = 1;
        }
    }
}
