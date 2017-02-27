using System;
using System.Collections.Generic;
using Inforigami.Regalo.Messaging;

namespace Inforigami.Regalo.Core.Tests.Unit
{
    public class CommandHandlerB : ICommandHandler<CommandHandledByMultipleHandlers>
    {
        public readonly IList<Type> Messages = new List<Type>();

        public void Handle(CommandHandledByMultipleHandlers command)
        {
            Messages.Add(typeof(CommandHandledByMultipleHandlers));
        }
    }
}