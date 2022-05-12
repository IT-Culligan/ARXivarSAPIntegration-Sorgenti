using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.IO;
using Abletech.Arxivar.Client;
using Abletech.Arxivar.Entities;
using Abletech.Arxivar.Entities.Enums;
using Abletech.Arxivar.Entities.Search;
using Abletech.Arxivar.Entities.Libraries;
using System.Xml;
using System.Net;

namespace ArxivarSAPIntegration
{
    public partial class ArxivarSAPIntegration : ServiceBase
    {
        private ServiceConfiguration _config;
        private Abletech.Utility.Logging.Xml _logger;
        private WCFConnectorManager _manager;

        private Abletech.Arxivar.Common _common = new Abletech.Arxivar.Common();

        private System.Timers.Timer _timer;
        private int secs = 0;

        private CulliganSapService.OutboundDeliveryGet serviceDDTOut = null;
        private SalesOrderGet.SalesOrderGet serviceOVOut = null;
        private BusinessPartner.BusinessPartnerGet serviceBusinessPartner = null;
        private MaterialDocumentGet.MaterialDocumentGet serviceMaterialDocumentGet = null;
        private SupplierInvoiceGet.SupplierInvoiceGet serviceSupplierInvoiceGet = null;
        private SupplierGetSingle.SupplierGetSingle serviceSupplierGetSingle = null;
        private InvoicesGet.InvoicesGet serviceInvoicesGet = null;

        Dm_CatRubriche _rubricaClienti;
        Dm_CatRubriche _rubricaFornitori;
        Dm_CatRubriche _rubricaAgenti;

        Dm_UtentiBase[] _listaUtenti;

        List<DocumentiImportati> _documentiImportati = new List<DocumentiImportati>();

        List<ElementoUtente> trascodificaUtentiArxivar = new List<ElementoUtente>();

        List<EnumTipoDocumento> ProcedureGiàPartire = new List<EnumTipoDocumento>();

        #region Funzioni per la scrittura dei log
        private void InitLogger()
        {
            string logPath = _config.LogPath;
            if (!Directory.Exists(logPath)) Directory.CreateDirectory(logPath);
            _logger = new Abletech.Utility.Logging.Xml();
            _logger.SetPathSalvataggioDefault(logPath);
            _logger.SetNomeFileDiLogDiDefault(this.SoftwareName);
            _logger.Level = _config.LogLevel;
        }
        private void WriteInfo(string message, params object[] args)
        {
            message = string.Format(message, args);
            _logger.Write(this.SoftwareName, message, System.Diagnostics.EventLogEntryType.Information, false);
        }
        private void WriteWarning(string message, params object[] args)
        {
            message = string.Format(message, args);
            _logger.Write(this.SoftwareName, message, System.Diagnostics.EventLogEntryType.Warning, false);
        }
        private void WriteError(string message, params object[] args)
        {
            message = string.Format(message, args);
            _logger.Write(this.SoftwareName, message, System.Diagnostics.EventLogEntryType.Error, false);
        }
        #endregion

        // proprietà utilizzata per la connessione ad ARXivar e per la scrittura del log
        public string SoftwareName
        {
            get { return "ArxivarSAPIntegration"; }
        }


        public ArxivarSAPIntegration(ServiceConfiguration config)
        {
            _config = config;
            InitializeComponent();
            InitLogger();
        }

        protected override void OnStart(string[] args)
        {
            WriteInfo("Service starting...");
            StartService();
            WriteInfo("Service started");
        }

        protected override void OnStop()
        {
            WriteInfo("Service stopping...");
            StopService();
            WriteInfo("Service stopped");
        }

        public void StartService()
        {
            try
            {

                dataInizio = _config.DataInizioRecuperoPregresso;

                ArxLogonRequest arxLogonRequest = new ArxLogonRequest
                {
                    ClientId = _config.ClientId,
                    ClientSecret = _config.ClientSecret,
                    Username = _config.WcfUsername,
                    Password = _config.WcfPassword,
                    EnablePushEvents = false
                };

                _manager = new WCFConnectorManager(_config.WcfUrl, arxLogonRequest);

                var logonResult = _manager.Logon();

                if (logonResult.LogOnError != ArxLogOnErrorType.None)
                    throw new Exception("Errore di connessione al servizio: " + logonResult.ToString());

                //connessione al servizio WCF
                /*_manager = new WCFConnectorManager(_config.WcfUsername, _config.WcfPassword, _config.WcfUrl, this.SoftwareName);
                ArxLogOnErrorType result = _manager.Logon();
                // verifico l'esito della connessione
                if (result != ArxLogOnErrorType.None) throw new Exception("Errore di connessione al servizio WCF: " + result.ToString());*/

                serviceDDTOut = new CulliganSapService.OutboundDeliveryGet();
                serviceOVOut = new SalesOrderGet.SalesOrderGet();
                serviceBusinessPartner = new BusinessPartner.BusinessPartnerGet();
                serviceMaterialDocumentGet = new MaterialDocumentGet.MaterialDocumentGet();
                serviceSupplierInvoiceGet = new SupplierInvoiceGet.SupplierInvoiceGet();
                serviceSupplierGetSingle = new SupplierGetSingle.SupplierGetSingle();
                serviceInvoicesGet = new InvoicesGet.InvoicesGet();

                serviceDDTOut.Credentials = new System.Net.NetworkCredential(_config.SapUsername, _config.SapPassword);
                serviceOVOut.Credentials = new System.Net.NetworkCredential(_config.SapUsername, _config.SapPassword);
                serviceBusinessPartner.Credentials = new System.Net.NetworkCredential(_config.SapUsername, _config.SapPassword);
                serviceMaterialDocumentGet.Credentials = new System.Net.NetworkCredential(_config.SapUsername, _config.SapPassword);
                serviceSupplierInvoiceGet.Credentials = new System.Net.NetworkCredential(_config.SapUsername, _config.SapPassword);
                serviceSupplierGetSingle.Credentials = new System.Net.NetworkCredential(_config.SapUsername, _config.SapPassword);
                serviceInvoicesGet.Credentials = new System.Net.NetworkCredential(_config.SapUsername, _config.SapPassword);

                var rubriche = _manager.ARX_DATI.Dm_CatRubriche_Get_Data(string.Empty);

                _listaUtenti = _manager.ARX_DATI.Dm_UtentiBase_GetData(string.Empty);

                _rubricaClienti = rubriche.FirstOrDefault(x => x.RUBRICA.Equals(_config.RubricaClienti));
                _rubricaFornitori = rubriche.FirstOrDefault(x => x.RUBRICA.Equals(_config.RubricaFornitori));
                _rubricaAgenti = rubriche.FirstOrDefault(x => x.RUBRICA.Equals(_config.RubricaAgenti));

                trascodificaUtentiArxivar = cercaUtentiTrascodificaSAP();

                secs = System.Convert.ToInt32(_config.Secondi);
                _timer = new System.Timers.Timer(secs * 1000);
                _timer.Elapsed += new System.Timers.ElapsedEventHandler(_timer_Elapsed);
                _timer.Enabled = true;
            }
            catch (Exception ex)
            {
                WriteError("StartService: " + ex.Message);
                throw ex;
            }
        }

        private List<ElementoUtente> cercaUtentiTrascodificaSAP()
        {
            List<ElementoUtente> ritorno = new List<ElementoUtente>();

            using (var search = _manager.ARX_SEARCH.Dm_Profile_Search_Get_New_Instance_By_TipiDocumentoCodice("CONF.UTSAP"))
            using (var select = _manager.ARX_SEARCH.Dm_Profile_Select_Get_New_Instance_By_TipiDocumentoCodice("CONF.UTSAP"))
            {
                select.DOCNUMBER.Selected = true;
                select.DOCNAME.Selected = true;

                string colonnaUtente = _common.SelezionaAggiuntivo(select, "UtenteArxivar", 0);

                search.Stato.SetFilter(Dm_Base_Search_Operatore_String.Uguale, "VALIDO");

                using (var risultatoRicerca = _manager.ARX_SEARCH.Dm_Profile_GetData(search, select, 0))
                {
                    if (risultatoRicerca.RowsCount(0) > 0)
                    {
                        ritorno = risultatoRicerca.GetDataTable(0).AsEnumerable().Select(x => new ElementoUtente
                        {
                            Descrizione = x.Field<string>("DOCNAME"),
                            Utente = _common.trasformainInt(x.Field<string>(colonnaUtente))
                        }).ToList();
                    }
                }
            }

            return ritorno;
        }

        private string formatData(DateTime data)
        {
            string ritorno = string.Empty;

            string mese = data.Month.ToString();
            string giorno = data.Day.ToString();
            string anno = data.Year.ToString();

            if (mese.Length == 1)
                mese = "0" + mese;

            if (giorno.Length == 1)
                giorno = "0" + giorno;

            ritorno = anno + "-" + mese + "-" + giorno + "T00:00:00.000";

            return ritorno;
        }

        public DateTime dataInizio = DateTime.Now; //new DateTime(2018, 12, 31);

        private void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _timer.Enabled = false;
            string idSistema = string.Empty;

            try
            {
                //INSDDTOUT(parsaDocumento(EnumTipoDocumento.DDTOUT));
                INSInvoiceOut(parsaDocumento(EnumTipoDocumento.InvoiceOut));
                INSInvoiceOutFRIZZY(parsaDocumento(EnumTipoDocumento.InvoiceOutFRIZZY));
                //INSInvoiceIn(parsaDocumento(EnumTipoDocumento.InvoiceIn));
                //INSDDTIn(parsaDocumento(EnumTipoDocumento.DDTIN));

                //if (_config.ImportPregresso)
                //{
                //    INSInvoiceIn(parsaDocumento(EnumTipoDocumento.InvoiceIn, dataInizio));

                //    dataInizio = dataInizio.AddDays(-1);
                //}

                //if (!_config.Prioritario)
                //{
                //    //if (_config.ImportDDtOut)
                //    //{
                //    //    INSDDTOUT(parsaDocumento(EnumTipoDocumento.DDTOUT));
                //    //}

                //    //if (_config.ImportOv)
                //    //{
                //    //    INSOV(parsaDocumento(EnumTipoDocumento.OV));
                //    //}

                //    //if (_config.ImportDDtIn)
                //    //{
                //    //    INSDDTIn(parsaDocumento(EnumTipoDocumento.DDTIN));
                //    //}

                //    //INSInvoiceOut(parsaDocumento(EnumTipoDocumento.InvoiceOut));
                //}
                //else
                //{
                //    INSInvoiceIn(parsaDocumento(EnumTipoDocumento.InvoiceIn));
                //}
            }
            catch (Exception errore)
            {
                WriteError("Errore durante l'esecuzione della query: {0}", errore.Message);
            }

            _timer.Enabled = true;
        }

        private List<RecordGestionale> parsaDocumento(EnumTipoDocumento TipoDocumento)
        {
            List<RecordGestionale> ritorno = new List<RecordGestionale>();

            if (TipoDocumento == EnumTipoDocumento.DDTOUT)
            {
                try
                {

                    DateTime dataCall;
                    if (!ProcedureGiàPartire.Contains(EnumTipoDocumento.DDTOUT) && !_config.primoGiroGiorni.Equals(0))
                    {
                        dataCall = DateTime.Now.AddDays(-_config.primoGiroGiorni);
                        ProcedureGiàPartire.Add(EnumTipoDocumento.DDTOUT);
                    }
                    else
                    {
                        dataCall = DateTime.Now.AddDays(-1);
                    }


                    #region DDTIN - VECCHIO METODO
                    ////serviceDDTOut.Timeout = 9000000;
                    //var risultato = serviceDDTOut.CallOutboundDeliveryGet(formatData(dataCall), "arxivar");
                    ////var risultato = serviceDDTOut.CallOutboundDeliveryGet("80027483", "arxivar");

                    //foreach (var elemento in risultato)
                    //{
                    //    RecordGestionale daInserire = new RecordGestionale();

                    //    daInserire.TipoDocumento = TipoDocumento;
                    //    daInserire.numerodocumento = elemento.DeliveryDocument[0];                        
                    //    daInserire.dataDocumento = _common.trasformainData(elemento.DocumentDate[0]);
                    //    daInserire.Utente = elemento.CreatedByUser[0];
                    //    daInserire.tipoDocumento = elemento.DeliveryDocumentType[0];
                    //    daInserire.clienteFatturazione = elemento.SoldToParty[0];
                    //    daInserire.clienteSpedizione = elemento.ShipToParty[0];
                    //    daInserire.destinazioneMerce = serviceBusinessPartner.CallBusinessPartnerGet("2018-07-05T00:00:00.000", "single", elemento.ShipToParty[0])[0].BusinessPartnerFullName[0]; 
                    //    daInserire.barcode = string.Format("{0}{1}{2}{3}", daInserire.numerodocumento, daInserire.dataDocumento.Year, daInserire.dataDocumento.Month.ToString().PadLeft(2, '0'), daInserire.dataDocumento.Day.ToString().PadLeft(2, '0'));

                    //    ritorno.Add(daInserire);
                    //}
                    #endregion

                    #region DDTIN - NUOVO METODO

                    List<SAP_Testate_Outbound_Delivery_Arxivar> ListaOutbound = new List<SAP_Testate_Outbound_Delivery_Arxivar>();
                    ListaOutbound = IntegratorHelper.GetListaOutbound();

                    if (ListaOutbound != null)
                    {
                        int counter = 1;
                        foreach (SAP_Testate_Outbound_Delivery_Arxivar Doc in ListaOutbound)
                        {
                            Console.WriteLine(counter.ToString());

                            RecordGestionale daInserire = new RecordGestionale();
                            daInserire.TipoDocumento = EnumTipoDocumento.DDTOUT;
                            daInserire.numerodocumento = Doc.DeliveryDocument;
                            daInserire.dataDocumento = System.Convert.ToDateTime(Doc.DocumentDate);
                            daInserire.Utente = Doc.CreatedByUser;
                            daInserire.tipoDocumento = Doc.DeliveryDocumentType;
                            daInserire.clienteFatturazione = Doc.SoldToParty;
                            daInserire.clienteSpedizione = Doc.ShipToParty;
                            daInserire.destinazioneMerce = "ND"; //serviceBusinessPartner.CallBusinessPartnerGet("2018-07-05T00:00:00.000", "single", Doc.ShipToParty)[0].BusinessPartnerFullName[0];
                            daInserire.barcode = string.Format("{0}{1}{2}{3}{4}", Doc.DeliveryDocumentType, Doc.DeliveryDocument, System.Convert.ToDateTime(Doc.DocumentDate).Day.ToString().PadLeft(2, '0'), System.Convert.ToDateTime(Doc.DocumentDate).Month.ToString().PadLeft(2, '0'), System.Convert.ToDateTime(Doc.DocumentDate).Year);
                            daInserire.oggetto = string.Format("DDT Vendita nr: {0} del {1}", daInserire.numerodocumento, daInserire.dataDocumento.ToShortDateString());
                            ritorno.Add(daInserire);

                            counter++;
                        }
                    }

                    #endregion
                }
                catch (Exception errore)
                {
                    WriteError("Errore in parsaDocumento:{0}", errore.Message);
                }
            }
            else if (TipoDocumento == EnumTipoDocumento.OV)
            {
                try
                {

                    DateTime dataCall;
                    if (!ProcedureGiàPartire.Contains(EnumTipoDocumento.OV))
                    {
                        dataCall = DateTime.Now.AddDays(-_config.primoGiroGiorni);
                        ProcedureGiàPartire.Add(EnumTipoDocumento.OV);
                    }
                    else
                    {
                        dataCall = DateTime.Now.AddDays(-1);
                    }


                    var risultato = serviceOVOut.CallSalesOrderGet(formatData(dataCall), "arxivar");

                    foreach (var elemento in risultato)
                    {
                        RecordGestionale daInserire = new RecordGestionale();

                        daInserire.TipoDocumento = TipoDocumento;

                        daInserire.codiceClienteFornitore = elemento.SoldToParty[0];
                        daInserire.dataDocumento = _common.trasformainData(elemento.SalesOrderDate[0]);
                        daInserire.numerodocumento = elemento.SalesOrder[0];
                        daInserire.esercizio = daInserire.dataDocumento.Year;

                        daInserire.vostroDocumento = elemento.PurchaseOrderByCustomer[0];
                        daInserire.vostroDocumentodata = _common.trasformainData(elemento.CustomerPurchaseOrderDate[0]);

                        daInserire.oggetto = "ORD. VENDITA n° " + daInserire.numerodocumento + " del " + daInserire.dataDocumento.ToShortDateString();

                        daInserire.OrderReason = elemento.SDDocumentReason[0];

                        daInserire.tipoDocumento = elemento.SalesOrderType[0];
                        daInserire.codiceNumero = _common.trasformainInt(elemento.SalesOrder[0]);
                        daInserire.destinazioneMerce = elemento.SalesOffice[0];

                        daInserire.barcode = string.Format("{0}{1}{2}{3}", daInserire.numerodocumento, daInserire.dataDocumento.Year, daInserire.dataDocumento.Month.ToString().PadLeft(2, '0'), daInserire.dataDocumento.Day.ToString().PadLeft(2, '0'));

                        foreach (var v in elemento.to_Partner)
                        {
                            if (v.PartnerFunction[0].Equals("SH"))
                            {
                                daInserire.clienteSpedizione = v.Customer[0];
                            }
                        }

                        ritorno.Add(daInserire);
                    }
                }
                catch (Exception errore)
                {
                    WriteError("Errore in parsaDocumento:{0}", errore.Message);
                }
            }
            else if (TipoDocumento == EnumTipoDocumento.DDTIN)
            {
                try
                {
                    DateTime dataCall;
                    if (!ProcedureGiàPartire.Contains(EnumTipoDocumento.DDTIN))
                    {
                        dataCall = DateTime.Now.AddDays(-_config.primoGiroGiorni);
                        ProcedureGiàPartire.Add(EnumTipoDocumento.DDTIN);
                    }
                    else
                    {
                        dataCall = DateTime.Now.AddDays(-1);
                    }

                    serviceMaterialDocumentGet.Timeout = 9000000;
                    //var risultato = serviceMaterialDocumentGet.CallMaterialDocumentGet(formatData(dataCall), "arxivar");
                    var risultato = serviceMaterialDocumentGet.CallMaterialDocumentGet("2019-12-31T00:00:00.000", "arxivar");

                    foreach (var elemento in risultato)
                    {
                        RecordGestionale daInserire = new RecordGestionale();

                        daInserire.TipoDocumento = TipoDocumento;

                        daInserire.codiceContatore = elemento.to_MaterialDocumentHeader[0].A_MaterialDocumentHeaderType.InventoryTransactionType[0];
                        daInserire.numerodocumento = elemento.MaterialDocument[0];
                        daInserire.dataDocumento = _common.trasformainData(elemento.to_MaterialDocumentHeader[0].A_MaterialDocumentHeaderType.PostingDate[0].ToString());
                        daInserire.vostroDocumento = elemento.to_MaterialDocumentHeader[0].A_MaterialDocumentHeaderType.ReferenceDocument[0];
                        daInserire.vostroDocumentodata = _common.trasformainData(elemento.to_MaterialDocumentHeader[0].A_MaterialDocumentHeaderType.DocumentDate[0].ToString());
                        daInserire.codiceClienteFornitore = elemento.Supplier[0];
                        daInserire.barcode = string.Format("{0}{1}{2}{3}", daInserire.numerodocumento, daInserire.dataDocumento.Year, daInserire.dataDocumento.Month.ToString().PadLeft(2, '0'), daInserire.dataDocumento.Day.ToString().PadLeft(2, '0'));
                        daInserire.oggetto = "numero " + daInserire.vostroDocumento + " del " + daInserire.vostroDocumentodata.ToShortDateString();


                        ritorno.Add(daInserire);
                    }
                }
                catch (Exception errore)
                {
                    WriteError("Errore in parsaDocumento:{0}", errore.Message);
                }
            }
            else if (TipoDocumento == EnumTipoDocumento.InvoiceIn)
            {
                try
                {
                    #region IMPORT DOCUMENTI CONSERVAZIONE
                    List<SAP_Open_Item_CE_Fornitori> ListaFatturePassive = new List<SAP_Open_Item_CE_Fornitori>();
                    ListaFatturePassive = IntegratorHelper.GetFatturePassive();

                    if (ListaFatturePassive != null)
                    {
                        int counter = 1;
                        foreach (SAP_Open_Item_CE_Fornitori Doc in ListaFatturePassive)
                        {
                            Console.WriteLine(counter.ToString());

                            RecordGestionale daInserire = new RecordGestionale();
                            daInserire.TipoDocumento = EnumTipoDocumento.InvoiceIn;

                            daInserire.contator = _common.trasformainInt(Doc.AccountingDocument);
                            daInserire.registroIva = "A";
                            daInserire.protocollo = Doc.AccountingDocument;
                            daInserire.tipoDocumento = "FP";
                            daInserire.codiceContatore = Doc.AccountingDocumentType;
                            //daInserire.dataDocumento = _common.trasformainData(elemento.PostingDate[0]);//_common.trasformainData(elemento.DocumentDate[0]);
                            //daInserire.dataprotocollo = _common.trasformainData(elemento.DocumentDate[0]);//_common.trasformainData(elemento.PostingDate[0]);


                            daInserire.dataDocumento = Doc.DocumentDate.Value;//_common.trasformainData(elemento.DocumentDate[0]);
                            daInserire.dataprotocollo = Doc.PostingDate.Value;//_common.trasformainData(elemento.PostingDate[0]);

                            daInserire.numerodocumento = Doc.DocumentReferenceID;
                            daInserire.codiceClienteFornitore = Doc.Supplier;
                            daInserire.azienda = Doc.SupplierName;
                            daInserire.Utente = Doc.AccountingDocCreatedByUser;
                            daInserire.AccountingDocumentType = Doc.AccountingDocumentType;
                            daInserire.esercizio = _common.trasformainInt(Doc.FiscalYear);

                            daInserire.oggetto = "Fattura. n° " + daInserire.numerodocumento + " del " + daInserire.dataDocumento.ToShortDateString();

                            ritorno.Add(daInserire);

                            counter++;
                        }
                    }
                    #endregion

                    #region DEFAULT 
                    DateTime dataCall;
                    if (!ProcedureGiàPartire.Contains(EnumTipoDocumento.InvoiceIn) && !_config.primoGiroGiorni.Equals(0))
                    {
                        dataCall = DateTime.Now.AddDays(-_config.primoGiroGiorni);
                        ProcedureGiàPartire.Add(EnumTipoDocumento.InvoiceIn);
                    }
                    else
                    {
                        dataCall = DateTime.Now;
                    }

                    //var risultato = serviceSupplierInvoiceGet.CallSupplierInvoiceGet(formatData(dataCall).ToString(), "IT01", formatData(DateTime.Now.AddDays(-7)).ToString());
                    //var risultato = serviceSupplierInvoiceGet.CallSupplierInvoiceGet("2018-12-29T00:00:00.000", "IT01", "2018-12-29T00:00:00.000");

                    //foreach (var elemento in risultato)
                    //{
                    //    RecordGestionale daInserire = new RecordGestionale();

                    //    daInserire.TipoDocumento = TipoDocumento;

                    //    daInserire.contator = _common.trasformainInt(elemento.AccountingDocument[0]);
                    //    daInserire.registroIva = "A";
                    //    daInserire.protocollo = elemento.AccountingDocument[0];
                    //    daInserire.tipoDocumento = "FP";
                    //    daInserire.codiceContatore = elemento.AccountingDocumentType[0];
                    //    //daInserire.dataDocumento = _common.trasformainData(elemento.PostingDate[0]);//_common.trasformainData(elemento.DocumentDate[0]);
                    //    //daInserire.dataprotocollo = _common.trasformainData(elemento.DocumentDate[0]);//_common.trasformainData(elemento.PostingDate[0]);

                    //    daInserire.dataDocumento = _common.trasformainData(elemento.DocumentDate[0]);//_common.trasformainData(elemento.DocumentDate[0]);
                    //    daInserire.dataprotocollo = _common.trasformainData(elemento.PostingDate[0]);//_common.trasformainData(elemento.PostingDate[0]);

                    //    daInserire.numerodocumento = elemento.AssignmentReference[0];
                    //    daInserire.codiceClienteFornitore = elemento.Supplier[0];
                    //    daInserire.azienda = elemento.SupplierName[0];
                    //    daInserire.Utente = elemento.AccountingDocCreatedByUser[0];
                    //    daInserire.AccountingDocumentType = elemento.AccountingDocumentType[0];
                    //    daInserire.esercizio = _common.trasformainInt(elemento.FiscalYear[0]);

                    //    daInserire.oggetto = "Fattura. n° " + daInserire.numerodocumento + " del " + daInserire.dataDocumento.ToShortDateString();

                    //    ritorno.Add(daInserire);

                        
                    //}
                    #endregion
                }
                catch (Exception errore)
                {
                    WriteError("Errore in parsaDocumento:{0}", errore.Message);
                }
            }
            else if (TipoDocumento == EnumTipoDocumento.InvoiceOut)
            {

                DateTime dataCall;
                if (!ProcedureGiàPartire.Contains(EnumTipoDocumento.InvoiceOut))
                {
                    dataCall = DateTime.Now.AddDays(-_config.primoGiroGiorni);
                    ProcedureGiàPartire.Add(EnumTipoDocumento.InvoiceOut);
                }
                else
                {
                    dataCall = DateTime.Now.AddDays(-1);
                }

                serviceInvoicesGet.Timeout = 9000000;

                //using (SAP_BI_oldEntities context = new SAP_BI_oldEntities())
                using (SAPEntities context = new SAPEntities())
                {

   //                 DateTime da = System.Convert.ToDateTime("01/01/2022");
                    DateTime a = System.Convert.ToDateTime("17/04/2022");
                   // DateTime a = DateTime.Now.AddDays(-(int)DateTime.Now.DayOfWeek);
                    //DateTime a = DateTime.Now.AddDays(-1);




                    var result = from t in context.FE_PDF_Arxivar_CHECK
                                 where t.BillingDocumentType != "S1" && t.BillingDocumentType != "S2"
                             && t.FiscalYear == "2022" && t.BillingDocumentDate <= a
                                 select t;

                    //var result = from t in context.PDF_Arxivar_RECUPERO_2018
                    //             select t;


                    int tot = result.Count();

                    //foreach (var elemento in risultato)
                    foreach (var elemento in result)
                    {
                        RecordGestionale daInserire = new RecordGestionale();

                        daInserire.TipoDocumento = TipoDocumento;

                        daInserire.codiceClienteFornitore = elemento.SoldToParty;
                        daInserire.codiceContatore = elemento.BillingDocumentType;
                        daInserire.numerodocumento = elemento.AccountingDocument;
                        daInserire.importo = System.Convert.ToDouble(elemento.TotalNetAmount);
                        daInserire.protocollo = (elemento.BillingDocument);

                        if (string.IsNullOrEmpty(daInserire.numerodocumento))
                        {
                            continue;
                        }

                        daInserire.dataDocumento = _common.trasformainData(elemento.BillingDocumentDate.ToString());
                        daInserire.BillingDocument = elemento.BillingDocument;
                        daInserire.registroIva = "A";
                        daInserire.protocollo = daInserire.BillingDocument;
                        daInserire.esercizio = _common.trasformainInt(elemento.FiscalYear);

                        daInserire.cdIndi = elemento.VATRegistrationCountry;

                        daInserire.oggetto = "Fattura. n° " + daInserire.numerodocumento + " del " + daInserire.dataDocumento.ToShortDateString();

                        ritorno.Add(daInserire);
                    }
                }
            }
            else if (TipoDocumento == EnumTipoDocumento.InvoiceOutFRIZZY)
            {

                DateTime dataCall;
                if (!ProcedureGiàPartire.Contains(EnumTipoDocumento.InvoiceOutFRIZZY))
                {
                    dataCall = DateTime.Now.AddDays(-_config.primoGiroGiorni);
                    ProcedureGiàPartire.Add(EnumTipoDocumento.InvoiceOut);
                }
                else
                {
                    dataCall = DateTime.Now.AddDays(-1);
                }

                serviceInvoicesGet.Timeout = 9000000;

                //using (SAP_BI_oldEntities context = new SAP_BI_oldEntities())
                using (SAPEntities context = new SAPEntities())
                {

                    DateTime da = System.Convert.ToDateTime("01/01/2022");
                    DateTime a = System.Convert.ToDateTime("17/04/2022");
                    //DateTime a = System.Convert.ToDateTime("19/12/2021");
                   // DateTime a = DateTime.Now.Date.AddDays(-1);




                    var result = from t in context.FE_PDF_Arxivar_CHECK_FRIZZY
                                 where t.BillingDocumentType != "S1" && t.BillingDocumentType != "S2"
                             && t.FiscalYear == "2022" && t.BillingDocumentDate <= a
                                 select t;

                    //var result = from t in context.PDF_Arxivar_RECUPERO_2018
                    //             select t;


                    int tot = result.Count();

                    //foreach (var elemento in risultato)
                    foreach (var elemento in result)
                    {
                        RecordGestionale daInserire = new RecordGestionale();

                        daInserire.TipoDocumento = TipoDocumento;

                        daInserire.codiceClienteFornitore = elemento.SoldToParty;
                        daInserire.codiceContatore = elemento.BillingDocumentType;
                        daInserire.numerodocumento = elemento.AccountingDocument;
                        daInserire.importo = System.Convert.ToDouble(elemento.TotalNetAmount);
                        daInserire.protocollo = (elemento.BillingDocument);

                        if (string.IsNullOrEmpty(daInserire.numerodocumento))
                        {
                            continue;
                        }

                        daInserire.dataDocumento = _common.trasformainData(elemento.BillingDocumentDate.ToString());
                        daInserire.BillingDocument = elemento.BillingDocument;
                        daInserire.registroIva = "A";
                        daInserire.azienda = "FRIZZY";
                        daInserire.protocollo = daInserire.BillingDocument;
                        daInserire.esercizio = _common.trasformainInt(elemento.FiscalYear);

                        daInserire.cdIndi = elemento.VATRegistrationCountry;

                        daInserire.oggetto = "Fattura. n° " + daInserire.numerodocumento + " del " + daInserire.dataDocumento.ToShortDateString();

                        ritorno.Add(daInserire);
                    }
                }
            }

            return ritorno;
        }

        private List<RecordGestionale> parsaDocumento(EnumTipoDocumento TipoDocumento, DateTime dataCall)
        {
            List<RecordGestionale> ritorno = new List<RecordGestionale>();
            if (TipoDocumento == EnumTipoDocumento.InvoiceIn)
            {
                try
                {
                    var risultato = serviceSupplierInvoiceGet.CallSupplierInvoiceGet(formatData(dataCall).ToString(), "IT01", formatData(DateTime.Now.AddDays(-7)).ToString());
                    //var risultato = serviceSupplierInvoiceGet.CallSupplierInvoiceGet("2010-01-01T00:00:00.000", "IT01", "2010-01-01T00:00:00.000");

                    foreach (var elemento in risultato)
                    {
                        RecordGestionale daInserire = new RecordGestionale();

                        daInserire.TipoDocumento = TipoDocumento;

                        daInserire.contator = _common.trasformainInt(elemento.AccountingDocument[0]);
                        daInserire.registroIva = "A";
                        daInserire.protocollo = elemento.AccountingDocument[0];
                        daInserire.tipoDocumento = "FP";
                        daInserire.codiceContatore = elemento.AccountingDocumentType[0];
                        //daInserire.dataDocumento = _common.trasformainData(elemento.PostingDate[0]);//_common.trasformainData(elemento.DocumentDate[0]);
                        //daInserire.dataprotocollo = _common.trasformainData(elemento.DocumentDate[0]);//_common.trasformainData(elemento.PostingDate[0]);

                        daInserire.dataDocumento = _common.trasformainData(elemento.DocumentDate[0]);//_common.trasformainData(elemento.DocumentDate[0]);
                        daInserire.dataprotocollo = _common.trasformainData(elemento.PostingDate[0]);//_common.trasformainData(elemento.PostingDate[0]);

                        daInserire.numerodocumento = elemento.AssignmentReference[0];
                        daInserire.codiceClienteFornitore = elemento.Supplier[0];
                        daInserire.azienda = elemento.SupplierName[0];
                        daInserire.Utente = elemento.AccountingDocCreatedByUser[0];
                        daInserire.AccountingDocumentType = elemento.AccountingDocumentType[0];
                        daInserire.esercizio = _common.trasformainInt(elemento.FiscalYear[0]);

                        daInserire.oggetto = "Fattura. n° " + daInserire.numerodocumento + " del " + daInserire.dataDocumento.ToShortDateString();

                        ritorno.Add(daInserire);
                    }
                }
                catch (Exception errore)
                {
                    WriteError("Errore in parsaDocumento:{0}", errore.Message);
                }
            }

            return ritorno;
        }

        public void StopService()
        {
            try
            {
                // qui eseguire ulteriori attività di stop (es. stop di timer)

                // disconnessione dal servizio WCF
                if (_manager != null) _manager.Dispose();
            }
            catch (Exception ex)
            {
                WriteError("StopService: " + ex.Message);
            }
        }

        #region Importazione

        private void INSDDTOUT(List<RecordGestionale> elementoDaInserirelist)
        {
            foreach (var elementoDaInserire in elementoDaInserirelist)
            {
                try
                {
                    if (checkDDTOut(elementoDaInserire))
                    {
                        continue;
                    }

                    WriteInfo("Sto processando l'ID Gestionale {0}", elementoDaInserire.ID);

                    if (string.IsNullOrEmpty(_config.ClasseDocDDTOut))
                    {
                        throw new Exception("La classe documentale DDt Cliente è vuota.");
                    }

                    using (var documentoDaInserire = _manager.ARX_DATI.Dm_Profile_ForInsert_Get_New_Instance_ByDocumentTypeCodice(_config.ClasseDocDDTOut))
                    {
                        //docu cicico = serviceBusinessPartner.CallBusinessPartnerGet("2018-07-05T00:00:00.000", "single", elementoDaInserire.clienteFatturazione);
                        ElementoAnagraficaCliFor anagraficaCliente = parsaAnagrafica(elementoDaInserire, EnumTipoAnagrafica.Clienti);

                        if (anagraficaCliente == null)
                        {
                            IntegratorHelper.WriteResult(elementoDaInserire.numerodocumento, "Problemi anagrafica cliente.");
                            WriteError("Errore: la ricerca del codice {0} non ha prodotto risultati. Non lo processo fino al prossimo avvio", elementoDaInserire.clienteFatturazione);
                            _documentiImportati.Add(new DocumentiImportati { Tipo = EnumTipoDocumento.DDTOUT, Chiave = formattaCodiceDDTOut(elementoDaInserire) });
                            throw new Exception(string.Format("Errore durante la ricerca del codice {0}", elementoDaInserire.clienteFatturazione));
                        }
                        documentoDaInserire.Aoo = _config.Aoo;
                        documentoDaInserire.Stato = _config.StatoImport;
                        documentoDaInserire.InOut = DmProfileInOut.Uscita;

                        int idrubricaCliente = CercaRubricaperCodice(anagraficaCliente, _rubricaClienti.RUBRICA, _rubricaClienti.ID, 2);

                        if (idrubricaCliente == 0)
                        {
                            throw new Exception(string.Format("Errore: non è stato possibile inserire il fornitore in rubrica per il record {0}", elementoDaInserire.codiceClienteFornitore));
                        }

                        Dm_DatiProfilo Destinatario = _manager.ARX_DATI.Dm_DatiProfilo_GetNewInstance_From_IdRubrica(idrubricaCliente, Dm_DatiProfilo_Campo.DE);
                        documentoDaInserire.To.Add(Destinatario);

                        documentoDaInserire.ProtocolloInterno = elementoDaInserire.numerodocumento.ToString();
                        documentoDaInserire.DataDoc = elementoDaInserire.dataDocumento;

                        documentoDaInserire.DocName = elementoDaInserire.oggetto;

                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "CODCON", elementoDaInserire.codiceContatore);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "NUMDOC", elementoDaInserire.numerodocumento);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "DATADOC", elementoDaInserire.dataDocumento);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "STRUTTURA", elementoDaInserire.struttura);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "DESTMERCE", elementoDaInserire.destinazioneMerce);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "PIVA", Destinatario.PARTIVA);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "CODFIS", Destinatario.CODFIS);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "ESERCIZIO", elementoDaInserire.dataDocumento.Year);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "NUMREG", elementoDaInserire.codiceNumero);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "CODCLIFOR", elementoDaInserire.clienteFatturazione);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "Cd_Clien", elementoDaInserire.clienteFatturazione);

                        string barcode = string.Empty;

                        if (!string.IsNullOrEmpty(elementoDaInserire.barcode))
                        {
                            barcode = elementoDaInserire.barcode;
                        }

                        using (var risultatoInserimento = _manager.ARX_DATI.Dm_Profile_Insert_For_Barcode(documentoDaInserire, barcode))
                        {
                            if (risultatoInserimento.EXCEPTION != Security_Exception.Nothing)
                            {
                                _documentiImportati.Add(new DocumentiImportati { Tipo = EnumTipoDocumento.DDTOUT, Chiave = formattaCodiceDDTOut(elementoDaInserire) });
                                WriteError("L'inserimento del documento id {0} nella classe DDTOUT non è andato a buon fine: {1}. Non verrà riprocessato fino al riavvio", elementoDaInserire.ID, risultatoInserimento.MESSAGE);
                            }
                            else
                            {
                                _documentiImportati.Add(new DocumentiImportati { Tipo = EnumTipoDocumento.DDTOUT, Chiave = formattaCodiceDDTOut(elementoDaInserire) });
                                WriteWarning("Il documento {0} nella classe DDTOUT è stato inserito regolarmente docnumber {1}", elementoDaInserire.ID, risultatoInserimento.PROFILE.DOCNUMBER);
                                _manager.ARX_DATI.SD_ASSOCDOC_Insert(risultatoInserimento.PROFILE.DOCNUMBER, formattaCodiceDDTOut(elementoDaInserire), string.Empty);

                                if (!string.IsNullOrEmpty(elementoDaInserire.barcode))
                                {
                                    _manager.ARX_DATI.Dm_Barcode_Insert(risultatoInserimento.PROFILE.DOCNUMBER, Dm_Barcode_TipoImpronta.N, elementoDaInserire.barcode);
                                }
                            }
                        }
                    }

                    if (_manager.UserGrantor != null)
                    {
                        _manager.DeImpersonate();
                    }
                }
                catch (Exception errore)
                {
                    _documentiImportati.Add(new DocumentiImportati { Tipo = EnumTipoDocumento.DDTOUT, Chiave = formattaCodiceDDTOut(elementoDaInserire) });
                    WriteError("Errore in nella procedura inserimento:{0}", errore.Message);
                }

                IntegratorHelper.SetRead(elementoDaInserire.numerodocumento, true);
            }

        }

        private void INSOV(List<RecordGestionale> elementoDaInserireList)
        {
            if (string.IsNullOrEmpty(_config.ClasseDocOV))
            {
                throw new Exception("La classe documentale Ordini di Vendita è vuota.");
            }

            foreach (var elementoDaInserire in elementoDaInserireList)
            {
                try
                {

                    if (checkOV(elementoDaInserire))
                    {
                        continue;
                    }

                    WriteInfo("Sto processando l'ID Gestionale {0}", elementoDaInserire.ID);

                    using (var documentoDaInserire = _manager.ARX_DATI.Dm_Profile_ForInsert_Get_New_Instance_ByDocumentTypeCodice(_config.ClasseDocOV))
                    {
                        documentoDaInserire.Aoo = _config.Aoo;
                        documentoDaInserire.Stato = _config.StatoImport;
                        documentoDaInserire.InOut = DmProfileInOut.Uscita;

                        ElementoAnagraficaCliFor anagraficaCliente = parsaAnagrafica(elementoDaInserire, EnumTipoAnagrafica.Clienti);

                        if (anagraficaCliente == null)
                        {
                            WriteError("Errore: la ricerca del codice {0} non ha prodotto risultati. Non lo processo fino al prossimo avvio", elementoDaInserire.clienteFatturazione);
                            _documentiImportati.Add(new DocumentiImportati { Tipo = EnumTipoDocumento.OV, Chiave = formattaCodiceOV(elementoDaInserire) });
                            throw new Exception(string.Format("Errore durante la ricerca del codice {0}", elementoDaInserire.clienteFatturazione));
                        }
                        documentoDaInserire.Aoo = _config.Aoo;
                        documentoDaInserire.Stato = _config.StatoImport;
                        documentoDaInserire.InOut = DmProfileInOut.Uscita;

                        int idrubricaCliente = CercaRubricaperCodice(anagraficaCliente, _rubricaClienti.RUBRICA, _rubricaClienti.ID, 2);

                        if (idrubricaCliente == 0)
                        {
                            throw new Exception(string.Format("Errore: non è stato possibile inserire il fornitore in rubrica per il record {0}", elementoDaInserire.codiceClienteFornitore));
                        }

                        Dm_DatiProfilo Destinatario = _manager.ARX_DATI.Dm_DatiProfilo_GetNewInstance_From_IdRubrica(idrubricaCliente, Dm_DatiProfilo_Campo.DE);
                        documentoDaInserire.To.Add(Destinatario);

                        int idRubricaCodiceDestinatario = CercaRubricaperCodice(anagraficaCliente, _rubricaClienti.RUBRICA, _rubricaClienti.ID, 2);
                        Dm_DatiProfilo DestinatarioMerce = _manager.ARX_DATI.Dm_DatiProfilo_GetNewInstance_From_IdRubrica(idRubricaCodiceDestinatario, Dm_DatiProfilo_Campo.DE);

                        documentoDaInserire.ProtocolloInterno = elementoDaInserire.numerodocumento.ToString();
                        documentoDaInserire.DataDoc = elementoDaInserire.dataDocumento;

                        documentoDaInserire.DocName = elementoDaInserire.oggetto;

                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "CODCON", elementoDaInserire.codiceContatore);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "NUMDOC", elementoDaInserire.numerodocumento);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "DATADOC", elementoDaInserire.dataDocumento);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "STRUTTURA", elementoDaInserire.struttura);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "AGENTE", elementoDaInserire.agente);

                        if (DestinatarioMerce != null)
                        {
                            _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "DESTMERCE", DestinatarioMerce.CONTATTO);
                        }

                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "PIVA", elementoDaInserire.partitaIva);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "CODFIS", elementoDaInserire.codiceFiscale);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "ESERCIZIO", elementoDaInserire.dataDocumento.Year);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "NUMREG", elementoDaInserire.codiceNumero);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "RIFCLIDOC", elementoDaInserire.vostroDocumento);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "RIFCLIDOCDATA", elementoDaInserire.vostroDocumentodata);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "CODCLIFOR", elementoDaInserire.codiceClienteFornitore);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "NR_OPPORTUNITA", elementoDaInserire.idOpportunità);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "OrderReason", elementoDaInserire.OrderReason);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "Filiale", elementoDaInserire.filialeCRM);
                        string barcode = string.Empty;

                        if (!string.IsNullOrEmpty(elementoDaInserire.barcode))
                        {
                            barcode = elementoDaInserire.barcode;
                        }

                        using (var risultatoInserimento = _manager.ARX_DATI.Dm_Profile_Insert_For_Barcode(documentoDaInserire, barcode))
                        {
                            if (risultatoInserimento.EXCEPTION != Security_Exception.Nothing)
                            {
                                _documentiImportati.Add(new DocumentiImportati { Tipo = EnumTipoDocumento.OV, Chiave = formattaCodiceOV(elementoDaInserire) });
                                WriteError("L'inserimento del documento id {0} nella classe OV non è andato a buon fine: {1}. Non verrà riprocessato fino al riavvio", elementoDaInserire.ID, risultatoInserimento.MESSAGE);
                            }
                            else
                            {
                                _manager.ARX_DATI.SD_ASSOCDOC_Insert(risultatoInserimento.PROFILE.DOCNUMBER, formattaCodiceOV(elementoDaInserire), string.Empty);
                                _documentiImportati.Add(new DocumentiImportati { Tipo = EnumTipoDocumento.OV, Chiave = formattaCodiceOV(elementoDaInserire) });
                                WriteWarning("Il documento {0} nella classe Ordine di Vendita è stato inserito regolarmente docnumber {1}", elementoDaInserire.ID, risultatoInserimento.PROFILE.DOCNUMBER);

                                if (!string.IsNullOrEmpty(elementoDaInserire.barcode))
                                {
                                    _manager.ARX_DATI.Dm_Barcode_Insert(risultatoInserimento.PROFILE.DOCNUMBER, Dm_Barcode_TipoImpronta.N, elementoDaInserire.barcode);
                                }
                            }
                        }
                    }

                    if (_manager.UserGrantor != null)
                    {
                        _manager.DeImpersonate();
                    }
                }
                catch (Exception errore)
                {
                    _documentiImportati.Add(new DocumentiImportati { Tipo = EnumTipoDocumento.OV, Chiave = formattaCodiceOV(elementoDaInserire) });
                    WriteError("Errore in nella procedura inserimento:{0}", errore.Message);
                }
            }

        }

        private void INSInvoiceOut(List<RecordGestionale> elementoDaInserireList)
        {
            if (string.IsNullOrEmpty(_config.ClasseDocInvoiceOut))
            {
                throw new Exception("La classe documentale Ordini di Vendita è vuota.");
            }
            int i = 0;
            foreach (var elementoDaInserire in elementoDaInserireList)
            {
                i++;
                try
                {

                    if (checkInvoiceOut(elementoDaInserire))
                    {
                        continue;
                    }

                    WriteInfo("Sto processando l'ID Gestionale {0}", elementoDaInserire.ID);

                    using (var documentoDaInserire = _manager.ARX_DATI.Dm_Profile_ForInsert_Get_New_Instance_ByDocumentTypeCodice(_config.ClasseDocInvoiceOut))
                    {
                        documentoDaInserire.Aoo = _config.Aoo;
                        documentoDaInserire.Stato = _config.StatoImport;
                        documentoDaInserire.InOut = DmProfileInOut.Uscita;

                        ElementoAnagraficaCliFor anagraficaCliente = parsaAnagrafica(elementoDaInserire, EnumTipoAnagrafica.Clienti);

                        if (anagraficaCliente == null)
                        {
                            WriteError("Errore: la ricerca del codice {0} non ha prodotto risultati. Non lo processo fino al prossimo avvio", elementoDaInserire.clienteFatturazione);
                            _documentiImportati.Add(new DocumentiImportati { Tipo = EnumTipoDocumento.InvoiceOut, Chiave = formattaCodiceInvoiceOut(elementoDaInserire) });
                            throw new Exception(string.Format("Errore durante la ricerca del codice {0}", elementoDaInserire.clienteFatturazione));
                        }
                        documentoDaInserire.Aoo = _config.Aoo;
                        documentoDaInserire.Stato = _config.StatoImport;
                        documentoDaInserire.InOut = DmProfileInOut.Uscita;

                        int idrubricaCliente = CercaRubricaperCodice(anagraficaCliente, _rubricaClienti.RUBRICA, _rubricaClienti.ID, 2);

                        if (idrubricaCliente == 0)
                        {
                            throw new Exception(string.Format("Errore: non è stato possibile inserire il fornitore in rubrica per il record {0}", elementoDaInserire.codiceClienteFornitore));
                        }

                        Dm_DatiProfilo Destinatario = _manager.ARX_DATI.Dm_DatiProfilo_GetNewInstance_From_IdRubrica(idrubricaCliente, Dm_DatiProfilo_Campo.DE);
                        documentoDaInserire.To.Add(Destinatario);

                        int idRubricaCodiceDestinatario = CercaRubricaperCodice(anagraficaCliente, _rubricaClienti.RUBRICA, _rubricaClienti.ID, 2);
                        Dm_DatiProfilo DestinatarioMerce = _manager.ARX_DATI.Dm_DatiProfilo_GetNewInstance_From_IdRubrica(idRubricaCodiceDestinatario, Dm_DatiProfilo_Campo.DE);

                        documentoDaInserire.ProtocolloInterno = elementoDaInserire.numerodocumento.ToString();
                        documentoDaInserire.DataDoc = elementoDaInserire.dataDocumento;

                        documentoDaInserire.DocName = elementoDaInserire.oggetto;

                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "PIVA", anagraficaCliente.Piva);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "CODFIS", anagraficaCliente.CodiceFiscale);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "EMAIL", anagraficaCliente.Email);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "PEC", anagraficaCliente.EmailPec);


                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "CODCLI", elementoDaInserire.codiceClienteFornitore);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "CODFOR", elementoDaInserire.codiceClienteFornitore);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "CODCONTATORE", elementoDaInserire.codiceContatore);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "NUMDOC", elementoDaInserire.numerodocumento);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "PROTOCOLLOIVA", elementoDaInserire.protocollo);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "NUMREG", elementoDaInserire.BillingDocument);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "DATADOC", elementoDaInserire.dataDocumento);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "REGIVA", elementoDaInserire.registroIva);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "ANNO", elementoDaInserire.dataDocumento.Year);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "TOTALE", elementoDaInserire.importo);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "CDINDI", anagraficaCliente.Nazione);

                        using (Arx_File fileDaImportare = GetPDF(elementoDaInserire.BillingDocument))
                        {
                            if (fileDaImportare != null)
                            {
                                documentoDaInserire.File = fileDaImportare;
                            }

                            using (var risultatoInserimento = _manager.ARX_DATI.Dm_Profile_Insert(documentoDaInserire))
                            {
                                if (risultatoInserimento.EXCEPTION != Security_Exception.Nothing)
                                {
                                    _documentiImportati.Add(new DocumentiImportati { Tipo = EnumTipoDocumento.InvoiceOut, Chiave = formattaCodiceInvoiceOut(elementoDaInserire) });
                                    WriteError("L'inserimento del documento id {0} nella classe INSInvoiceOut non è andato a buon fine: {1}. Non verrà riprocessato fino al riavvio", elementoDaInserire.ID, risultatoInserimento.MESSAGE);
                                    WriteErrorInDb(elementoDaInserire.BillingDocument, elementoDaInserire.numerodocumento, risultatoInserimento.EXCEPTION.ToString(), elementoDaInserire.codiceClienteFornitore);
                                }
                                else
                                {
                                    _manager.ARX_DATI.SD_ASSOCDOC_Insert(risultatoInserimento.PROFILE.DOCNUMBER, formattaCodiceInvoiceOut(elementoDaInserire), string.Empty);
                                    _documentiImportati.Add(new DocumentiImportati { Tipo = EnumTipoDocumento.InvoiceOut, Chiave = formattaCodiceInvoiceOut(elementoDaInserire) });
                                    WriteWarning("Il documento {0} nella classe INSInvoiceOut è stato inserito regolarmente docnumber {1}", elementoDaInserire.ID, risultatoInserimento.PROFILE.DOCNUMBER);
                                }
                            }
                        }
                    }

                    if (_manager.UserGrantor != null)
                    {
                        _manager.DeImpersonate();
                    }
                }
                catch (Exception errore)
                {
                    _documentiImportati.Add(new DocumentiImportati { Tipo = EnumTipoDocumento.InvoiceOut, Chiave = formattaCodiceInvoiceOut(elementoDaInserire) });
                    WriteError("Errore in nella procedura inserimento:{0}", errore.Message);
                    WriteErrorInDb(elementoDaInserire.BillingDocument, elementoDaInserire.numerodocumento, errore.Message, elementoDaInserire.codiceClienteFornitore);
                }
            }

        }

        private void INSInvoiceOutFRIZZY(List<RecordGestionale> elementoDaInserireList)
        {
            if (string.IsNullOrEmpty(_config.ClasseDocInvoiceOut))
            {
                throw new Exception("La classe documentale Ordini di Vendita è vuota.");
            }
            int i = 0;
            foreach (var elementoDaInserire in elementoDaInserireList)
            {
                i++;
                try
                {

                    if (checkInvoiceOut(elementoDaInserire))
                    {
                        continue;
                    }

                    WriteInfo("Sto processando l'ID Gestionale {0}", elementoDaInserire.ID);

                    using (var documentoDaInserire = _manager.ARX_DATI.Dm_Profile_ForInsert_Get_New_Instance_ByDocumentTypeCodice(_config.ClasseDocInvoiceOut))
                    {
                        //documentoDaInserire.Aoo = _config.Aoo;
                        documentoDaInserire.Aoo = "FRIZZY";
                        documentoDaInserire.Stato = _config.StatoImport;
                        documentoDaInserire.InOut = DmProfileInOut.Uscita;

                        ElementoAnagraficaCliFor anagraficaCliente = parsaAnagrafica(elementoDaInserire, EnumTipoAnagrafica.Clienti);

                        if (anagraficaCliente == null)
                        {
                            WriteError("Errore: la ricerca del codice {0} non ha prodotto risultati. Non lo processo fino al prossimo avvio", elementoDaInserire.clienteFatturazione);
                            _documentiImportati.Add(new DocumentiImportati { Tipo = EnumTipoDocumento.InvoiceOut, Chiave = formattaCodiceInvoiceOut(elementoDaInserire) });
                            throw new Exception(string.Format("Errore durante la ricerca del codice {0}", elementoDaInserire.clienteFatturazione));
                        }
                        //documentoDaInserire.Aoo = _config.Aoo;
                        //documentoDaInserire.Stato = _config.StatoImport;
                        //documentoDaInserire.InOut = DmProfileInOut.Uscita;

                        int idrubricaCliente = CercaRubricaperCodice(anagraficaCliente, _rubricaClienti.RUBRICA, _rubricaClienti.ID, 2);

                        if (idrubricaCliente == 0)
                        {
                            throw new Exception(string.Format("Errore: non è stato possibile inserire il fornitore in rubrica per il record {0}", elementoDaInserire.codiceClienteFornitore));
                        }

                        Dm_DatiProfilo Destinatario = _manager.ARX_DATI.Dm_DatiProfilo_GetNewInstance_From_IdRubrica(idrubricaCliente, Dm_DatiProfilo_Campo.DE);
                        documentoDaInserire.To.Add(Destinatario);

                        int idRubricaCodiceDestinatario = CercaRubricaperCodice(anagraficaCliente, _rubricaClienti.RUBRICA, _rubricaClienti.ID, 2);
                        Dm_DatiProfilo DestinatarioMerce = _manager.ARX_DATI.Dm_DatiProfilo_GetNewInstance_From_IdRubrica(idRubricaCodiceDestinatario, Dm_DatiProfilo_Campo.DE);

                        documentoDaInserire.ProtocolloInterno = elementoDaInserire.numerodocumento.ToString();
                        documentoDaInserire.DataDoc = elementoDaInserire.dataDocumento;

                        documentoDaInserire.DocName = elementoDaInserire.oggetto;

                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "PIVA", anagraficaCliente.Piva);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "CODFIS", anagraficaCliente.CodiceFiscale);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "EMAIL", anagraficaCliente.Email);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "PEC", anagraficaCliente.EmailPec);


                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "CODCLI", elementoDaInserire.codiceClienteFornitore);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "CODFOR", elementoDaInserire.codiceClienteFornitore);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "CODCONTATORE", elementoDaInserire.codiceContatore);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "NUMDOC", elementoDaInserire.numerodocumento);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "PROTOCOLLOIVA", elementoDaInserire.protocollo);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "NUMREG", elementoDaInserire.BillingDocument);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "DATADOC", elementoDaInserire.dataDocumento);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "REGIVA", elementoDaInserire.registroIva);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "ANNO", elementoDaInserire.dataDocumento.Year);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "TOTALE", elementoDaInserire.importo);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "CDINDI", anagraficaCliente.Nazione);

                        using (Arx_File fileDaImportare = GetPDF(elementoDaInserire.BillingDocument))
                        {
                            if (fileDaImportare != null)
                            {
                                documentoDaInserire.File = fileDaImportare;
                            }

                            using (var risultatoInserimento = _manager.ARX_DATI.Dm_Profile_Insert(documentoDaInserire))
                            {
                                if (risultatoInserimento.EXCEPTION != Security_Exception.Nothing)
                                {
                                    _documentiImportati.Add(new DocumentiImportati { Tipo = EnumTipoDocumento.InvoiceOut, Chiave = formattaCodiceInvoiceOut(elementoDaInserire) });
                                    WriteError("L'inserimento del documento id {0} nella classe INSInvoiceOut non è andato a buon fine: {1}. Non verrà riprocessato fino al riavvio", elementoDaInserire.ID, risultatoInserimento.MESSAGE);
                                    WriteErrorInDb(elementoDaInserire.BillingDocument, elementoDaInserire.numerodocumento, risultatoInserimento.EXCEPTION.ToString(), elementoDaInserire.codiceClienteFornitore);
                                }
                                else
                                {
                                    _manager.ARX_DATI.SD_ASSOCDOC_Insert(risultatoInserimento.PROFILE.DOCNUMBER, formattaCodiceInvoiceOut(elementoDaInserire), string.Empty);
                                    _documentiImportati.Add(new DocumentiImportati { Tipo = EnumTipoDocumento.InvoiceOut, Chiave = formattaCodiceInvoiceOut(elementoDaInserire) });
                                    WriteWarning("Il documento {0} nella classe INSInvoiceOut è stato inserito regolarmente docnumber {1}", elementoDaInserire.ID, risultatoInserimento.PROFILE.DOCNUMBER);
                                }
                            }
                        }
                    }

                    if (_manager.UserGrantor != null)
                    {
                        _manager.DeImpersonate();
                    }
                }
                catch (Exception errore)
                {
                    _documentiImportati.Add(new DocumentiImportati { Tipo = EnumTipoDocumento.InvoiceOut, Chiave = formattaCodiceInvoiceOut(elementoDaInserire) });
                    WriteError("Errore in nella procedura inserimento:{0}", errore.Message);
                    WriteErrorInDb(elementoDaInserire.BillingDocument, elementoDaInserire.numerodocumento, errore.Message, elementoDaInserire.codiceClienteFornitore);
                }
            }

        }

        //private void INSInvoiceIN(List<RecordGestionale> elementoDaInserireList)
        //{
        //    if (string.IsNullOrEmpty(_config.ClasseDocInvoiceIn))
        //    {
        //        throw new Exception("La classe documentale Fatture Passive è vuota.");
        //    }
        //    int i = 0;
        //    foreach (var elementoDaInserire in elementoDaInserireList)
        //    {
        //        i++;
        //        try
        //        {

        //            if (checkInvoiceIN(elementoDaInserire))
        //            {
        //                continue;
        //            }

        //            WriteInfo("Sto processando l'ID Gestionale {0}", elementoDaInserire.ID);

        //            using (var documentoDaInserire = _manager.ARX_DATI.Dm_Profile_ForInsert_Get_New_Instance_ByDocumentTypeCodice(_config.ClasseDocInvoiceIn))
        //            {
        //                documentoDaInserire.Aoo = _config.Aoo;
        //                documentoDaInserire.Stato = _config.StatoImport;
        //                documentoDaInserire.InOut = DmProfileInOut.Uscita;

        //                ElementoAnagraficaCliFor anagraficaCliente = parsaAnagrafica(elementoDaInserire, EnumTipoAnagrafica.Clienti);

        //                if (anagraficaCliente == null)
        //                {
        //                    WriteError("Errore: la ricerca del codice {0} non ha prodotto risultati. Non lo processo fino al prossimo avvio", elementoDaInserire.clienteFatturazione);
        //                    _documentiImportati.Add(new DocumentiImportati { Tipo = EnumTipoDocumento.InvoiceIn, Chiave = formattaCodiceInvoiceIN(elementoDaInserire) });
        //                    throw new Exception(string.Format("Errore durante la ricerca del codice {0}", elementoDaInserire.clienteFatturazione));
        //                }
        //                documentoDaInserire.Aoo = _config.Aoo;
        //                documentoDaInserire.Stato = _config.StatoImport;
        //                documentoDaInserire.InOut = DmProfileInOut.Uscita;

        //                int idrubricaCliente = CercaRubricaperCodice(anagraficaCliente, _rubricaClienti.RUBRICA, _rubricaClienti.ID, 2);

        //                if (idrubricaCliente == 0)
        //                {
        //                    throw new Exception(string.Format("Errore: non è stato possibile inserire il fornitore in rubrica per il record {0}", elementoDaInserire.codiceClienteFornitore));
        //                }

        //                Dm_DatiProfilo Destinatario = _manager.ARX_DATI.Dm_DatiProfilo_GetNewInstance_From_IdRubrica(idrubricaCliente, Dm_DatiProfilo_Campo.DE);
        //                documentoDaInserire.To.Add(Destinatario);

        //                int idRubricaCodiceDestinatario = CercaRubricaperCodice(anagraficaCliente, _rubricaClienti.RUBRICA, _rubricaClienti.ID, 2);
        //                Dm_DatiProfilo DestinatarioMerce = _manager.ARX_DATI.Dm_DatiProfilo_GetNewInstance_From_IdRubrica(idRubricaCodiceDestinatario, Dm_DatiProfilo_Campo.DE);

        //                documentoDaInserire.ProtocolloInterno = elementoDaInserire.numerodocumento.ToString();
        //                documentoDaInserire.DataDoc = elementoDaInserire.dataDocumento;

        //                documentoDaInserire.DocName = elementoDaInserire.oggetto;

        //                _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "PIVA", anagraficaCliente.Piva);
        //                _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "CODFIS", anagraficaCliente.CodiceFiscale);
        //                _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "EMAIL", anagraficaCliente.Email);
        //                _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "PEC", anagraficaCliente.EmailPec);


        //                _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "CODCLI", elementoDaInserire.codiceClienteFornitore);
        //                _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "CODFOR", elementoDaInserire.codiceClienteFornitore);
        //                _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "CODCONTATORE", elementoDaInserire.codiceContatore);
        //                _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "NUMDOC", elementoDaInserire.numerodocumento);
        //                _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "PROTOCOLLOIVA", elementoDaInserire.protocollo);
        //                _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "NUMREG", elementoDaInserire.BillingDocument);
        //                _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "DATADOC", elementoDaInserire.dataDocumento);
        //                _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "REGIVA", elementoDaInserire.registroIva);
        //                _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "ANNO", elementoDaInserire.dataDocumento.Year);
        //                _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "TOTALE", elementoDaInserire.importo);
        //                _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "CDINDI", anagraficaCliente.Nazione);

        //                //using (Arx_File fileDaImportare = GetPDF(elementoDaInserire.BillingDocument))
        //                //{
        //                //    if (fileDaImportare != null)
        //                //    {
        //                //        documentoDaInserire.File = fileDaImportare;
        //                //    }

        //                //    using (var risultatoInserimento = _manager.ARX_DATI.Dm_Profile_Insert(documentoDaInserire))
        //                //    {
        //                //        if (risultatoInserimento.EXCEPTION != Security_Exception.Nothing)
        //                //        {
        //                //            _documentiImportati.Add(new DocumentiImportati { Tipo = EnumTipoDocumento.InvoiceOut, Chiave = formattaCodiceInvoiceOut(elementoDaInserire) });
        //                //            WriteError("L'inserimento del documento id {0} nella classe INSInvoiceOut non è andato a buon fine: {1}. Non verrà riprocessato fino al riavvio", elementoDaInserire.ID, risultatoInserimento.MESSAGE);
        //                //            WriteErrorInDb(elementoDaInserire.BillingDocument, elementoDaInserire.numerodocumento, risultatoInserimento.EXCEPTION.ToString(), elementoDaInserire.codiceClienteFornitore);
        //                //        }
        //                //        else
        //                //        {
        //                //            _manager.ARX_DATI.SD_ASSOCDOC_Insert(risultatoInserimento.PROFILE.DOCNUMBER, formattaCodiceInvoiceOut(elementoDaInserire), string.Empty);
        //                //            _documentiImportati.Add(new DocumentiImportati { Tipo = EnumTipoDocumento.InvoiceOut, Chiave = formattaCodiceInvoiceOut(elementoDaInserire) });
        //                //            WriteWarning("Il documento {0} nella classe INSInvoiceOut è stato inserito regolarmente docnumber {1}", elementoDaInserire.ID, risultatoInserimento.PROFILE.DOCNUMBER);
        //                //        }
        //                //    }
        //                //}
        //            }

        //            if (_manager.userImpersonated != null)
        //            {
        //                _manager.DeImpersonate();
        //            }
        //        }
        //        catch (Exception errore)
        //        {
        //            _documentiImportati.Add(new DocumentiImportati { Tipo = EnumTipoDocumento.InvoiceIn, Chiave = formattaCodiceInvoiceIN(elementoDaInserire) });
        //            WriteError("Errore in nella procedura inserimento:{0}", errore.Message);
        //            WriteErrorInDb(elementoDaInserire.BillingDocument, elementoDaInserire.numerodocumento, errore.Message, elementoDaInserire.codiceClienteFornitore);
        //        }
        //    }

        //}

        private Arx_File GetPDF(string numerodocumento)
        {
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            Arx_File ritorno = null;

            try
            {
                var TARGETURL = _config.TargetUrl + "'" + numerodocumento + "'";

                using (var client = new WebClient())
                {

                    //Login con Basic Authentication
                    var byteArray = Encoding.ASCII.GetBytes(_config.UsrInt);
                    client.Headers.Add(HttpRequestHeader.Authorization, string.Format("Basic {0}", System.Convert.ToBase64String(byteArray)));

                    try
                    {
                        // ... Read the string.
                        var result = client.DownloadString(TARGETURL);

                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(result);
                        string pdfBase64 = doc.InnerText;

                        ritorno = new Arx_File(System.Convert.FromBase64String(@pdfBase64), numerodocumento + ".pdf", DateTime.Now);
                    }
                    catch (WebException wex)
                    {
                        if (((HttpWebResponse)wex.Response).StatusCode == HttpStatusCode.NotFound)
                        {
                            // error 404, do what you need to do
                            Console.WriteLine("Problemi Stampa Fattura (404)");
                        }
                        else
                        {
                            Console.WriteLine("Problemi Stampa Fattura: " + wex.Message);
                        }
                    }
                }
            }
            catch (Exception errore)
            {
                WriteError("Errore in GeneraPDF:{0}", errore.Message);
            }

            return ritorno;

        }


        private Arx_File GetPDFPREPROD(string numerodocumento)
        {
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            Arx_File ritorno = null;

            try
            {
                //var TARGETURL = _config.TargetUrl + "'" + numerodocumento + "'";
                var TARGETURL = "https://my301187-api.s4hana.ondemand.com/sap/opu/odata/sap/API_BILLING_DOCUMENT_SRV/GetPDF?BillingDocument=" + "'" + numerodocumento + "'";

                using (var client = new WebClient())
                {

                    //Login con Basic Authentication
                    var byteArray = Encoding.ASCII.GetBytes("USRINT:CulliganCulligan2020_IT!");
                    client.Headers.Add(HttpRequestHeader.Authorization, string.Format("Basic {0}", System.Convert.ToBase64String(byteArray)));

                    try
                    {
                        // ... Read the string.
                        var result = client.DownloadString(TARGETURL);

                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(result);
                        string pdfBase64 = doc.InnerText;

                        ritorno = new Arx_File(System.Convert.FromBase64String(@pdfBase64), numerodocumento + ".pdf", DateTime.Now);
                    }
                    catch (WebException wex)
                    {
                        if (((HttpWebResponse)wex.Response).StatusCode == HttpStatusCode.NotFound)
                        {
                            // error 404, do what you need to do
                            Console.WriteLine("Problemi Stampa Fattura (404)");
                        }
                        else
                        {
                            Console.WriteLine("Problemi Stampa Fattura: " + wex.Message);
                        }
                    }
                }
            }
            catch (Exception errore)
            {
                WriteError("Errore in GeneraPDF:{0}", errore.Message);
            }

            return ritorno;

        }

        private void INSDDTIn(List<RecordGestionale> elementoDaInserireList)
        {
            if (string.IsNullOrEmpty(_config.ClasseDocDDTIn))
            {
                throw new Exception("La classe documentale DDt Cliente è vuota.");
            }

            foreach (var elementoDaInserire in elementoDaInserireList)
            {
                try
                {

                    if (checkDDtIn(elementoDaInserire))
                    {
                        continue;
                    }

                    WriteInfo("Sto processando l'ID Gestionale {0}", elementoDaInserire.ID);

                    using (var documentoDaInserire = _manager.ARX_DATI.Dm_Profile_ForInsert_Get_New_Instance_ByDocumentTypeCodice(_config.ClasseDocDDTIn))
                    {
                        documentoDaInserire.Aoo = _config.Aoo;
                        documentoDaInserire.Stato = _config.StatoImport;
                        documentoDaInserire.InOut = DmProfileInOut.Uscita;

                        ElementoAnagraficaCliFor anagraficaFornitore = parsaAnagrafica(elementoDaInserire, EnumTipoAnagrafica.Fornitori);

                        if (anagraficaFornitore == null)
                        {
                            WriteError("Errore: la ricerca del codice {0} non ha prodotto risultati. Non lo processo fino al prossimo avvio", elementoDaInserire.clienteFatturazione);
                            _documentiImportati.Add(new DocumentiImportati { Tipo = EnumTipoDocumento.DDTIN, Chiave = formattaCodiceDDTIn(elementoDaInserire) });
                            throw new Exception(string.Format("Errore durante la ricerca del codice {0}", elementoDaInserire.clienteFatturazione));
                        }

                        documentoDaInserire.Aoo = _config.Aoo;
                        documentoDaInserire.Stato = _config.StatoImport;
                        documentoDaInserire.InOut = DmProfileInOut.Uscita;

                        int idrubricaCliente = CercaRubricaperCodice(anagraficaFornitore, _rubricaFornitori.RUBRICA, _rubricaFornitori.ID, 2);

                        if (idrubricaCliente == 0)
                        {
                            throw new Exception(string.Format("Errore: non è stato possibile inserire il fornitore in rubrica per il record {0}", elementoDaInserire.codiceClienteFornitore));
                        }

                        Dm_DatiProfilo Mittente = _manager.ARX_DATI.Dm_DatiProfilo_GetNewInstance_From_IdRubrica(idrubricaCliente, Dm_DatiProfilo_Campo.MI);
                        documentoDaInserire.From = Mittente;

                        documentoDaInserire.ProtocolloInterno = elementoDaInserire.numerodocumento.ToString();
                        documentoDaInserire.DataDoc = elementoDaInserire.dataDocumento;

                        documentoDaInserire.DocName = elementoDaInserire.oggetto;

                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "CODCON", elementoDaInserire.codiceContatore);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "NUMDOCV", elementoDaInserire.vostroDocumento);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "DATADOCV", elementoDaInserire.vostroDocumentodata);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "ANNOV", elementoDaInserire.vostroDocumentodata.Year);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "NUMRIC", elementoDaInserire.numerodocumento);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "DATAREG", elementoDaInserire.dataDocumento);

                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "PIVA", Mittente.PARTIVA);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "CODFIS", Mittente.CODFIS);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "ANNO", elementoDaInserire.dataDocumento.Year);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "RIFCLIDOC", elementoDaInserire.vostroDocumento);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "RIFCLIDOCDATA", elementoDaInserire.vostroDocumentodata);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "CODCLIFOR", elementoDaInserire.codiceClienteFornitore);

                        string barcode = string.Empty;

                        if (!string.IsNullOrEmpty(elementoDaInserire.barcode))
                        {
                            barcode = elementoDaInserire.barcode;
                        }

                        using (var risultatoInserimento = _manager.ARX_DATI.Dm_Profile_Insert_For_Barcode(documentoDaInserire, barcode))
                        {
                            if (risultatoInserimento.EXCEPTION != Security_Exception.Nothing)
                            {
                                _documentiImportati.Add(new DocumentiImportati { Tipo = EnumTipoDocumento.DDTIN, Chiave = formattaCodiceDDTIn(elementoDaInserire) });
                                WriteError("L'inserimento del documento id {0} nella classe DDTIN non è andato a buon fine: {1}. Non verrà riprocessato fino al riavvio", elementoDaInserire.ID, risultatoInserimento.MESSAGE);
                            }
                            else
                            {
                                _manager.ARX_DATI.SD_ASSOCDOC_Insert(risultatoInserimento.PROFILE.DOCNUMBER, formattaCodiceDDTIn(elementoDaInserire), string.Empty);
                                _documentiImportati.Add(new DocumentiImportati { Tipo = EnumTipoDocumento.DDTIN, Chiave = formattaCodiceDDTIn(elementoDaInserire) });
                                WriteWarning("Il documento {0} nella classe DDTIN è stato inserito regolarmente docnumber {1}", elementoDaInserire.ID, risultatoInserimento.PROFILE.DOCNUMBER);

                                if (!string.IsNullOrEmpty(elementoDaInserire.barcode))
                                {
                                    _manager.ARX_DATI.Dm_Barcode_Insert(risultatoInserimento.PROFILE.DOCNUMBER, Dm_Barcode_TipoImpronta.N, elementoDaInserire.barcode);
                                }
                            }
                        }
                    }

                    if (_manager.UserGrantor != null)
                    {
                        _manager.DeImpersonate();
                    }
                }
                catch (Exception errore)
                {
                    _documentiImportati.Add(new DocumentiImportati { Tipo = EnumTipoDocumento.DDTIN, Chiave = formattaCodiceDDTIn(elementoDaInserire) });
                    WriteError("Errore in nella procedura inserimento:{0}", errore.Message);
                }
            }

        }

        private void INSInvoiceIn(List<RecordGestionale> elementoDaInserireList)
        {
            if (string.IsNullOrEmpty(_config.ClasseDocInvoiceIn))
            {
                throw new Exception("La classe documentale Invoice IN è vuota.");
            }

            foreach (var elementoDaInserire in elementoDaInserireList)
            {
                try
                {

                    if (checkInvoiceIn(elementoDaInserire))
                    {
                        continue;
                    }

                    WriteInfo("Sto processando l'ID Gestionale {0}", elementoDaInserire.ID);

                    using (var documentoDaInserire = _manager.ARX_DATI.Dm_Profile_ForInsert_Get_New_Instance_ByDocumentTypeCodice(_config.ClasseDocInvoiceIn))
                    {
                        if (!string.IsNullOrEmpty(elementoDaInserire.Utente))
                        {
                            var selezione = trascodificaUtentiArxivar.FirstOrDefault(x => x.Descrizione.Equals(elementoDaInserire.Utente, StringComparison.CurrentCultureIgnoreCase));

                            if (selezione != null)
                            {
                                _manager.Impersonate_By_UserId(selezione.Utente);
                            }
                        }

                        documentoDaInserire.Aoo = _config.Aoo;
                        documentoDaInserire.Stato = _config.StatoImport;
                        documentoDaInserire.InOut = DmProfileInOut.Uscita;

                        ElementoAnagraficaCliFor anagraficaFornitore = parsaAnagrafica(elementoDaInserire, EnumTipoAnagrafica.Fornitori);

                        if (anagraficaFornitore == null)
                        {
                            WriteError("Errore: la ricerca del codice {0} non ha prodotto risultati. Non lo processo fino al prossimo avvio", elementoDaInserire.clienteFatturazione);
                            _documentiImportati.Add(new DocumentiImportati { Tipo = EnumTipoDocumento.InvoiceIn, Chiave = formattaCodiceDDTIn(elementoDaInserire) });
                            throw new Exception(string.Format("Errore durante la ricerca del codice {0}", elementoDaInserire.clienteFatturazione));
                        }

                        documentoDaInserire.Aoo = _config.Aoo;
                        documentoDaInserire.Stato = _config.StatoImport;
                        documentoDaInserire.InOut = DmProfileInOut.Uscita;

                        int idrubricaCliente = CercaRubricaperCodice(anagraficaFornitore, _rubricaFornitori.RUBRICA, _rubricaFornitori.ID, 2);

                        if (idrubricaCliente == 0)
                        {
                            throw new Exception(string.Format("Errore: non è stato possibile inserire il fornitore in rubrica per il record {0}", elementoDaInserire.codiceClienteFornitore));
                        }

                        Dm_DatiProfilo Mittente = _manager.ARX_DATI.Dm_DatiProfilo_GetNewInstance_From_IdRubrica(idrubricaCliente, Dm_DatiProfilo_Campo.MI);
                        documentoDaInserire.From = Mittente;

                        documentoDaInserire.ProtocolloInterno = elementoDaInserire.numerodocumento.ToString();
                        documentoDaInserire.DataDoc = elementoDaInserire.dataDocumento;

                        documentoDaInserire.DocName = elementoDaInserire.oggetto;

                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "CODFOR", elementoDaInserire.codiceClienteFornitore);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "CODCONTATORE", elementoDaInserire.codiceContatore);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "NUMDOC", elementoDaInserire.numerodocumento);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "DATADOC", elementoDaInserire.dataDocumento);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "PIVA", Mittente.PARTIVA);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "CODFIS", Mittente.CODFIS);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "ANNO", elementoDaInserire.esercizio);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "REGIVA", elementoDaInserire.registroIva);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "PROTOCOLLOIVA", elementoDaInserire.protocollo);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "DATAREG", elementoDaInserire.dataprotocollo);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "NUMREG", elementoDaInserire.contator);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "DESTMERCE", elementoDaInserire.destinazioneMerce);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "AGENTE", elementoDaInserire.agente);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "TIPO", "SAP");
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "ESERCIZIO", elementoDaInserire.dataprotocollo.Year);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "contator", elementoDaInserire.contator);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "ANNOREG", elementoDaInserire.dataprotocollo.Year);
                        _common.ValorizzaAggiuntivo(documentoDaInserire.Aggiuntivi, "CONTOCONT", elementoDaInserire.codiceClienteFornitore);

                        string barcode = string.Empty;

                        if (!string.IsNullOrEmpty(elementoDaInserire.barcode))
                        {
                            barcode = elementoDaInserire.barcode;
                        }

                        using (var risultatoInserimento = _manager.ARX_DATI.Dm_Profile_Insert_For_Barcode(documentoDaInserire, barcode))
                        {
                            if (risultatoInserimento.EXCEPTION != Security_Exception.Nothing)
                            {
                                _documentiImportati.Add(new DocumentiImportati { Tipo = EnumTipoDocumento.InvoiceIn, Chiave = formattaCodiceInvoiceIn(elementoDaInserire) });
                                WriteError("L'inserimento del documento id {0} nella classe InvoiceIn non è andato a buon fine: {1}. Non verrà riprocessato fino al riavvio", elementoDaInserire.ID, risultatoInserimento.MESSAGE);
                            }
                            else
                            {
                                _manager.ARX_DATI.SD_ASSOCDOC_Insert(risultatoInserimento.PROFILE.DOCNUMBER, formattaCodiceInvoiceIn(elementoDaInserire), string.Empty);
                                _documentiImportati.Add(new DocumentiImportati { Tipo = EnumTipoDocumento.InvoiceIn, Chiave = formattaCodiceInvoiceIn(elementoDaInserire) });
                                WriteWarning("Il documento {0} nella classe InvoiceIn è stato inserito regolarmente docnumber {1}", elementoDaInserire.ID, risultatoInserimento.PROFILE.DOCNUMBER);

                                if (string.IsNullOrEmpty(elementoDaInserire.barcode))
                                {
                                    if (_config.StampaBarcode && elementoDaInserire.dataDocumento.Year < 2019)
                                    {
                                        _manager.ARX_DATI.Dm_Barcode_Print(risultatoInserimento.PROFILE.DOCNUMBER, Dm_Barcode_TipoImpronta.N, true);
                                    }
                                }
                                else
                                {
                                    _manager.ARX_DATI.Dm_Barcode_Insert(risultatoInserimento.PROFILE.DOCNUMBER, Dm_Barcode_TipoImpronta.N, elementoDaInserire.barcode);
                                }
                            }
                        }
                    }
                }
                catch (Exception errore)
                {
                    _documentiImportati.Add(new DocumentiImportati { Tipo = EnumTipoDocumento.InvoiceIn, Chiave = formattaCodiceInvoiceIn(elementoDaInserire) });
                    WriteError("Errore in nella procedura inserimento del documento {1}-{2}:{0}", errore.Message, elementoDaInserire.numerodocumento, elementoDaInserire.AccountingDocumentType);
                }
                finally
                {
                    if (_manager.UserGrantor != null)
                    {
                        _manager.DeImpersonate();
                    }
                }
            }

        }

        public void WriteErrorInDb(string BillingDocument, string AccountingDocument, string Errore, string cliente)
        {
            using (SAP_BI_oldEntities context = new SAP_BI_oldEntities())
            {
                ARXIVAR_Fatture_Errore fe = new ARXIVAR_Fatture_Errore();

                fe.AccountingDocument = AccountingDocument;
                fe.BillingDocument = BillingDocument;
                fe.Errore = Errore;
                fe.Cliente = cliente;

                context.AddToARXIVAR_Fatture_Errore(fe);
                context.SaveChanges();



            }
        }

        private ElementoAnagraficaCliFor parsaAnagrafica(RecordGestionale elementoDaInserire, EnumTipoAnagrafica tipoAnagrafica)
        {
            ElementoAnagraficaCliFor ritorno = null;
            string codiceClienteFornitore = string.Empty;

            if (elementoDaInserire.TipoDocumento == EnumTipoDocumento.DDTOUT)
            {
                if (string.IsNullOrEmpty(elementoDaInserire.clienteFatturazione))
                    codiceClienteFornitore = elementoDaInserire.clienteSpedizione;
                else
                    codiceClienteFornitore = elementoDaInserire.clienteFatturazione;
            }
            else
            {
                codiceClienteFornitore = elementoDaInserire.codiceClienteFornitore;
            }

            if (string.IsNullOrEmpty(codiceClienteFornitore))
            {
                IntegratorHelper.WriteResult(elementoDaInserire.numerodocumento, "Il codice cliente/fornitore è vuoto e non procedo con la ricerca");
                throw new Exception("Il codice cliente/fornitore è vuoto e non procedo con la ricerca");
            }

            if (tipoAnagrafica == EnumTipoAnagrafica.Clienti)
            {
                //using (SAPEntities context = new SAPEntities())
                using (BI_STGEntities context = new BI_STGEntities())
                {
                    var result = (from t in context.SFDC_Anagrafica_Clienti
                                  where t.AccountNumber__c.Equals(codiceClienteFornitore)
                                  select t).FirstOrDefault();

                    //var result = (from t in context.ANAGRAFICA_CLIENTI_temp
                    //              where t.AccountNumber__c.Equals(codiceClienteFornitore)
                    //              select t).FirstOrDefault();


                    if (result != null)
                    {
                        ritorno = new ElementoAnagraficaCliFor();

                        ritorno.Nazione = result.BillingCountryCode;
                        ritorno.Codice = result.AccountNumber__c;
                        ritorno.RagioneSociale = result.Name;
                        ritorno.CodiceFiscale = !string.IsNullOrEmpty(result.FiscalCode__c) ? result.FiscalCode__c : result.CompanyFiscalCode__c;
                        ritorno.Piva = !string.IsNullOrEmpty(result.VatNumber__c) ? result.VatNumber__c : result.VATNumberExtraEU__c;
                        if (!string.IsNullOrEmpty(result.Email_pec__c) && !result.Email_pec__c.Equals("noreplay@culligan.it", StringComparison.CurrentCultureIgnoreCase) && !result.Email_pec__c.Equals("nomail@culligan.it", StringComparison.CurrentCultureIgnoreCase))
                            ritorno.EmailPec = result.Email_pec__c;
                        if (!string.IsNullOrEmpty(result.Email__c) && !result.Email__c.Equals("noreplay@culligan.it", StringComparison.CurrentCultureIgnoreCase) && !result.Email__c.Equals("nomail@culligan.it", StringComparison.CurrentCultureIgnoreCase))
                            ritorno.Email = result.Email__c;
                    }


                }

                //var ricercaWS = serviceBusinessPartner.CallBusinessPartnerGet("2018-07-05T00:00:00.000", "single", codiceClienteFornitore);

                //if (ricercaWS != null)
                //{
                //    if (ricercaWS.Count() > 0)
                //    {
                //        ritorno = new ElementoAnagraficaCliFor();

                //        ritorno.Codice = ricercaWS[0].BusinessPartner[0];
                //        ritorno.RagioneSociale = ricercaWS[0].BusinessPartnerFullName[0];

                //        foreach (var tax in ricercaWS[0].to_BusinessPartnerTax)
                //        {
                //            try
                //            {
                //                if (tax.BPTaxType[0].Equals("IT1"))
                //                    ritorno.CodiceFiscale = tax.BPTaxNumber[0]; //CLIENTI.to_Customer[0].A_CustomerType.TaxNumber1[0];
                //            }
                //            catch (Exception)
                //            {
                //                ritorno.CodiceFiscale = string.Empty;
                //            }

                //            try
                //            {
                //                if (tax.BPTaxType[0].Equals("IT0"))
                //                    ritorno.Piva = tax.BPTaxNumber[0]; //CLIENTI.to_Customer[0].A_CustomerType.TaxNumber1[0];


                //                if (tax.BPTaxType[0].Equals("IT2"))
                //                    ritorno.Piva = tax.BPTaxNumber[0]; //CLIENTI.to_Customer[0].A_CustomerType.TaxNumber1[0];
                //            }
                //            catch (Exception)
                //            {
                //                ritorno.Piva = string.Empty;
                //            }


                //        }

                //        try
                //        {
                //            if (!string.IsNullOrEmpty(ricercaWS[0].to_BusinessPartnerAddress[0].to_EmailAddress[0].EmailAddress[0]) &&
                //           ! (ricercaWS[0].to_BusinessPartnerAddress[0].to_EmailAddress[0].EmailAddress[0].Equals("nomail@culligan.it", StringComparison.CurrentCultureIgnoreCase) ||
                //           ricercaWS[0].to_BusinessPartnerAddress[0].to_EmailAddress[0].EmailAddress[0].Equals("noreplay@culligan.it", StringComparison.CurrentCultureIgnoreCase)))
                //        {
                //            ritorno.Email = ricercaWS[0].to_BusinessPartnerAddress[0].to_EmailAddress[0].EmailAddress[0];
                //        }
                //        else
                //        {
                //                ritorno.Email = string.Empty;
                //        }
                //        }
                //        catch (Exception esc) { ritorno.Email = string.Empty; }

                //        try
                //        {
                //            if (!string.IsNullOrEmpty(ricercaWS[0].to_BusinessPartnerAddress[0].to_EmailAddress[3].EmailAddress[0]) &&
                //           !(ricercaWS[0].to_BusinessPartnerAddress[0].to_EmailAddress[3].EmailAddress[0].Equals("nomail@culligan.it", StringComparison.CurrentCultureIgnoreCase) ||
                //           ricercaWS[0].to_BusinessPartnerAddress[0].to_EmailAddress[3].EmailAddress[0].Equals("noreplay@culligan.it", StringComparison.CurrentCultureIgnoreCase)))
                //            {
                //                ritorno.EmailPec = ricercaWS[0].to_BusinessPartnerAddress[0].to_EmailAddress[3].EmailAddress[0];
                //            }
                //            else
                //            {
                //                ritorno.EmailPec = string.Empty;
                //            }
                //        } catch (Exception esc) { ritorno.EmailPec = string.Empty; }

                //    }
                //} 
            }
            else if (tipoAnagrafica == EnumTipoAnagrafica.Fornitori)
            {
                var ricercaWS = serviceSupplierGetSingle.CallSupplierGetSingle("2018-07-05T00:00:00.000", codiceClienteFornitore);

                if (ricercaWS != null)
                {
                    if (ricercaWS.Count() > 0)
                    {
                        ritorno = new ElementoAnagraficaCliFor();

                        ritorno.Codice = codiceClienteFornitore;
                        ritorno.RagioneSociale = ricercaWS[0].BusinessPartnerFullName[0];

                        foreach (var tax in ricercaWS[0].to_BusinessPartnerTax)
                        {
                            try
                            {
                                if (tax.BPTaxType[0].Equals("IT1"))
                                    ritorno.CodiceFiscale = tax.BPTaxNumber[0]; //CLIENTI.to_Customer[0].A_CustomerType.TaxNumber1[0];
                            }
                            catch (Exception)
                            {
                                ritorno.CodiceFiscale = string.Empty;
                            }

                            try
                            {
                                if (tax.BPTaxType[0].Equals("IT0"))
                                    ritorno.Piva = tax.BPTaxNumber[0]; //CLIENTI.to_Customer[0].A_CustomerType.TaxNumber1[0];


                                if (tax.BPTaxType[0].Equals("IT2"))
                                    ritorno.Piva = tax.BPTaxNumber[0]; //CLIENTI.to_Customer[0].A_CustomerType.TaxNumber1[0];
                            }
                            catch (Exception)
                            {
                                ritorno.Piva = string.Empty;
                            }


                        }

                    }
                }
            }

            return ritorno;
        }

        private ElementoAnagraficaCliFor parsaAnagraficaPREPROD(RecordGestionale elementoDaInserire, EnumTipoAnagrafica tipoAnagrafica)
        {
            ElementoAnagraficaCliFor ritorno = null;
            string codiceClienteFornitore = string.Empty;

            if (elementoDaInserire.TipoDocumento == EnumTipoDocumento.DDTOUT)
            {
                if (string.IsNullOrEmpty(elementoDaInserire.clienteFatturazione))
                    codiceClienteFornitore = elementoDaInserire.clienteSpedizione;
                else
                    codiceClienteFornitore = elementoDaInserire.clienteFatturazione;
            }
            else
            {
                codiceClienteFornitore = elementoDaInserire.codiceClienteFornitore;
            }

            if (string.IsNullOrEmpty(codiceClienteFornitore))
            {
                IntegratorHelper.WriteResult(elementoDaInserire.numerodocumento, "Il codice cliente/fornitore è vuoto e non procedo con la ricerca");
                throw new Exception("Il codice cliente/fornitore è vuoto e non procedo con la ricerca");
            }

            if (tipoAnagrafica == EnumTipoAnagrafica.Clienti)
            {
                using (SAPEntities context = new SAPEntities())
                {
                    var result = (from t in context.PREPROD_SFDC_Account
                                  where t.AccountNumber__c.Equals(codiceClienteFornitore)
                                  select t).FirstOrDefault();

                    //var result = (from t in context.ANAGRAFICA_CLIENTI_temp
                    //              where t.AccountNumber__c.Equals(codiceClienteFornitore)
                    //              select t).FirstOrDefault();


                    if (result != null)
                    {
                        ritorno = new ElementoAnagraficaCliFor();

                        ritorno.Nazione = result.BillingCountryCode;
                        ritorno.Codice = result.AccountNumber__c;
                        ritorno.RagioneSociale = result.Name;
                        ritorno.CodiceFiscale = !string.IsNullOrEmpty(result.FiscalCode__c) ? result.FiscalCode__c : result.CompanyFiscalCode__c;
                        ritorno.Piva = !string.IsNullOrEmpty(result.VatNumber__c) ? result.VatNumber__c : result.VATNumberExtraEU__c;
                        if (!string.IsNullOrEmpty(result.Email_pec__c) && !result.Email_pec__c.Equals("noreplay@culligan.it", StringComparison.CurrentCultureIgnoreCase) && !result.Email_pec__c.Equals("nomail@culligan.it", StringComparison.CurrentCultureIgnoreCase))
                            ritorno.EmailPec = result.Email_pec__c;
                        if (!string.IsNullOrEmpty(result.Email__c) && !result.Email__c.Equals("noreplay@culligan.it", StringComparison.CurrentCultureIgnoreCase) && !result.Email__c.Equals("nomail@culligan.it", StringComparison.CurrentCultureIgnoreCase))
                            ritorno.Email = result.Email__c;
                    }


                }

                //var ricercaWS = serviceBusinessPartner.CallBusinessPartnerGet("2018-07-05T00:00:00.000", "single", codiceClienteFornitore);

                //if (ricercaWS != null)
                //{
                //    if (ricercaWS.Count() > 0)
                //    {
                //        ritorno = new ElementoAnagraficaCliFor();

                //        ritorno.Codice = ricercaWS[0].BusinessPartner[0];
                //        ritorno.RagioneSociale = ricercaWS[0].BusinessPartnerFullName[0];

                //        foreach (var tax in ricercaWS[0].to_BusinessPartnerTax)
                //        {
                //            try
                //            {
                //                if (tax.BPTaxType[0].Equals("IT1"))
                //                    ritorno.CodiceFiscale = tax.BPTaxNumber[0]; //CLIENTI.to_Customer[0].A_CustomerType.TaxNumber1[0];
                //            }
                //            catch (Exception)
                //            {
                //                ritorno.CodiceFiscale = string.Empty;
                //            }

                //            try
                //            {
                //                if (tax.BPTaxType[0].Equals("IT0"))
                //                    ritorno.Piva = tax.BPTaxNumber[0]; //CLIENTI.to_Customer[0].A_CustomerType.TaxNumber1[0];


                //                if (tax.BPTaxType[0].Equals("IT2"))
                //                    ritorno.Piva = tax.BPTaxNumber[0]; //CLIENTI.to_Customer[0].A_CustomerType.TaxNumber1[0];
                //            }
                //            catch (Exception)
                //            {
                //                ritorno.Piva = string.Empty;
                //            }


                //        }

                //        try
                //        {
                //            if (!string.IsNullOrEmpty(ricercaWS[0].to_BusinessPartnerAddress[0].to_EmailAddress[0].EmailAddress[0]) &&
                //           ! (ricercaWS[0].to_BusinessPartnerAddress[0].to_EmailAddress[0].EmailAddress[0].Equals("nomail@culligan.it", StringComparison.CurrentCultureIgnoreCase) ||
                //           ricercaWS[0].to_BusinessPartnerAddress[0].to_EmailAddress[0].EmailAddress[0].Equals("noreplay@culligan.it", StringComparison.CurrentCultureIgnoreCase)))
                //        {
                //            ritorno.Email = ricercaWS[0].to_BusinessPartnerAddress[0].to_EmailAddress[0].EmailAddress[0];
                //        }
                //        else
                //        {
                //                ritorno.Email = string.Empty;
                //        }
                //        }
                //        catch (Exception esc) { ritorno.Email = string.Empty; }

                //        try
                //        {
                //            if (!string.IsNullOrEmpty(ricercaWS[0].to_BusinessPartnerAddress[0].to_EmailAddress[3].EmailAddress[0]) &&
                //           !(ricercaWS[0].to_BusinessPartnerAddress[0].to_EmailAddress[3].EmailAddress[0].Equals("nomail@culligan.it", StringComparison.CurrentCultureIgnoreCase) ||
                //           ricercaWS[0].to_BusinessPartnerAddress[0].to_EmailAddress[3].EmailAddress[0].Equals("noreplay@culligan.it", StringComparison.CurrentCultureIgnoreCase)))
                //            {
                //                ritorno.EmailPec = ricercaWS[0].to_BusinessPartnerAddress[0].to_EmailAddress[3].EmailAddress[0];
                //            }
                //            else
                //            {
                //                ritorno.EmailPec = string.Empty;
                //            }
                //        } catch (Exception esc) { ritorno.EmailPec = string.Empty; }

                //    }
                //} 
            }
            else if (tipoAnagrafica == EnumTipoAnagrafica.Fornitori)
            {
                var ricercaWS = serviceSupplierGetSingle.CallSupplierGetSingle("2018-07-05T00:00:00.000", codiceClienteFornitore);

                if (ricercaWS != null)
                {
                    if (ricercaWS.Count() > 0)
                    {
                        ritorno = new ElementoAnagraficaCliFor();

                        ritorno.Codice = codiceClienteFornitore;
                        ritorno.RagioneSociale = ricercaWS[0].BusinessPartnerFullName[0];

                        foreach (var tax in ricercaWS[0].to_BusinessPartnerTax)
                        {
                            try
                            {
                                if (tax.BPTaxType[0].Equals("IT1"))
                                    ritorno.CodiceFiscale = tax.BPTaxNumber[0]; //CLIENTI.to_Customer[0].A_CustomerType.TaxNumber1[0];
                            }
                            catch (Exception)
                            {
                                ritorno.CodiceFiscale = string.Empty;
                            }

                            try
                            {
                                if (tax.BPTaxType[0].Equals("IT0"))
                                    ritorno.Piva = tax.BPTaxNumber[0]; //CLIENTI.to_Customer[0].A_CustomerType.TaxNumber1[0];


                                if (tax.BPTaxType[0].Equals("IT2"))
                                    ritorno.Piva = tax.BPTaxNumber[0]; //CLIENTI.to_Customer[0].A_CustomerType.TaxNumber1[0];
                            }
                            catch (Exception)
                            {
                                ritorno.Piva = string.Empty;
                            }


                        }

                    }
                }
            }

            return ritorno;
        }

        private bool checkDDTOut(RecordGestionale elementoDaInserire)
        {
            bool ritorno = false;
            var selezione = _documentiImportati.FirstOrDefault(x => x.Tipo == EnumTipoDocumento.DDTOUT && x.Chiave.Equals(formattaCodiceDDTOut(elementoDaInserire)));

            if (selezione != null)
            {
                return true;
            }

            var selezioneIdErp = _manager.ARX_DATI.SD_ASSOCDOC_Get_Data_By_IdErp(formattaCodiceDDTOut(elementoDaInserire), string.Empty);

            if (selezioneIdErp != null)
            {
                if (selezioneIdErp.Count() > 0)
                {
                    _documentiImportati.Add(new DocumentiImportati { Tipo = EnumTipoDocumento.DDTOUT, Chiave = formattaCodiceDDTOut(elementoDaInserire) });
                    return true;
                }
            }

            return ritorno;
        }

        private bool checkOV(RecordGestionale elementoDaInserire)
        {
            bool ritorno = false;
            var selezione = _documentiImportati.FirstOrDefault(x => x.Tipo == EnumTipoDocumento.OV && x.Chiave.Equals(formattaCodiceOV(elementoDaInserire)));

            if (selezione != null)
            {
                return true;
            }

            var selezioneIdErp = _manager.ARX_DATI.SD_ASSOCDOC_Get_Data_By_IdErp(formattaCodiceOV(elementoDaInserire), string.Empty);

            if (selezioneIdErp != null)
            {
                if (selezioneIdErp.Count() > 0)
                {
                    _documentiImportati.Add(new DocumentiImportati { Tipo = EnumTipoDocumento.OV, Chiave = formattaCodiceOV(elementoDaInserire) });
                    return true;
                }
            }

            return ritorno;
        }

        private bool checkDDtIn(RecordGestionale elementoDaInserire)
        {
            bool ritorno = false;
            var selezione = _documentiImportati.FirstOrDefault(x => x.Tipo == EnumTipoDocumento.DDTIN && x.Chiave.Equals(formattaCodiceDDTIn(elementoDaInserire)));

            if (selezione != null)
            {
                return true;
            }

            var selezioneIdErp = _manager.ARX_DATI.SD_ASSOCDOC_Get_Data_By_IdErp(formattaCodiceDDTIn(elementoDaInserire), string.Empty);

            if (selezioneIdErp != null)
            {
                if (selezioneIdErp.Count() > 0)
                {
                    _documentiImportati.Add(new DocumentiImportati { Tipo = EnumTipoDocumento.DDTIN, Chiave = formattaCodiceDDTIn(elementoDaInserire) });
                    return true;
                }
            }

            return ritorno;
        }

        private bool checkInvoiceIn(RecordGestionale elementoDaInserire)
        {
            bool ritorno = false;
            var selezione = _documentiImportati.FirstOrDefault(x => x.Tipo == EnumTipoDocumento.InvoiceIn && x.Chiave.Equals(formattaCodiceInvoiceIn(elementoDaInserire)));

            if (selezione != null)
            {
                return true;
            }

            var selezioneIdErp = _manager.ARX_DATI.SD_ASSOCDOC_Get_Data_By_IdErp(formattaCodiceInvoiceIn(elementoDaInserire), string.Empty);

            if (selezioneIdErp != null)
            {
                if (selezioneIdErp.Count() > 0)
                {
                    _documentiImportati.Add(new DocumentiImportati { Tipo = EnumTipoDocumento.InvoiceIn, Chiave = formattaCodiceInvoiceIn(elementoDaInserire) });
                    return true;
                }
            }

            return ritorno;
        }

        private bool checkInvoiceOut(RecordGestionale elementoDaInserire)
        {
            bool ritorno = false;
            var selezione = _documentiImportati.FirstOrDefault(x => x.Tipo == EnumTipoDocumento.InvoiceOut && x.Chiave.Equals(formattaCodiceInvoiceOut(elementoDaInserire)));

            if (selezione != null)
            {
                return true;
            }

            var selezioneIdErp = _manager.ARX_DATI.SD_ASSOCDOC_Get_Data_By_IdErp(formattaCodiceInvoiceOut(elementoDaInserire), string.Empty);

            if (selezioneIdErp != null)
            {
                if (selezioneIdErp.Count() > 0)
                {
                    _documentiImportati.Add(new DocumentiImportati { Tipo = EnumTipoDocumento.InvoiceOut, Chiave = formattaCodiceInvoiceOut(elementoDaInserire) });
                    return true;
                }
            }

            return ritorno;
        }

        private bool checkInvoiceIN(RecordGestionale elementoDaInserire)
        {
            bool ritorno = false;
            var selezione = _documentiImportati.FirstOrDefault(x => x.Tipo == EnumTipoDocumento.InvoiceIn && x.Chiave.Equals(formattaCodiceInvoiceOut(elementoDaInserire)));

            if (selezione != null)
            {
                return true;
            }

            var selezioneIdErp = _manager.ARX_DATI.SD_ASSOCDOC_Get_Data_By_IdErp(formattaCodiceInvoiceIN(elementoDaInserire), string.Empty);

            if (selezioneIdErp != null)
            {
                if (selezioneIdErp.Count() > 0)
                {
                    _documentiImportati.Add(new DocumentiImportati { Tipo = EnumTipoDocumento.InvoiceIn, Chiave = formattaCodiceInvoiceIN(elementoDaInserire) });
                    return true;
                }
            }

            return ritorno;
        }

        private string formattaCodiceDDTOut(RecordGestionale elementoDaInserire)
        {
            string ritorno = string.Empty;

            ritorno = string.Format("{0}-{1}-{2}", elementoDaInserire.TipoDocumento, elementoDaInserire.tipoDocumento, elementoDaInserire.numerodocumento);

            return ritorno;
        }

        private string formattaCodiceOV(RecordGestionale elementoDaInserire)
        {
            string ritorno = string.Empty;

            ritorno = string.Format("{0}-{1}-{2}", elementoDaInserire.TipoDocumento, elementoDaInserire.tipoDocumento, elementoDaInserire.numerodocumento);

            return ritorno;
        }

        private string formattaCodiceDDTIn(RecordGestionale elementoDaInserire)
        {
            string ritorno = string.Empty;

            ritorno = string.Format("{0}-{1}-{2}", elementoDaInserire.TipoDocumento, elementoDaInserire.codiceContatore, elementoDaInserire.numerodocumento);

            return ritorno;
        }

        private string formattaCodiceInvoiceIn(RecordGestionale elementoDaInserire)
        {
            string ritorno = string.Empty;

            //ritorno = string.Format("{0}-{1}-{2}-{3}", elementoDaInserire.TipoDocumento,elementoDaInserire.esercizio, elementoDaInserire.codiceContatore, elementoDaInserire.numerodocumento);
            //ritorno = string.Format("{0}-{1}-{2}", elementoDaInserire.TipoDocumento, elementoDaInserire.esercizio,  elementoDaInserire.protocollo);
            ritorno = string.Format("{0}-{1}", elementoDaInserire.esercizio, elementoDaInserire.protocollo);

            return ritorno;
        }

        private string formattaCodiceInvoiceOut(RecordGestionale elementoDaInserire)
        {
            string ritorno = string.Empty;

            //ritorno = string.Format("{0}-{1}", elementoDaInserire.TipoDocumento,  elementoDaInserire.numerodocumento);

            //ritorno = string.Format("{0}-{1}", elementoDaInserire.numerodocumento, elementoDaInserire.esercizio);
            ritorno = string.Format("{0}-{1}-{2}", elementoDaInserire.numerodocumento, elementoDaInserire.esercizio,elementoDaInserire.azienda);
            return ritorno;
        }


        private string formattaCodiceInvoiceIN(RecordGestionale elementoDaInserire)
        {
            string ritorno = string.Empty;

            //ritorno = string.Format("{0}-{1}", elementoDaInserire.TipoDocumento,  elementoDaInserire.numerodocumento);

            ritorno = string.Format("{0}-{1}", elementoDaInserire.numerodocumento, elementoDaInserire.esercizio);
            return ritorno;
        }

        private int CercaRubricaperCodice(ElementoAnagraficaCliFor anagrafica, string nomeRubrica, int idRubrica, int utente)
        {
            if (utente.Equals(0))
            {
                utente = 2;
            }
            int valoreRitorno = 0;

            WriteInfo(string.Format("Inizio la ricerca nella rubrica {0} codice {1}", anagrafica.Codice, nomeRubrica));
            using (Dm_Contatti_Search search = new Dm_Contatti_Search())// _manager.ARX_SEARCH.Dm_Contatti_Search_Get_New_Instance())
            using (Dm_Contatti_Select select = new Dm_Contatti_Select())//_manager.ARX_SEARCH.Dm_Contatti_Select_Get_New_Instance())
            {
                search.Dm_Rubrica.CODICE.SetFilter(Dm_Base_Search_Operatore_String.Uguale, anagrafica.Codice);
                search.Dm_CatRubriche.RUBRICA.SetFilter(Dm_Base_Search_Operatore_String.Uguale, nomeRubrica);

                select.DM_RUBRICA_SYSTEM_ID.Selected = true;

                using (var ds = _manager.ARX_SEARCH.Dm_Contatti_GetData(search, select))
                {

                    if (ds.RowsCount(0) == 0)
                    {
                        valoreRitorno = ContattoInRubricaAggiungi(anagrafica, idRubrica, utente);
                    }
                    else
                    {
                        valoreRitorno = int.Parse(ds.GetCellValue(0, 0, 0).ToString());
                        ContattoInRubricaUpdate(anagrafica, valoreRitorno);
                    }
                }
            }
            return valoreRitorno;
        }

        private void ContattoInRubricaUpdate(ElementoAnagraficaCliFor recordDaModificare, int valoreRitorno)
        {
            bool modificato = false;
            using (var update = _manager.ARX_DATI.Dm_Rubrica_ForUpdate_GetNewInstance_By_SystemId(valoreRitorno))
            {
                if (recordDaModificare.RagioneSociale != null && !update.RAGIONE_SOCIALE.Equals(recordDaModificare.RagioneSociale))
                {
                    update.RAGIONE_SOCIALE = recordDaModificare.RagioneSociale;
                    modificato = true;
                }

                if (recordDaModificare.Piva != null && !update.PARTIVA.Equals(recordDaModificare.Piva))
                {
                    update.PARTIVA = recordDaModificare.Piva;
                    modificato = true;
                }

                if (recordDaModificare.CodiceFiscale != null && !update.CODFIS.Equals(recordDaModificare.CodiceFiscale))
                {
                    update.CODFIS = recordDaModificare.CodiceFiscale;
                    modificato = true;
                }

                if (recordDaModificare.Email != null && !update.MAIL.Equals(recordDaModificare.Email))
                {
                    update.MAIL = recordDaModificare.Email;
                    modificato = true;
                }

                //if (recordDaModificare.EmailPec != null && !update.EmailPec.Equals(recordDaModificare.EmailPec))
                //{
                //    update.EmailPec = recordDaModificare.EmailPec;
                //    modificato = true;
                //}


                if (recordDaModificare.EmailPec != null)
                {
                    var selezione = update.Aggiuntivi.FirstOrDefault(x => x.Nome.Equals("Email_PEC", StringComparison.CurrentCultureIgnoreCase));

                    if (((Aggiuntivo_String)selezione).Valore != null && !((Aggiuntivo_String)selezione).Valore.Equals(recordDaModificare.EmailPec))
                    {

                        ((Aggiuntivo_String)selezione).Valore = recordDaModificare.EmailPec;
                        modificato = true;

                    }
                }


                if (modificato)
                {
                    using (var risultatoUpdate = _manager.ARX_DATI.Dm_Rubrica_Update(update))
                    {
                        if (risultatoUpdate != null)
                        {

                        }
                    }
                }
            }
        }

        private int ContattoInRubricaAggiungi(ElementoAnagraficaCliFor recordDaaInserire, int idrubrica, int IdUtente)
        {
            int utenteImpersonificato = 0;

            if (_manager.UserGrantor != null)
            {
                utenteImpersonificato = _manager.UserGrantor.UTENTE;
                _manager.DeImpersonate();
            }
            int ritorno = 0;

            try
            {
                using (Dm_Rubrica_ForInsert insert = _manager.ARX_DATI.Dm_Rubrica_ForInsert_GetNewInstance_By_DmCatRubricheId(idrubrica))
                {
                    insert.CODICE = recordDaaInserire.Codice;
                    insert.RAGIONE_SOCIALE = recordDaaInserire.RagioneSociale;
                    insert.PARTIVA = recordDaaInserire.Piva;
                    insert.CODFIS = recordDaaInserire.CodiceFiscale;
                    insert.MAIL = recordDaaInserire.Email;

                    if (!string.IsNullOrEmpty(recordDaaInserire.EmailPec))
                    {
                        var selezione = insert.Aggiuntivi.FirstOrDefault(x => x.Nome.Equals("Email_PEC", StringComparison.CurrentCultureIgnoreCase));

                        if (selezione != null)
                        {
                            ((Aggiuntivo_String)selezione).Valore = recordDaaInserire.EmailPec;
                        }
                    }

                    Dm_Permessi_Rubrica permesso = _manager.ARX_DATI.Dm_Permessi_Rubrica_Get_Data_By_DmCatRubricheIdAndUserConnected(idrubrica);
                    if (System.Convert.ToInt32(permesso.ACCESSO) <= 0) //permesso in lettura
                    {
                        throw new Exception(string.Format("L'utente {0} non ha i permessi di scrittura sulla rubrica fornitori", IdUtente));
                    }
                    else
                    {
                        var risultato_import = _manager.ARX_DATI.Dm_Rubrica_Insert(insert);
                        ritorno = risultato_import.SYSTEM_ID;
                        WriteWarning(string.Format("Inserimnto Rubrica {0}: l'inserimento del codice {1} è andato a buon fine", idrubrica.ToString(), recordDaaInserire.RagioneSociale));
                    }
                }
            }
            catch (Exception erroreinserimento)
            {
                WriteError(string.Format("Errore durante l'inserimento in Rubrica {0} per l'utente {2}: {1}", idrubrica, erroreinserimento.Message, IdUtente));
            }
            finally
            {
                if (!utenteImpersonificato.Equals(0))
                {
                    _manager.Impersonate_By_UserId(utenteImpersonificato);
                }
            }

            return ritorno;
        }



        #endregion

    }
}
