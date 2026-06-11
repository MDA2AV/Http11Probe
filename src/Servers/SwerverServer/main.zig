// Http11Probe target for swerver. Mirrors the reference servers' endpoint
// contract (see NginxServer/echo.js): GET / -> "OK", POST / -> echo body,
// /echo -> request headers dumped, /cookie -> parsed cookies. A static
// docroot backs the conditional/range probe tests.
const std = @import("std");
const swerver = @import("swerver");
const router = swerver.router;
const response_mod = swerver.response;

fn handleRoot(ctx: *router.HandlerContext) response_mod.Response {
    if (ctx.request.method == .POST) {
        const body = ctx.request.body.sliceOrNull() orelse "";
        return .{ .status = 200, .headers = &[_]response_mod.Header{
            .{ .name = "Content-Type", .value = "text/plain" },
        }, .body = .{ .bytes = body } };
    }
    return .{ .status = 200, .headers = &[_]response_mod.Header{
        .{ .name = "Content-Type", .value = "text/plain" },
    }, .body = .{ .bytes = "OK" } };
}

fn handleEcho(ctx: *router.HandlerContext) response_mod.Response {
    var off: usize = 0;
    const buf = ctx.response_buf;
    for (ctx.request.headers) |h| {
        const line = std.fmt.bufPrint(buf[off..], "{s}: {s}\n", .{ h.name, h.value }) catch break;
        off += line.len;
    }
    return .{ .status = 200, .headers = &[_]response_mod.Header{
        .{ .name = "Content-Type", .value = "text/plain" },
    }, .body = .{ .bytes = buf[0..off] } };
}

fn handleCookie(ctx: *router.HandlerContext) response_mod.Response {
    var off: usize = 0;
    const buf = ctx.response_buf;
    if (ctx.request.getHeader("cookie")) |raw| {
        var it = std.mem.splitScalar(u8, raw, ';');
        while (it.next()) |pair| {
            const trimmed = std.mem.trim(u8, pair, " \t");
            if (std.mem.indexOfScalar(u8, trimmed, '=')) |eq| {
                if (eq > 0) {
                    const line = std.fmt.bufPrint(buf[off..], "{s}={s}\n", .{ trimmed[0..eq], trimmed[eq + 1 ..] }) catch break;
                    off += line.len;
                }
            }
        }
    }
    return .{ .status = 200, .headers = &[_]response_mod.Header{
        .{ .name = "Content-Type", .value = "text/plain" },
    }, .body = .{ .bytes = buf[0..off] } };
}

pub fn main(init: std.process.Init) !void {
    const allocator = init.gpa;
    var loaded: ?swerver.config_file.LoadedConfig = null;
    defer if (loaded) |*lc| lc.deinit();
    var args = try std.process.Args.Iterator.initAllocator(init.minimal.args, allocator);
    defer args.deinit();
    _ = args.next();
    var config_path: ?[]const u8 = null;
    while (args.next()) |a| {
        const arg = std.mem.sliceTo(a, 0);
        if (std.mem.eql(u8, arg, "--config")) {
            if (args.next()) |v| config_path = std.mem.sliceTo(v, 0);
        }
    }
    var cfg: swerver.config.ServerConfig = blk: {
        if (config_path) |p| {
            loaded = try swerver.config_file.loadConfigFile(allocator, p);
            break :blk loaded.?.server_config;
        }
        break :blk swerver.config.ServerConfig.default();
    };
    try cfg.validate();

    var app = router.Router.init(.{});
    try app.get("/", handleRoot);
    try app.post("/", handleRoot);
    try app.get("/echo", handleEcho);
    try app.post("/echo", handleEcho);
    try app.get("/cookie", handleCookie);

    const srv = try swerver.ServerBuilder.config(cfg).router(app).disablePreencoded().build(allocator);
    defer { srv.deinit(); allocator.destroy(srv); }
    try srv.run(null);
}
