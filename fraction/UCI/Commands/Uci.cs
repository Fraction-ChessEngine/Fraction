namespace fraction.UCI;

public record Uci() : ICommand {
    public const string arg0 = "uci";
    public static ICommand Parse(Engine engine, string[] args) {
        if (arg0 == args[0]) return new Uci();
        return Unknown.Parse(args);
    }

    public string Serialize() {
        return arg0;
    }
}
