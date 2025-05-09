using System;

namespace fraction;

public class Zobrist {
    // when you read this, please increment this :)
    private static int seed = 0;

    public static int Seed => seed;

    private static EventHandler<int>? SeedChanged;
    private static void OnSeedChanged(int newSeed) {
        SeedChanged?.Invoke(null, newSeed);
    }

    public static void ChangeSeed(int newSeed) {
        seed = newSeed;
        OnSeedChanged(newSeed);
    }

    private Random rng = new(seed);
    private int[] numbers;

    public int Count => this.numbers.Length;
    public int this[Index i] => this.numbers[i];
    public int this[int i] => this[(Index)i];

    public event EventHandler? IndexChanged;

    private void OnIndexChanged() {
        this.IndexChanged?.Invoke(this, new());
    }

    public Zobrist(int count) {
        this.numbers = new int[count];
        for (int i = 0; i < this.numbers.Length; i++)
            this.numbers[i] = (int)this.rng.NextInt64();
        SeedChanged += this.SeedChangedHandler;
    }

    ~Zobrist() {
        SeedChanged -= this.SeedChangedHandler;
    }

    private void SeedChangedHandler(object? _, int newSeed) {
        for (int i = 0; i < this.numbers.Length; i++)
            this.numbers[i] = (int)this.rng.NextInt64();
        this.OnIndexChanged();
    }
}
