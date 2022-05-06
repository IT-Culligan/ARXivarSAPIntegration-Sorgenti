using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArxivarSAPIntegration
{
    public enum EnumTipoDocumento { DDTOUT = 1, OV = 2, DDTIN=3, InvoiceIn=4, InvoiceOut=5, InvoiceOutFRIZZY = 6 };

    public enum EnumTipoAnagrafica { Clienti = 1, Fornitori = 2 };

    public class RecordGestionale
    {
        public EnumTipoDocumento TipoDocumento { get; set; }
        public int ID { get; set; }
        public string classe { get; set; }
        public string codiceContatore { get; set; }
        public string numerodocumento { get; set; }
        public DateTime dataDocumento { get; set; }
        public string codiceClienteFornitore { get; set; }
        public string codiceClienteMF { get; set; }
        public string azienda { get; set; }
        public string ragionesociale { get; set; }
        public string destinazioneMerce { get; set; }
        public string partitaIva { get; set; }
        public string codiceFiscale { get; set; }
        public string trattamento { get; set; }
        public string tipoDocumento { get; set; }
        public string allegati { get; set; }
        public string oggetto { get; set; }
        public int codiceNumero { get; set; }
        public string struttura { get; set; }
        public string agente { get; set; }
        public string vostroDocumento { get; set; }
        public DateTime vostroDocumentodata { get; set; }
        public string codiceAutore { get; set; }
        public string barcode { get; set; }
        public string tipologiaImport { get; set; }
        public int idOpportunità { get; set; }
        public string serialNum { get; set; }
        public int idMF { get; set; }
        public string codiceIndirizzoSpedizione { get; set; }
        public DateTime dataReso { get; set; }
        public DateTime dataArrivoReso { get; set; }
        public int flagReso { get; set; }
        public int flagRicevutoReso { get; set; }
        public int contator { get; set; }
        public DateTime dataprotocollo { get; set; }
        public int esercizio { get; set; }
        public string registroIva { get; set; }
        public string protocollo { get; set; }
        public string contocontabile { get; set; }
        public int idcontratto { get; set; }
        public string filialeCRM { get; set; }
        public string codiceContratto { get; set; }
        public string tipoContratto { get; set; }
        public int ID_arx { get; set; }
        public string flusso { get; set; }
        public string idScad { get; set; }
        public string note { get; set; }
        public double importo { get; set; }
        public string causaleBlocco { get; set; }
        public string tipoReclamo { get; set; }
        public string Prodotto { get; set; }
        public string ProdottoDescrizione { get; set; }
        public DateTime Data_Scad { get; set; }
        public string Tipo_Pag { get; set; }
        public List<RecordGestionaleRigaRicambi> righeRicambi { get; set; }
        public int idClaimOv { get; set; }
        public string odl_nr { get; set; }
        public string bu { get; set; }
        public string codice_tecnico { get; set; }
        public string cd_prodo { get; set; }
        public string clienteFatturazione { get; set; }
        public string clienteSpedizione { get; set; }
        public string seriale { get; set; }
        public string nomefileint { get; set; }
        public string cd_cliente { get; set; }
        public string Utente { get; set; }
        public string OrderReason { get; set; }
        public string BillingDocument { get; set; }
        public string AccountingDocumentType { get; set; }
        public Int64 protIvaGest { get; set; }
        public Int64 numRegGest { get; set; }
        public string cdIndi { get; set; }

    }

    public class RecordGestionaleRigaRicambi
    {
        public string Prodotto { get; set; }
        public double Quantità { get; set; }
        public string Descprodotto { get; set; }
    }

    public class DocumentiImportati
    {
        public EnumTipoDocumento Tipo { get; set; }
        public string Chiave { get; set; }
    }

    public class ElementoAnagraficaCliFor
    {
        public string Codice { get; set; }
        public string RagioneSociale { get; set; }
        public string Indirizzo { get; set; }
        public string Localita { get; set; }
        public string Cap { get; set; }
        public string CodiceFiscale { get; set; }
        public string Piva { get; set; }
        public string Email { get; set; }
        public string EmailPec { get; set; }
        public EnumTipoAnagrafica TipoAnagrafica { get; set; }
        public string Nazione { get; set; }
    }

    public class ElementoUtente
    {
        public int Utente { get; set; }
        public string Descrizione { get; set; }
    }
}
