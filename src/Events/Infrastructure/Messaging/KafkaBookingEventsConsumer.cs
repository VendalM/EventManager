using System.Text.Json;
using Confluent.Kafka;
using Contracts.Messages;
using Events.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace Events.Infrastructure.Messaging;

/// <summary>
/// Слушатель сообщений
/// </summary>
public sealed class KafkaBookingEventsConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KafkaOptions _options;
    private readonly ILogger<KafkaBookingEventsConsumer> _logger;

    public KafkaBookingEventsConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaOptions> options,
        ILogger<KafkaBookingEventsConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Обработка получения сообщений
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.ConsumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();

        consumer.Subscribe(new[]
        {
            BookingTopics.BookingRequested,
            BookingTopics.BookingCancelled
        });

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);

                    using var scope = _scopeFactory.CreateScope();
                    var handler = scope.ServiceProvider.GetRequiredService<IBookingEventHandler>();

                    switch (result.Topic)
                    {
                        case BookingTopics.BookingRequested:
                            var requested = JsonSerializer.Deserialize<BookingRequested>(result.Message.Value);
                            if (requested != null)
                                await handler.HandleBookingRequestedAsync(requested, stoppingToken);
                            break;

                        case BookingTopics.BookingCancelled:
                            var cancelled = JsonSerializer.Deserialize<BookingCancelled>(result.Message.Value);
                            if (cancelled != null)
                                await handler.HandleBookingCancelledAsync(cancelled, stoppingToken);
                            break;
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Ошибка чтения сообщения из Kafka.");
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Не удалось разобрать Kafka-сообщение.");
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка обработки Kafka-сообщения.");
                }
            }
        }
        finally
        {
            consumer.Close();
        }
    }
}