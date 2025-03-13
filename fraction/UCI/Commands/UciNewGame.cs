namespace fraction.UCI;

public record UciNewGame() : ICommand {
    public const string arg0 = "ucinewgame";

    public static ICommand Parse(Engine engine, string[] args) {
        if (arg0 == args[0]) return new UciNewGame();
        return new Unknown(args);
    }

    public string Serialize() {
        return arg0;
    }
}
