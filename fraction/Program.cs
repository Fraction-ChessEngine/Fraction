using System;
using System.IO;

namespace fraction;
public class Program
{
    

    public static void Main(string[] args) {
        
        Config.HandleArgs(args);
        


        var engine = new Fraction();
        engine.Run();
    }
}
