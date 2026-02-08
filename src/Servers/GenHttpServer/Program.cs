using GenHTTP.Engine.Internal;
using GenHTTP.Modules.IO;
using GenHTTP.Modules.Practices;

var port = args.Length > 0 && int.TryParse(args[0], out var p) ? p : 8080;

var content = Content.From(Resource.FromString("OK"));

await Host.Create()
    .Handler(content)
    .Defaults()
    .Port((ushort)port)
    .RunAsync();
