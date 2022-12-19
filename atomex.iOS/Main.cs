using Beacon.Sdk.Core.Infrastructure.Cryptography.Libsodium;
using UIKit;

namespace atomex.iOS
{
    public class Application
    {
        // This is the main entry point of the application.
        static void Main(string[] args)
        {
            Sodium.SetLibraryType(SodiumLibraryType.StaticInternal);
            // if you want to use a different Application Delegate class from "AppDelegate"
            // you can specify it here.
            UIApplication.Main(args, null, "AppDelegate");
        }
    }
}
