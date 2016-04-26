public class Tuple3<T1, T2, T3>
{
	public T1 First { get; private set; }
	public T2 Second { get; private set; }
	public T3 Third { get; private set; }
	internal Tuple3(T1 first, T2 second, T3 third)
	{
		First = first;
		Second = second;
		Third = third;
	}
}

public static class Tuple3
{
	public static Tuple3<T1, T2, T3> New<T1, T2, T3>(T1 first, T2 second, T3 third)
	{
		return new Tuple3<T1, T2, T3>(first, second, third);
	}
}