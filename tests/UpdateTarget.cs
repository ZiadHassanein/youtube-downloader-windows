using System;
using System.IO;
internal static class UpdateTarget {
    public static void Main() { File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"restarted.txt"),"updated"); }
}
