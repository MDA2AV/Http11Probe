core.register_service("echo", "http", function(applet)
    local body = ""
    local hdrs = applet.headers
    for name, values in pairs(hdrs) do
        for _, v in ipairs(values) do
            body = body .. name .. ": " .. v .. "\n"
        end
    end
    applet:set_status(200)
    applet:add_header("Content-Type", "text/plain")
    applet:add_header("Content-Length", tostring(#body))
    applet:start_response()
    applet:send(body)
end)

core.register_service("echo_body", "http", function(applet)
    local body = applet:receive()
    if body == nil then body = "" end
    applet:set_status(200)
    applet:add_header("Content-Type", "text/plain")
    applet:add_header("Content-Length", tostring(#body))
    applet:start_response()
    applet:send(body)
end)
