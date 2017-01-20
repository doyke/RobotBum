﻿using TowerBotLibCore.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TowerBotFoundationCore;

namespace TowerBotLibCore
{
    public static class PluginsManager
    {
        public static List<AirplaneBasic> ListOldAirplanes { get; set; }

        public static DateTime TimeNext { get; set; }
        public static TimeSpan Period { get; set; }
        public static List<Alert> listOldAlerts { get; set; }

        static PluginsManager()
        {            
            ListOldAirplanes = new List<AirplaneBasic>();
            Period = new TimeSpan(0, 0, 15);
            listOldAlerts = new List<Alert>();

        }
        
        public static List<Alert> GetAlerts(bool updateAll)
        {
            List<Alert> listAlerts = new List<Alert>();

            if (TimeNext == null || TimeNext <= DateTime.Now || updateAll)
            {

                for (int i = 0; i < Radar.ListRadars.Count; i++)
                {
                    var radar = Radar.ListRadars[i];

                    List<AirplaneBasic> listAirplanes = null;
                    if (radar != null)
                    {

                        listAirplanes = AirplanesData.GetAirplanes(radar).Result;
                        var newAlerts = Run(radar, listAirplanes);

                        var listToDelete = new List<Alert>();

                        listToDelete.ForEach(item => newAlerts.Remove(item));

                        listAlerts.AddRange(newAlerts);

                    }

                    ListOldAirplanes = listAirplanes;

                }

                TimeNext = DateTime.Now + Period;

            }

            return listAlerts;


        }

        private static List<Alert> Run(Radar radar, object parameter = null)
        {

            List<Alert> listAlerts = new List<Alert>();

            for (int i = 0; i < radar.Plugins.Count; i++)
            {
                listAlerts.AddRange(radar.Plugins[i].Analyser(parameter));
            }

            // Verify if there are some alert older then it's time to be removed
            if (listAlerts.Count > 0)
            {
                List<Alert> list = listOldAlerts.Where(s => s.TimeToBeRemoved <= DateTime.Now).ToList();
                for (int i = 0; i < list.Count; i++)
                {
                    listOldAlerts.Remove(list[i]);
                }
            }

            // Verify if there is any alert equal.
            List<Alert> listAlertLessThenOneHour = listOldAlerts;
            if (radar.Name == "BRA")
                listAlertLessThenOneHour = listOldAlerts.Where(s => s.TimeCreated > DateTime.Now.AddHours(-1)).ToList();

            for (int i = 0; i < listAlertLessThenOneHour.Count; i++)
            {
                var alertEqual = listAlerts.Where(s => s.ID == listAlertLessThenOneHour[i].ID && s.AlertType == listAlertLessThenOneHour[i].AlertType).ToList().LastOrDefault();
                if (alertEqual != null)
                {
                    if (alertEqual.Level <= listAlertLessThenOneHour[i].Level)
                    {
                        listAlerts.Remove(alertEqual);
                    }
                }

                // Verify if there are some alert from the same group
                if (!String.IsNullOrEmpty(listAlertLessThenOneHour[i].Group))
                {
                    var alertSameGroup = listAlerts.Where(s => s.ID != listAlertLessThenOneHour[i].ID && s.Group == listAlertLessThenOneHour[i].Group).ToList();
                    for (int j = 0; j < alertSameGroup.Count; j++)
                    {
                        listAlertLessThenOneHour[i].Group = String.Empty;
                        listAlertLessThenOneHour[i].TimeToBeRemoved = listAlertLessThenOneHour[i].TimeCreated.AddDays(1);
                        listAlertLessThenOneHour[i].ID += listAlertLessThenOneHour[i].TimeCreated.ToString("ddMMyyyyhhmm");

                    }
                }

            }

            listOldAlerts.AddRange(listAlerts);

            return listAlerts;

        }

        public static void RefreshAll()
        {
            TimeNext = new DateTime(1988, 4, 1);
            listOldAlerts = new List<Alert>();
        }

        public static void AccessPluginCommandLine()
        {
            Console.WriteLine("Qual filtro você deseja acessar?\n");

            for (int i = 0; i < Radar.ListRadars.Count; i++)
            {
                for (int j = 0; j < Radar.ListRadars[i].Plugins.Count; j++)
                {
                    Console.WriteLine("-{0} (ativo:{1}, em teste:{2})", Radar.ListRadars[i].Plugins[j].Name, Radar.ListRadars[i].Plugins[j].IsActive, Radar.ListRadars[i].Plugins[j].IsTesting);
                }
            }
            string comando = Console.ReadLine();
            IPlugin selectedPlugin = null;
            for (int i = 0; i < Radar.ListRadars[i].Plugins.Count; i++)
            {
                for (int j = 0; j < Radar.ListRadars[i].Plugins.Count; j++)
                {
                    if (Radar.ListRadars[i].Plugins[j].Name.ToLower().StartsWith(comando.ToLower()))
                    {
                        selectedPlugin = Radar.ListRadars[i].Plugins[j];
                        selectedPlugin.CommandLine();
                        break;
                    }

                }
            }

            if (selectedPlugin == null)
            {
                Console.WriteLine("Filtro não encontrado.");
            }

        }

    }
}
