using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Inforigami.Regalo.Core.EventSourcing;
using Inforigami.Regalo.Core.Tests.DomainModel.Users;

namespace Inforigami.Regalo.Core.Tests.Unit
{
    [TestFixture]
    public class InMemoryEventStoreTests : TestFixtureBase
    {
        [Test]
        public void GivenEmptyEventStore_WhenLoadingEvents_ThenNothingShouldBeReturned()
        {
            // Arrange
            IEventStore store = new InMemoryEventStore();
            
            // Act
            IEnumerable<object> events = store.Load(Guid.NewGuid());

            // Assert
            CollectionAssert.IsEmpty(events);
        }

        [Test]
        public void GivenEmptyEventStore_WhenLoadingEventsForSpecificVersion_ThenNothingShouldBeReturned()
        {
            // Arrange
            IEventStore store = new InMemoryEventStore();

            // Act
            IEnumerable<object> events = store.Load(Guid.NewGuid(), 1);

            // Assert
            CollectionAssert.IsEmpty(events);
        }

        [Test]
        public void GivenEmptyEventStore_WhenAddingEventsOneAtATime_ThenStoreShouldContainThoseEvents()
        {
            // Arrange
            IEventStore store = new InMemoryEventStore();
            var userId = Guid.NewGuid();
            var userRegistered = new UserRegistered(userId);

            // Act
            store.Update(userId, new[] { userRegistered });

            // Assert
            CollectionAssert.IsNotEmpty(((InMemoryEventStore)store).Events);
            Assert.AreSame(userRegistered, ((InMemoryEventStore)store).Events.First());
        }

        [Test]
        public void GivenEmptyEventStore_WhenAddingEventsInBulk_ThenStoreShouldContainThoseEvents()
        {
            // Arrange
            IEventStore store = new InMemoryEventStore();
            var user = new User();
            user.Register();
            user.ChangePassword("newpassword");

            // Act
            store.Update(user.Id, user.GetUncommittedEvents());

            // Assert
            CollectionAssert.IsNotEmpty(((InMemoryEventStore)store).Events);
            CollectionAssert.AreEqual(user.GetUncommittedEvents(), ((InMemoryEventStore)store).Events);
        }

        [Test]
        public void GivenEventStorePopulatedWithEventsForMultipleAggregates_WhenLoadingEventsForAnAggregate_ThenShouldReturnEventsForThatAggregate()
        {
            // Arrange
            IEventStore store = new InMemoryEventStore();

            var user1 = new User();
            user1.Register();
            user1.ChangePassword("user1pwd1");
            user1.ChangePassword("user1pwd2");

            var user2 = new User();
            user2.Register();
            user2.ChangePassword("user2pwd1");
            user2.ChangePassword("user2pwd2");

            store.Update(user1.Id, user1.GetUncommittedEvents());
            store.Update(user2.Id, user2.GetUncommittedEvents());

            // Act
            IEnumerable<object> eventsForUser1 = store.Load(user1.Id);

            // Assert
            CollectionAssert.AreEqual(user1.GetUncommittedEvents(), eventsForUser1, "Store didn't return user1's events properly.");
        }

        [Test]
        public void GivenEventStorePopulatedWithManyEventsForAnAggregate_WhenLoadingForSpecificVersion_ThenShouldOnlyLoadEventsUpToAndIncludingThatVersion()
        {
            // Arrange
            IEventStore store = new InMemoryEventStore();
            var id = Guid.NewGuid();
            var allEvents = new EventChain().Add(new UserRegistered(id))
                                            .Add(new UserChangedPassword("pwd1"))
                                            .Add(new UserChangedPassword("pwd2"))
                                            .Add(new UserChangedPassword("pwd3"))
                                            .Add(new UserChangedPassword("pwd4"));

            store.Update(id, allEvents);

            // Act
            IEnumerable<Event> version3 = store.Load(id, allEvents[2].Version).ToArray();

            // Assert
            CollectionAssert.AreEqual(allEvents.Take(3).ToArray(), version3);
        }
    }
}
