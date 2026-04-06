using System;
using System.Collections.Generic;

namespace PlaycusDL {

    /// <summary>
    /// Base class for GameEvent so call to AddParam can be chained.
    /// </summary>
    public class GameEvent<T> where T : GameEvent<T> {
    
        internal readonly Params parameters;

        public GameEvent(string name) 
        {
            if (String.IsNullOrEmpty(name)) {
                throw new ArgumentException("Name cannot be null or empty");
            }

            this.Name = name;
            this.parameters = new Params();
        }

        public string Name { get; private set; }

        /// <summary>
        /// Adds an event parameter to the event.
        /// </summary>
        public T AddParam(string key, object value)
        {
            this.parameters.AddParam(key, value);
            return (T) this;
        }

        public Dictionary<string, object> AsDictionary()
        {
            return new Dictionary<string, object>() {
                { "eventName", Name },
                { "eventParams", new Dictionary<string, object>(parameters.AsDictionary()) }
            };
        }
    }

    /// <summary>
    /// Creates a GameEvent for sending an event to Collect.  If you want to extend the behaviour
    /// use <see cref="GameEvent{T}"/> so chaining works correctly. <seealso cref="DeltaDNA.Transaction"/>
    /// </summary>
    public class GameEventPDL : GameEvent<GameEventPDL> {

        public GameEventPDL(string name) : base(name) {}
    }
} // namespace PlaycusDL
