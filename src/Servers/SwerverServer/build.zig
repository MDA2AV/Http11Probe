const std = @import("std");
pub fn build(b: *std.Build) void {
    const target = b.standardTargetOptions(.{});
    const optimize: std.builtin.OptimizeMode = .ReleaseFast;
    const dep = b.dependency("swerver", .{ .target = target, .optimize = optimize,
        .@"enable-tls" = true, .@"enable-http2" = true, .@"enable-http3" = true });
    const m = b.createModule(.{ .root_source_file = b.path("main.zig"),
        .target = target, .optimize = optimize, .link_libc = true });
    m.addImport("swerver", dep.module("swerver"));
    const exe = b.addExecutable(.{ .name = "swerver-probe", .root_module = m });
    b.installArtifact(exe);
}
