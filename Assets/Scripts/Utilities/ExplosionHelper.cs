using System.Collections.Generic;

public class ExplosionHelper {
    private static List<IExplodable> Explodables = new List<IExplodable>();

    public static void Add(IExplodable explodable) {
        Explodables.Add(explodable);
    }

    public static void Remove(IExplodable explodable) {
        Explodables.Remove(explodable);
    }

    public static List<IExplodable> Get() {
        return Explodables;
    }
}