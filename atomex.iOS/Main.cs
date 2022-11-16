using UIKit;

namespace atomex.iOS
{
    public class Application
    {
        // This is the main entry point of the application.
        static void Main(string[] args)
        {
            ObjCRuntime.Dlfcn.dlopen ("libsodium.dylib", 0);
            
            // if you want to use a different Application Delegate class from "AppDelegate"
            // you can specify it here.
            UIApplication.Main(args, null, "AppDelegate");
        }
    }
}
