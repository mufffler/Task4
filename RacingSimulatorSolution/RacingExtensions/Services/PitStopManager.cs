using System;
using RacingExtensions.Interfaces;

namespace RacingExtensions.Services
{
    public class PitStopManager : IRacingService
    {
        public string ServiceName { get; }
        public string TeamName { get; }

        public PitStopManager(string serviceName, string teamName)
        {
            ServiceName = serviceName;
            TeamName = teamName;
        }

        public string RefuelAndChangeTires(string carName, int fuelAmount, bool changeToSoft)
        {
            string tireType = changeToSoft ? "Soft (Мягкие)" : "Hard (Жесткие)";
            return $"Команда {TeamName} обслужила {carName}: залито {fuelAmount}л топлива, установлен комплект {tireType}.";
        }
    }
}