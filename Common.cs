using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Abletech.Arxivar.Entities.Search;
using Abletech.Arxivar.Entities.Enums;
using Abletech.Arxivar.Entities.Libraries;
using Abletech.Arxivar.Entities;
using Abletech.Arxivar.Client;

namespace Abletech.Arxivar
{
    class Common
    {
        #region Gestione log
        public delegate void OnWriteInfoDelegate(string message, params object[] args);
        public delegate void OnWriteWarningDelegate(string message, params object[] args);
        public delegate void OnWriteErrorDelegate(string message, params object[] args);

        public event OnWriteInfoDelegate OnWriteInfo;
        public event OnWriteWarningDelegate OnWriteWarning;
        public event OnWriteErrorDelegate OnWriteError;

        private void WriteInfo(string message, params object[] args)
        {
            if (OnWriteInfo != null) OnWriteInfo(message, args);
        }
        
        private void WriteWarning(string message, params object[] args)
        {
            if (OnWriteWarning != null) OnWriteWarning(message, args);
        }

        private void WriteError(string message, params object[] args)
        {
            if (OnWriteError != null) OnWriteError(message, args);
        }
        #endregion

        #region Gestione Aggiuntivi

        public string SelezionaAggiuntivo(Dm_Profile_Select select, string externalId, int index)
        {
            Aggiuntivo_Selected agg = select.Aggiuntivi.FirstOrDefault(x => x.ExternalId.Equals(externalId, StringComparison.CurrentCultureIgnoreCase));
            if (agg == null) throw new Exception("Campo aggiuntivo non trovato: " + externalId);
            agg.Selected = true;
            agg.Index = index++;

            return agg.Nome;
        }

        public void RicercaAggiuntivo(Dm_Profile_Search search, string externalID, string valore)
        {
            Field_Abstract fieldAbstract = search.Aggiuntivi.FirstOrDefault(a => a != null && a.ExternalId != null && a.ExternalId.Equals(externalID, StringComparison.CurrentCultureIgnoreCase));
            if (fieldAbstract != null && fieldAbstract is Field_String)
            {
                ((Field_String)fieldAbstract).SetFilter(Dm_Base_Search_Operatore_String.Uguale, valore);
            }
        }

        public void ValorizzaAggiuntivo(Aggiuntivo_Base[] aggiuntivi, string externalID, string valore)
        {
            if (aggiuntivi == null)
            {
                WriteError("Errore: non è presente alcun campo aggiuntivo!");
                return;
            }
            var aggiuntivo = aggiuntivi.FirstOrDefault(x => x != null && x.ExternalId != null && x.ExternalId.Equals(externalID, StringComparison.CurrentCultureIgnoreCase));
            if (aggiuntivo == null)
            {
                WriteError("Errore: il campo aggiuntivo {0} non esiste", externalID);
                return;
            }
            if (aggiuntivo is Aggiuntivo_String)
            {
                ((Aggiuntivo_String)aggiuntivo).Valore = valore;
                WriteInfo("Valore {0}: {1}", externalID, valore);
            }
            else
            {
                WriteError("Errore: il campo aggiuntivo {0} non è di tipo Aggiuntivo_String", externalID);
            }
        }

        public void ValorizzaAggiuntivo(Aggiuntivo_Base[] aggiuntivi, string externalID, DateTime valore)
        {
            if (aggiuntivi == null)
            {
                WriteError("Errore: non è presente alcun campo aggiuntivo!");
                return;
            }
            var aggiuntivo = aggiuntivi.FirstOrDefault(x => x != null && x.ExternalId != null && x.ExternalId.Equals(externalID, StringComparison.CurrentCultureIgnoreCase));
            if (aggiuntivo == null)
            {
                WriteError("Errore: il campo aggiuntivo {0} non esiste", externalID);
                return;
            }
            if (aggiuntivo is Aggiuntivo_DateTime)
            {
                ((Aggiuntivo_DateTime)aggiuntivo).Valore = valore;
                WriteInfo("Valore {0}: {1}", externalID, valore);
            }
            else
            {
                WriteError("Errore: il campo aggiuntivo {0} non è di tipo Aggiuntivo_DateTime", externalID);
            }
        }

        public void ValorizzaAggiuntivo(Aggiuntivo_Base[] aggiuntivi, string externalID, int valore)
        {
            if (aggiuntivi == null)
            {
                WriteError("Errore: non è presente alcun campo aggiuntivo!");
                return;
            }
            var aggiuntivo = aggiuntivi.FirstOrDefault(x => x != null && x.ExternalId != null && x.ExternalId.Equals(externalID, StringComparison.CurrentCultureIgnoreCase));
            if (aggiuntivo == null)
            {
                WriteError("Errore: il campo aggiuntivo {0} non esiste", externalID);
                return;
            }
            if (aggiuntivo is Aggiuntivo_Int)
            {
                ((Aggiuntivo_Int)aggiuntivo).Valore = valore;
                WriteInfo("Valore {0}: {1}", externalID, valore);
            }
            else
            {
                WriteError("Errore: il campo aggiuntivo {0} non è di tipo Aggiuntivo_Int", externalID);
            }
        }

        public void ValorizzaAggiuntivo(Aggiuntivo_Base[] aggiuntivi, string externalID, double valore)
        {
            if (aggiuntivi == null)
            {
                WriteError("Errore: non è presente alcun campo aggiuntivo!");
                return;
            }
            var aggiuntivo = aggiuntivi.FirstOrDefault(x => x != null && x.ExternalId != null && x.ExternalId.Equals(externalID, StringComparison.CurrentCultureIgnoreCase));
            if (aggiuntivo == null)
            {
                WriteError("Errore: il campo aggiuntivo {0} non esiste", externalID);
                return;
            }
            if (aggiuntivo is Aggiuntivo_Double)
            {
                ((Aggiuntivo_Double)aggiuntivo).Valore = valore;
                WriteInfo("Valore {0}: {1}", externalID, valore);
            }
            else
            {
                WriteError("Errore: il campo aggiuntivo {0} non è di tipo Aggiuntivo_Int", externalID);
            }
        }

        public void ValorizzaAggiuntivo(Aggiuntivo_Base[] aggiuntivi, string externalID, bool valore)
        {
            if (aggiuntivi == null)
            {
                WriteError("Errore: non è presente alcun campo aggiuntivo!");
                return;
            }
            var aggiuntivo = aggiuntivi.FirstOrDefault(x => x != null && x.ExternalId != null && x.ExternalId.Equals(externalID, StringComparison.CurrentCultureIgnoreCase));
            if (aggiuntivo == null)
            {
                WriteError("Errore: il campo aggiuntivo {0} non esiste", externalID);
                return;
            }
            if (aggiuntivo is Aggiuntivo_Bool)
            {
                ((Aggiuntivo_Bool)aggiuntivo).Valore = valore;
                WriteInfo("Valore {0}: {1}", externalID, valore);
            }
            else
            {
                WriteError("Errore: il campo aggiuntivo {0} non è di tipo Aggiuntivo_Int", externalID);
            }
        }

        public Arx_DataSource eseguiQuery(WCFConnectorManager _manager, Dm_Sql SQL, Arx_KeyValue[] filtri, bool ValoriDaRitornare)
        {
            Arx_DataSource ritorno = new Arx_DataSource();
            try
            {
                using (var query = _manager.ARX_DATI.Dm_Sql_GetValues(SQL.ID, filtri, null))
                {
                    if (ValoriDaRitornare)
                    {
                        ritorno = query.ArxDataSource;
                    }
                }
            }
            catch (Exception errore)
            {
                WriteError("Errore durante l'esecuzione della query {0}:{1}", SQL.EXTERNALID, errore.Message);
            }
            return ritorno;
        }

        public DateTime trasformainData(string valore)
        {
            DateTime ritorno;

            DateTime.TryParse(valore, out ritorno);

            return ritorno;
        }

        public int trasformainInt(string valore)
        {
            int ritorno = 0;

            int.TryParse(valore, out ritorno);

            return ritorno;
        }

        public double trasformainDoublie(string valore)
        {
            double ritorno = 0;

            double.TryParse(valore, out ritorno);

            return ritorno;
        }

        #endregion
    }
}
