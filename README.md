# EventManager

EventManager сейчас разделен на три независимых Web API сервиса и общий проект с контрактами сообщений.

## Состав системы

| Проект | Ответственность | API порт | База данных |
| --- | --- | --- | --- |
| `Users` | Регистрация, вход, хеширование пароля, выдача JWT | `http://localhost:5202` | PostgreSQL `users`, порт хоста `5433` |
| `Events` | CRUD событий, учет доступных мест, обработка запросов на бронь | `http://localhost:5203` | PostgreSQL `events`, порт хоста `5434` |
| `Bookings` | Создание, просмотр и отмена броней | `http://localhost:5075` | PostgreSQL `bookings`, порт хоста `5435` |
| `Contracts` | Общие DTO, enum'ы и Kafka-контракты | не запускается | базы нет |

Инфраструктура для локального запуска находится в `docker/docker-compose.yml`:

- `users-db`, `events-db`, `bookings-db` - отдельные PostgreSQL базы под каждый сервис;
- `kafka` - брокер сообщений, доступен с хоста по `localhost:9092`;
- `redis` - кеш для сервиса `Events`, доступен с хоста по `localhost:6379`;
- `zookeeper` - служебный контейнер для Kafka.

У сервисов нет навигационных свойств между чужими доменными сущностями. Связи между сервисами хранятся только как идентификаторы: `UserId`, `EventId`, `BookingId`.

## Контракты сообщений

Контракты лежат в проекте `src/Contracts`. Это публичный договор между сервисами: название топика и структура сообщения должны быть одинаковыми у издателя и подписчика.

Используемые топики:

| Топик | Издатель | Подписчик | Назначение |
| --- | --- | --- | --- |
| `booking-requested` | `Bookings` | `Events` | Пользователь создал бронь, нужно проверить событие и места |
| `booking-confirmed` | `Events` | `Bookings` | Места успешно зарезервированы, бронь можно подтвердить |
| `booking-rejected` | `Events` | `Bookings` | Бронь нельзя подтвердить, нужно перевести ее в отказ |
| `booking-cancelled` | `Bookings` | `Events` | Подтвержденная бронь отменена, место нужно вернуть событию |

## Поток BookingConfirmed

Полный сценарий бронирования выглядит так:

1. Пользователь вызывает `Bookings` и создает бронь.
2. `Bookings` сохраняет бронь в своей БД со статусом `Pending`.
3. После сохранения `Bookings` публикует сообщение `BookingRequested` в топик `booking-requested`.
4. `Events` читает `BookingRequested`, находит событие в своей БД и проверяет доступность мест.
5. Если место есть, `Events` уменьшает `AvailableSeats`, сохраняет изменение в своей БД и публикует `BookingConfirmed` в топик `booking-confirmed`.
6. `Bookings` читает `BookingConfirmed`, находит бронь в своей БД и переводит ее из `Pending` в `Confirmed`.

Если событие не найдено, уже началось или свободных мест нет, `Events` публикует `BookingRejected`, а `Bookings` переводит бронь в `Rejected`.

Важно: `Bookings` не уменьшает места у события и не обращается к `Events` напрямую. `Events` не хранит брони и пользователей целиком, а работает только с идентификаторами из сообщения.

## Стратегия кеширования Events

Сервис `Events` использует Redis через пакет `StackExchange.Redis`. Подключение зарегистрировано в DI как singleton `IConnectionMultiplexer`, потому что Redis-клиент потокобезопасный и рассчитан на переиспользование в течение жизни приложения. Строка подключения хранится в `Events/appsettings.json` в параметре `Redis:ConnectionString`; для локального запуска используется `localhost:6379,abortConnect=false`, а при контейнерном запуске значение можно переопределить переменной окружения `Redis__ConnectionString`.

Кеширование изолировано за интерфейсом `IEventsCacheService` в Application-слое, реализация `EventsCacheService` находится в Infrastructure. Если Redis недоступен, операции кеша логируют предупреждение и не пробрасывают ошибку клиенту: чтение считается промахом кеша, запись или удаление просто пропускается, а основная бизнес-операция продолжает работать через базу данных.

Кешируются два сценария:

- `GET /events/{id}` — cache-aside по ключу `event:{id}`. Сначала читается Redis; при промахе событие загружается из БД и сохраняется в кеш.
- `GET /events/top` — топ популярных событий по ключу `events:top10`. Популярность считается как `(TotalSeats - AvailableSeats) / TotalSeats`, сортировка идет по убыванию процента проданных мест.

TTL вынесены в `Events/appsettings.json`:

- `Cache:EventsTtlMinutes` — время жизни кеша отдельного события;
- `Cache:TopEventsTtlMinutes` — время жизни кеша топа событий.

Для отдельного события выбрана стратегия обновления при записи: после изменения события или обработки Kafka-сообщения кеш `event:{id}` обновляется после успешного сохранения в БД, а при удалении события ключ удаляется после успешного удаления из БД. Для топа используется TTL без ручной инвалидации при каждой брони: это рейтинговый агрегат, небольшое устаревание допустимо, а частая инвалидация при изменении мест дала бы лишнюю нагрузку.

## Запуск

Самый простой способ запустить всю систему локально:

```powershell
cd C:\Users\mariya.zhmakina\Work\EventManager\docker
docker compose up --build
```

Команда собирает Docker-образы API-сервисов и поднимает всю систему:

- `Users`;
- `Events`;
- `Bookings`;
- `users-db`, `events-db`, `bookings-db`;
- `Kafka`, `Zookeeper`, `Redis`.

По умолчанию используется development-сборка из общего `docker/Dockerfile`. Для production-сборки без скриптов можно переключить Docker target и окружение:

```powershell
$env:DOCKER_BUILD_TARGET="production"
$env:ASPNETCORE_ENVIRONMENT="Production"
$env:DOTNET_ENVIRONMENT="Production"
docker compose up --build
```

Swagger:

- Users/Auth: `http://localhost:5202/swagger`
- Events: `http://localhost:5203/swagger`
- Bookings: `http://localhost:5075/swagger`

Остановить контейнеры:

```powershell
docker compose down
```

## Авторизация в Swagger

1. Откройте `Users` Swagger: `http://localhost:5202/swagger`.
2. Зарегистрируйте пользователя или администратора.
3. Выполните вход и скопируйте JWT-токен.
4. В Swagger сервисов `Events` и `Bookings` нажмите `Authorize` и вставьте токен.

## Ручной запуск без скрипта

Если нужно отлаживать API из IDE или через `dotnet run`, поднимите через Docker только инфраструктуру:

```powershell
cd C:\Users\mariya.zhmakina\Work\EventManager\docker
docker compose up -d users-db events-db bookings-db redis kafka
```

В отдельных терминалах:

```powershell
cd C:\Users\mariya.zhmakina\Work\EventManager\src
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run --project .\Users\Users.csproj --urls "http://localhost:5202"
```

```powershell
cd C:\Users\mariya.zhmakina\Work\EventManager\src
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run --project .\Events\Events.csproj --urls "http://localhost:5203"
```

```powershell
cd C:\Users\mariya.zhmakina\Work\EventManager\src
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run --project .\Bookings\Bookings.csproj --urls "http://localhost:5075"
```

## Тесты

Запуск всех тестов из папки `src`:

```powershell
cd C:\Users\mariya.zhmakina\Work\EventManager\src
dotnet test .\EventManager.sln
```

Unit-тесты проверяют бизнес-сценарии сервисов без Kafka:

- `Bookings` сохраняет бронь как `Pending` и публикует `BookingRequested`;
- `Events` обрабатывает `BookingRequested`, списывает место и публикует `BookingConfirmed`;
- `Bookings` обрабатывает `BookingConfirmed` и переводит бронь в `Confirmed`;
- отмена подтвержденной брони публикует `BookingCancelled`, а `Events` возвращает место.
