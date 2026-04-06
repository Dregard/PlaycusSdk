using System;
using System.Collections.Generic;

namespace PlaycusDL {

    using JSONObject = Dictionary<string, object>;

    /// <summary>
    /// Handlers which can be registered on <see cref="EventAction"/>s for
    /// handling actions of different types.
    /// </summary>
    public abstract class EventActionHandler {

        internal abstract bool Handle(EventTrigger trigger, ActionStore store);
        internal abstract string Type();
    }

    /// <summary>
    /// <see cref="EventActionHandler"/> for handling game parameters, which
    /// will be returned as a <see cref="JSONObject"/>.
    /// </summary>
    public class GameParametersHandler : EventActionHandler {

        private readonly Action<JSONObject> callback;

        public GameParametersHandler(Action<JSONObject> callback) {
            this.callback = callback;
        }

        internal override bool Handle(EventTrigger trigger, ActionStore store) {
            if (trigger.GetAction() == Type()) {
                var response = trigger.GetResponse();
                var persistedParams = store.Get(trigger);

                if (persistedParams != null) {
                    store.Remove(trigger);
                    callback(persistedParams);
                } else if (response.ContainsKey("parameters")) {
                    callback((JSONObject) response["parameters"]);
                } else {
                    callback(new JSONObject());
                }

                return true;
            }

            return false;
        }

        internal override string Type() {
            return "gameParameters";
        }
    }

}
