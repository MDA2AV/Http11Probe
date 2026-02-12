using GenHTTP.Api.Content;
using GenHTTP.Api.Protocol;
using GenHTTP.Engine.Internal;
using GenHTTP.Modules.Functional;
using GenHTTP.Modules.Practices;

var port = args.Length > 0 && int.TryParse(args[0], out var p) ? p : 8080;

var handler = Inline.Create()
    .Get(async (IRequest request) =>
    {
        await ValueTask.CompletedTask;
        return "OK";
    })
    .Post(async (IRequest request) =>
    {
        if (request.Content is not null)
        {
            using var reader = new StreamReader(request.Content);
            return await reader.ReadToEndAsync();
        }
        return "";
    });

await Host.Create()
    .Handler(handler)
    .Defaults()
    .Port((ushort)port)
    .RunAsync();
