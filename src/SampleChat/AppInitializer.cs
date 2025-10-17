using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SampleChat;

class AppInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Console.InputEncoding = Console.OutputEncoding = Encoding.UTF8;

        // Load environment variables from .env files in current dir and above.
        DotNetEnv.Env.TraversePath().Load();

        // Load environment variables from user profile directory.
        var userEnv = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".env");
        if (File.Exists(userEnv))
            DotNetEnv.Env.Load(userEnv);
    }
}
