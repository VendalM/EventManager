using System.Text.Json;
using Bookings.Application.Interfaces;
using Confluent.Kafka;
using Contracts.Messages;
using Microsoft.Extensions.Options;

namespace Bookings.Infrastructure.Messaging;

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
            BookingTopics.BookingConfirmed,
            BookingTopics.BookingRejected
        });

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);

                    using var scope = _scopeFactory.CreateScope();
                    var handler = scope.ServiceProvider.GetRequiredService<IBookingStatusHandler>();

                    switch (result.Topic)
                    {
                        case BookingTopics.BookingConfirmed:
                            var requested = JsonSerializer.Deserialize<BookingConfirmed>(result.Message.Value);
                            if (requested != null)
                                await handler.HandleBookingConfirmedAsync(requested, stoppingToken);
                            break;

                        case BookingTopics.BookingRejected:
                            var cancelled = JsonSerializer.Deserialize<BookingRejected>(result.Message.Value);
                            if (cancelled != null)
                                await handler.HandleBookingRejectedAsync(cancelled, stoppingToken);
                            break;
                    }
                } catch (ConsumeException ex)
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