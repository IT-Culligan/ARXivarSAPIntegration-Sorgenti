using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.IO;

namespace ArxivarSAPIntegration
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            string assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

            // leggo il file di configurazione, nella sottocartella Impostazioni dell'EXE
            string configurationPath = Path.Combine(Path.GetDirectoryName(assemblyLocation), "Impostazioni");
            if (!Directory.Exists(configurationPath)) Directory.CreateDirectory(configurationPath);
            ServiceConfiguration config = ServiceConfiguration.CaricaConfigurazione(assemblyName, "ArxivarSAPIntegration", configurationPath, typeof(ServiceConfiguration)) as ServiceConfiguration;
            // se non c'è il file di configurazione creo la classe base
            if (config == null) config = new ServiceConfiguration(assemblyName, "ArxivarSAPIntegration", configurationPath);
            // aggiorno il percorso di configurazione
            config.PathConfiguration = configurationPath;

            if (args.Contains("/config"))
            {
                // apro la form di configurazione
                FormConfiguration form = new FormConfiguration(config);
                form.ShowDialog();
                if (form.ToSave) config.SalvaConfigurazione();
            }
            else if (args.Contains("/debug"))
            {
                // trucco per entrare in debug nel codice di esecuzione
                ArxivarSAPIntegration debugService = new ArxivarSAPIntegration(config);
                debugService.StartService();
                System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
            }
            else
            {
                // avvio del servizio vero e proprio
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                    new ArxivarSAPIntegration(config)
                };
                ServiceBase.Run(ServicesToRun);
            }
        }
    }
}
