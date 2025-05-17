using ConsoleAppFramework;
using Serilog;
using SGPackageReader;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();
        
var app = ConsoleApp.Create();
app.Add("print-manifest", CLI.PrintManifestInfo);
app.Add("dump-assets", CLI.DumpAssets);
app.Run(args);
return 0;