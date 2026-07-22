// Интеграционные тесты поднимают PostgreSQL через Testcontainers.
// Последовательный запуск делает их стабильнее в IDE и не создает лишнюю гонку контейнеров.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
