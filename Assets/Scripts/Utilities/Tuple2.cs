public class Tuple2<T1, T2> {
	public T1 First { get; private set; }
	public T2 Second { get; private set; }
	internal Tuple2(T1 first, T2 second) {
		First = first;
		Second = second;
	}
}

public static class Tuple2 {
	public static Tuple2<T1, T2> New<T1, T2>(T1 first, T2 second) {
		return new Tuple2<T1, T2>(first, second);
	}
}