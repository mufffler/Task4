using System;
using RacingExtensions.Interfaces;

namespace RacingExtensions.Services
{
    public class EngineTuner : IRacingService
    {
        public string ServiceName { get; }
        public int BaseBoost { get; private set; }

        // Параметры передаются извне
        public EngineTuner(string serviceName, int baseBoost)
        {
            ServiceName = serviceName;
            BaseBoost = baseBoost;
        }

        public string TuneEngine(string carName, double currentSpeed, int stages)
        {
            double newSpeed = currentSpeed + (stages * 5.5) + BaseBoost;
            return $"[Модификация] {carName} прошел тюнинг ({stages} шт.). Скорость увеличена с {currentSpeed:F1} до {newSpeed:F1} м/с.";
        }

        public string ResetSettings()
        {
            return "Все настройки двигателя сброшены до заводских.";
        }
    }
}