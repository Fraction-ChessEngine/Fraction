using System.Collections;

namespace fraction.Tests;

public class Ethereal : IEnumerable<TheoryDataRow<string, int, ulong>> {
    private const string fileName = "ethereal.txt";
    public IEnumerator<TheoryDataRow<string, int, ulong>> GetData() {
        string[] lines = File.ReadAllLines(fileName);


        var list = new List<TheoryDataRow<string, int, ulong>>();

        foreach (var line in lines) {
            var data = line.Split(';');
            var fen = data[0];
            foreach (var pair in data[1..]) {
                var split = pair.Split(' ');
                var depth = int.Parse(split[0][1..]);
                var sum = ulong.Parse(split[1]);
                list.Add(new(fen, depth, sum));
            }
        }

        list.Sort(static (x, y) => x.Data.Item2 - y.Data.Item2);
        return list.GetEnumerator();
    }

    public IEnumerator<TheoryDataRow<string, int, ulong>> GetEnumerator() {
        return GetData();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }
}
