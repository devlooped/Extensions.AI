using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Weaving;

class ConsoleInitializer
{
#pragma warning disable CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
    [ModuleInitializer]
#pragma warning restore CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
    public static void Init()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Console.InputEncoding = Console.OutputEncoding = Encoding.UTF8;
    }
}
