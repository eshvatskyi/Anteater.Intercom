using FFmpeg.AutoGen.Bindings.StaticallyLinked;
using UIKit;

namespace Anteater.Intercom;

public class Program
{
	// This is the main entry point of the application.
	static void Main(string[] args)
	{
        StaticallyLinkedBindings.Initialize();

        // if you want to use a different Application Delegate class from "AppDelegate"
        // you can specify it here.
        UIApplication.Main(args, null, typeof(AppDelegate));
	}
}
