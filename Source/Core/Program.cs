namespace Mario2
{
    public static class Program
    {
        private static void Main(string[] args)
        {
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
            Game.Instance.Run();
        }
    }
}
