using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Inforigami.Regalo.Interfaces;
using NUnit.Framework;
using Inforigami.Regalo.Core;
using Inforigami.Regalo.Messaging;
using Inforigami.Regalo.Testing;

namespace Inforigami.Regalo.Core.Tests.Unit
{
    [TestFixture]
    public class EventBusTests
    {
        private ObjectEventHandler _objectEventHandler;
        private EventHandlerA _eventHandlerA;
        private EventHandlerB _eventHandlerB;
        private EventBus _eventBus;

        [SetUp]
        public void SetUp()
        {
            _objectEventHandler = new ObjectEventHandler();
            _eventHandlerA = new EventHandlerA();
            _eventHandlerB = new EventHandlerB();
            _eventBus = new EventBusTestDataBuilder().Build();

            Resolver.Configure(type => null, LocateAllEventHandlers, o => { });
            Conventions.SetRetryableEventHandlingExceptionFilter((o, exception) => false);
        }

        private IEnumerable<object> LocateAllEventHandlers(Type type)
        {
            return new object[] { _objectEventHandler, _eventHandlerA, _eventHandlerB }
                .Where(x => type.IsAssignableFrom(x.GetType()));
        }

        [TearDown]
        public void TearDown()
        {
            Resolver.Reset();
        }

        [Test]
        public void GivenAMessage_WhenAskedToPublish_ShouldTryToFindHandlersForMessageTypeHierarchy()
        {
            var expected = new[]
            {
                typeof(IEventHandler<object>),
                typeof(IEventHandler<IMessage>),
                typeof(IEventHandler<Message>),
                typeof(IEventHandler<IEvent>),
                typeof(IEventHandler<Event>),
                typeof(IEventHandler<SimpleEventBase>),
                typeof(IEventHandler<SimpleEvent>),
                typeof(IEventHandler<IEventHandlingSucceededEvent<object>>),
                typeof(IEventHandler<IEventHandlingSucceededEvent<IMessage>>),
                typeof(IEventHandler<IEventHandlingSucceededEvent<Message>>),
                typeof(IEventHandler<IEventHandlingSucceededEvent<IEvent>>),
                typeof(IEventHandler<IEventHandlingSucceededEvent<Event>>),
                typeof(IEventHandler<IEventHandlingSucceededEvent<SimpleEventBase>>),
                typeof(IEventHandler<IEventHandlingSucceededEvent<SimpleEvent>>)
            };

            var result = new List<Type>();
            Resolver.Reset();
            Resolver.Configure(
                type => null,
                type =>
                {
                    result.Add(type);
                    return LocateAllEventHandlers(type);
                }, 
                o => { });

            _eventBus.Publish(new SimpleEvent());

            CollectionAssert.AreEqual(expected, result);
        }

        [Test]
        public void GivenAMessage_WhenAskedToProcess_ShouldInvokeHandlersInCorrectSequence()
        {
            var expected = new[]
            {
                typeof(object),
                typeof(SimpleEventBase),
                typeof(SimpleEvent),
            };

            _eventBus.Publish(new SimpleEvent());

            _objectEventHandler.TargetsCalled.ToList().ForEach(Console.WriteLine);

            CollectionAssert.AreEqual(expected, _objectEventHandler.TargetsCalled);
        }

        [Test]
        public void GivenAMessageHandledMultipleHandlers_WhenAskedToPublish_ShouldInvokeAllCommandHandlersInCorrectSequence()
        {
            var processor = new EventBusTestDataBuilder().Build();

            processor.Publish(new EventHandledByMultipleHandlers());

            CollectionAssert.AreEqual(new[] { typeof(object) }, _objectEventHandler.TargetsCalled);
            CollectionAssert.AreEqual(new[] { typeof(EventHandledByMultipleHandlers) }, _eventHandlerA.TargetsCalled);
            CollectionAssert.AreEqual(new[] { typeof(EventHandledByMultipleHandlers) }, _eventHandlerB.TargetsCalled);
        }

        [Test]
        public void GivenAMessageThatWillFailHandling_WhenAskedToPublish_ShouldGenerateFailedHandlingMessage()
        {
            var failingEventHandler = new FailingEventHandler();
            Resolver.Reset();
            Resolver.Configure(
                type => null,
                type => new object[] { failingEventHandler }.Where(x => type.IsAssignableFrom(x.GetType())),
                o => { });

            var eventThatWillFailToBeHandled = new SimpleEvent();
            _eventBus.Publish(eventThatWillFailToBeHandled);

            CollectionAssert.AreEqual(
                new[]
                {
                    typeof(SimpleEvent),
                    typeof(IEventHandlingFailedEvent<SimpleEvent>)
                },
                failingEventHandler.TargetsCalled);
        }

        [Test]
        public void GivenAMessageThatWillFailHandling_WhenAskedToPublish_ShouldAllowRetryableExceptionsToPropagate()
        {
            Conventions.SetRetryableEventHandlingExceptionFilter((o, e) => true);
            var failingEventHandler = new FailingEventHandler();
            Resolver.Reset();
            Resolver.Configure(
                type => null,
                type => new object[] { failingEventHandler }.Where(x => type.IsAssignableFrom(x.GetType())),
                o => { });

            var eventThatWillFailToBeHandled = new SimpleEvent();
            var exception = Assert.Throws<TargetInvocationException>(() => _eventBus.Publish(eventThatWillFailToBeHandled));

            CollectionAssert.AreEqual(
                new[]
                {
                    typeof(SimpleEvent)
                },
                failingEventHandler.TargetsCalled);
        }
    }
}
