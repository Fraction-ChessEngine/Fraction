namespace fraction;

public record SearchArgs(Position pos,
        int Depth,
        int Nodes,
        int Mate,
        params Move[] Searchmoves
        );
