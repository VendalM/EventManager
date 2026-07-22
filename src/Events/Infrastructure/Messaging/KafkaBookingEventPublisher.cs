using System.Text.Json;
using Confluent.Kafka;
using Contracts.Messages;
using Events.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace Events.Infrastructure.Messaging;

/// <summary>
/// Kafka-реализация издателя событий бронирования.
/// </summary>
public sealed class KafkaBookingEventPublisher : IEventsEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;

    /// <summary>
    /// Создает producer для отправки сообщений в Kafka.
    /// </summary>
    /// <param name="options">Настройки подключения к Kafka.</param>
    public KafkaBookingEventPublisher(IOptions<KafkaOptions> options)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    /// <summary>
    /// Публикует событие о подтверждении брони в Kafka.
    /// </summary>
    /// <param name="message">Контракт события подтверждения брони.</param>
    /// <param name="cancellationToken">Токен отмены операции публикации.</param>
    public async Task PublishBookingConfirmedAsync(
        BookingConfirmed message,
        CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(message);

        await _producer.ProduceAsync(
            BookingTopics.BookingConfirmed,
            new Message<string, string>
            {
                // Ключом выбран EventId, чтобы сообщения по одному событию обрабатывались по порядку.
                Key = message.EventId.ToString(),
                Value = json
            },
            cancellationToken);
    }
    
    /// <summary>
    /// Публикует событие отклонения брони в Kafka.
    /// </summary>
    /// <param name="message">Контракт события снятия брони.</param>
    /// <param name="cancellationToken">Токен отмены публикации отклонения.</param>
    public async Task PublishBookingRejectedAsync(
        BookingRejected message,
        CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(message);

        await _producer.ProduceAsync(
            BookingTopics.BookingRejected,
            new Message<string, string>
            {
                // Ключом выбран EventId, чтобы сообщения по одному событию обрабатывались по порядку.
                Key = message.EventId.ToString(),
                Value = json
            },
            cancellationToken);
    }

    /// <summary>
    /// Освобождает Kafka producer и перед закрытием пытается отправить накопленные сообщения.
    /// </summary>
    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(10));
        _producer.Dispose();
    }
}
