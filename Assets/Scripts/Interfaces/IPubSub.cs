using System.Collections;

public interface IPubSub {

	void onMessage (string message, object data);

}
