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

core.register_service("cookie", "http", function(applet)
    local body = ""
    local hdrs = applet.headers
    if hdrs["cookie"] then
        for _, raw in ipairs(hdrs["cookie"]) do
            for pair in raw:gmatch("[^;]+") do
                local trimmed = pair:match("^%s*(.*)")
                local eq = trimmed:find("=")
                if eq and eq > 1 then
                    body = body .. trimmed:sub(1, eq-1) .. "=" .. trimmed:sub(eq+1) .. "\n"
                end
            end
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
