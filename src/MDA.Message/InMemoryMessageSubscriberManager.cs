﻿using MDA.Common;
using MDA.Message.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MDA.Message
{
    /// <summary>
    /// 表示一个内存消息订阅器。
    /// </summary>
    public class InMemoryMessageSubscriberManager : IMessageSubscriberManager
    {
        private readonly Dictionary<string, IMessageSubscriberCollection> _subscribers;
        private readonly IMessageSubscriberCollection _subscriberCollection;

        public InMemoryMessageSubscriberManager(IMessageSubscriberCollection subscriberCollection)
        {
            _subscriberCollection = subscriberCollection;
            _subscribers = new Dictionary<string, IMessageSubscriberCollection>();
        }

        public bool IsEmpty => !_subscribers.Any();

        public void Clear()
        {
            _subscribers.Clear();
        }

        public IEnumerable<MessageSubscriberDescriptor> GetSubscribers<TMessage>()
            where TMessage : IMessage
        {
            return _subscribers[GetMessageName<TMessage>()];
        }

        public IEnumerable<MessageSubscriberDescriptor> GetSubscribers(string messageName)
        {
            Assert.NotNullOrEmpty(messageName, nameof(messageName));

            return _subscribers[messageName];
        }

        public string GetMessageName<TMessage>()
            where TMessage : IMessage
        {
            return typeof(TMessage).Name;
        }

        public bool HasSubscriber<TMessage>()
            where TMessage : IMessage
        {
            return _subscribers[GetMessageName<TMessage>()].Any();
        }

        public bool HasSubscriber(string messageName)
        {
            Assert.NotNullOrEmpty(messageName, nameof(messageName));

            return _subscribers[messageName].Any();
        }

        public void Subscribe<TMessage, TMessageHandler>()
            where TMessage : IMessage
            where TMessageHandler : IMessageHandler<TMessage>
        {
            DoAddSubscriber(typeof(TMessageHandler), typeof(TMessage), GetMessageName<TMessage>(), isDynamic: false);
        }

        public void SubscribeDynamic<TMessageHandler>(string messageName) where TMessageHandler : IDynamicMessageHandler
        {
            DoAddSubscriber(typeof(TMessageHandler), null, messageName, isDynamic: true);
        }

        public void Unsubscribe<TMessage, TMessageHandler>()
            where TMessage : IMessage
            where TMessageHandler : IMessageHandler<TMessage>
        {
            DoRemoveSubcriber(GetMessageName<TMessage>(), MessageSubscriberDescriptor.Typed(typeof(TMessage), typeof(TMessageHandler)));
        }

        public void UnsubscribeDynamic<TMessageHandler>(string messageName)
            where TMessageHandler : IDynamicMessageHandler
        {
            DoRemoveSubcriber(messageName, MessageSubscriberDescriptor.Dynamic(messageName, typeof(TMessageHandler)));
        }

        private void DoAddSubscriber(
            Type handlerType,
            Type messageType,
            string messageName,
            bool isDynamic)
        {
            Assert.NotNull(handlerType, nameof(handlerType));
            Assert.NotNullOrEmpty(messageName, nameof(messageName));

            if (!_subscribers.ContainsKey(messageName))
            {
                _subscribers.Add(messageName, _subscriberCollection.New());
            }

            if (_subscribers[messageName].Any(s => s.MessageHandlerType == handlerType))
            {
                throw new ArgumentException(
                $"Handler Type {handlerType.Name} already registered for '{messageName}'", nameof(handlerType));
            }

            if (isDynamic)
            {
                _subscribers[messageName].Add(MessageSubscriberDescriptor.Dynamic(messageName, handlerType));
            }
            else
            {
                Assert.NotNull(messageType, nameof(messageType));

                _subscribers[messageName].Add(MessageSubscriberDescriptor.Typed(messageType, handlerType));
            }
        }

        private void DoRemoveSubcriber(string messageName, MessageSubscriberDescriptor subsToRemove)
        {
            if (string.IsNullOrEmpty(messageName) ||
                subsToRemove == null) return;

            if (_subscribers[messageName].Any())
            {
                _subscribers[messageName].Remove(subsToRemove);

                if (!_subscribers[messageName].Any())
                {
                    _subscribers.Remove(messageName);
                }
            }
        }
    }
}
