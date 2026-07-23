namespace Events.Infrastructure.Messaging;

/// <summary>
/// Настройки подключения к Kafka-брокеру.
/// </summary>
public sealed class KafkaOptions
{
    /// <summary>
    /// Адреса Kafka-брокеров, через которые приложение подключается к кластеру.
    /// </summary>
    public string BootstrapServers { get; set; } = string.Empty;
    
    /// <summary>
    /// Имя группы потребителей, внутри которой Kafka распределяет сообщения между экземплярами сервиса.
    /// </summary>
    public string ConsumerGroup { get; set; } = string.Empty;
}
