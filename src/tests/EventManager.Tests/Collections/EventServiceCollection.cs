
using EventManager.Tests.Fixtures;

namespace EventManager.Tests.Collections;

/// <summary>
/// Коллекция тестов для EventService с общим контекстом
/// </summary>
[CollectionDefinition("EventService collection")]
public class EventServiceCollection : ICollectionFixture<EventServiceFixture>
{
}