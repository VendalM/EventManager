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

## Запуск

Самый простой способ запустить всю систему локально:

```powershell
.\scripts\start-dev.ps1
```

Скрипт:

- поднимает Docker-инфраструктуру;
- собирает три API проекта;
- запускает `Users`, `Events` и `Bookings` в фоне;
- пишет логи в `.run/logs`;
- показывает ссылки на Swagger.

Если PowerShell блокирует запуск `.ps1`, используйте командный файл:

```powershell
.\scripts\start-dev.cmd
```

Swagger:

- Users/Auth: `http://localhost:5202/swagger`
- Events: `http://localhost:5203/swagger`
- Bookings: `http://localhost:5075/swagger`

Остановить только API сервисы:

```powershell
.\scripts\stop-dev.ps1
```

Остановить API сервисы и Docker-инфраструктуру:

```powershell
.\scripts\stop-dev.ps1 -WithDocker
```

## Авторизация в Swagger

1. Откройте `Users` Swagger: `http://localhost:5202/swagger`.
2. Зарегистрируйте пользователя или администратора.
3. Выполните вход и скопируйте JWT-токен.
4. В Swagger сервисов `Events` и `Bookings` нажмите `Authorize` и вставьте токен.

## Ручной запуск без скрипта

Если нужно запустить по шагам:

```powershell
cd C:\Users\mariya.zhmakina\Work\EventManager\docker
docker compose up -d
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
