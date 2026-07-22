using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Contracts.Messages;
using Microsoft.Extensions.Options;

namespace Events.Infrastructure.Messaging;

/// <summary>
/// Hosted service, который при старте сервиса пытается создать Kafka-топики для обмена событиями бронирования.
/// </summary>
public sealed class KafkaTopicInitializer : IHostedService
{
    private readonly KafkaOptions _options;
    private readonly ILogger<KafkaTopicInitializer> _logger;

    /// <summary>
    /// Создает инициализатор Kafka-топиков.
    /// </summary>
    /// <param name="options">Настройки подключения к Kafka.</param>
    /// <param name="logger">Логгер для сообщений о создании топиков.</param>
    public KafkaTopicInitializer(
        IOptions<KafkaOptions> options,
        ILogger<KafkaTopicInitializer> logger)
    {
        _options = options.Value;
        _logger = logger;
    }
    
    /// <summary>
    /// Запускает инициализацию всех топиков, которые нужны сервисам бронирований и событий.
    /// </summary>
    /// <param name="cancellationToken">Токен остановки приложения.</param>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var topics = new[]
        {
            BookingTopics.BookingRequested,
            BookingTopics.BookingCancelled,
            BookingTopics.BookingConfirmed,
            BookingTopics.BookingRejected
        };

        foreach (var topic in topics)
        {
            await InitializeAsync(topic, cancellationToken);
        }
    }

    /// <summary>
    /// Пытается создать один Kafka-топик и не прерывает запуск приложения при ошибке создания.
    /// </summary>
    /// <param name="topicName">Имя создаваемого топика.</param>
    /// <param name="cancellationToken">Токен остановки приложения.</param>
    /// <param name="numPartitions">Количество разделов топика.</param>
    /// <param name="replicationFactor">Фактор репликации топика.</param>
    private async Task InitializeAsync(
        string topicName,
        CancellationToken cancellationToken,
        int numPartitions = 1,
        short replicationFactor = 1)
    {
        if (string.IsNullOrWhiteSpace(_options.BootstrapServers))
        {
            _logger.LogWarning("Kafka bootstrap servers are not configured. Topic initialization skipped.");
            return;
        }

        var config = new AdminClientConfig { BootstrapServers = _options.BootstrapServers };

        using var adminClient = new AdminClientBuilder(config).Build();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            await adminClient.CreateTopicsAsync(
                new[]
                {
                    new TopicSpecification
                    {
                        Name = topicName,
                        NumPartitions = numPartitions,
                        ReplicationFactor = replicationFactor
                    }
                });

            _logger.LogInformation("Kafka topic '{TopicName}' has been created.", topicName);
        }
        catch (CreateTopicsException ex)
        {
            var topicResult = ex.Results.FirstOrDefault(result => result.Topic == topicName);
            if (topicResult?.Error.Code == ErrorCode.TopicAlreadyExists)
            {
                _logger.LogInformation("Kafka topic '{TopicName}' already exists.", topicName);
                return;
            }

            _logger.LogWarning(
                ex,
                "Kafka topic '{TopicName}' could not be created. Application startup will continue.",
                topicName);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Kafka topic '{TopicName}' initialization failed. Application startup will continue.",
                topicName);
        }
    }
    
    /// <summary>
    /// Завершает работу инициализатора; отдельных ресурсов для освобождения нет.
    /// </summary>
    /// <param name="cancellationToken">Токен остановки приложения.</param>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}