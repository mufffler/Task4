namespace RacingExtensions.Interfaces
{
    /// <summary>
    /// Базовый интерфейс для подключаемых гоночных сервисов
    /// </summary>
    public interface IRacingService
    {
        string ServiceName { get; }
    }
}