using System.Collections;
using System.Collections.Generic;

public class PubSub {

	static Dictionary<string, List<IPubSub>> subscriptions = new Dictionary<string, List<IPubSub>> (); 

	public static void subscribe (string message, IPubSub subscriber) {
		if (!subscriptions.ContainsKey (message)) {
			subscriptions.Add (message, new List<IPubSub>());
		}
		List<IPubSub> messageEntry = subscriptions[message];
		if (!messageEntry.Contains (subscriber)) {
			messageEntry.Add (subscriber);
		}
	}

	public static void unsubscribe (string message, IPubSub subscriber) {
		if (subscriptions.ContainsKey (message)) {
			List<IPubSub> messageEntry = subscriptions[message];
			if (messageEntry.Contains (subscriber)) {
				messageEntry.Remove (subscriber);
				if (messageEntry.Count == 0) {
					subscriptions.Remove (message);
				}
			}
		}
	}

	public static void unsubscribeAllForSubscriber (IPubSub subscriber) {
		foreach (KeyValuePair<string, List<IPubSub>> messageEntries in subscriptions) {
			foreach (IPubSub subscribeObj in messageEntries.Value) {
				if (subscribeObj == subscriber) {
					unsubscribe (messageEntries.Key, subscriber);
					break;
				}
			}
		}
	}

	public static void publish (string message, object data = null) {
		if (subscriptions.ContainsKey (message)) {
			foreach (IPubSub subscriber in subscriptions[message]) {
				subscriber.onMessage (message, data);
			}
		}
	}
}
