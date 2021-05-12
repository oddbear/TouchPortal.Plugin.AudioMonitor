namespace TouchPortal.Plugin.AudioMonitor
{
    class Program
    {
        static void Main(string[] args)
        {
            var plugin = new AudioMonitorPlugin();
            plugin.Run();
        }
    }
}
