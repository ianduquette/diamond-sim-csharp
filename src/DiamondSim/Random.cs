namespace DiamondSim;

public interface IRandomSource {
    double NextDouble(); // [0,1)
}

public sealed class SeededRandom : IRandomSource {
    private readonly Random _rng;

    public SeededRandom(int seed) {
        _rng = new Random(seed);
    }

    public double NextDouble() {
        return _rng.NextDouble();
    }
}
