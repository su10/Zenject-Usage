public class KlassWithId
{
    private static int Counter = 0;

    public static void ResetIdCounter()
    {
        Counter = 0;
    }

    public int id { get; private set; }

    public KlassWithId()
    {
        this.id = Counter++;
    }
}