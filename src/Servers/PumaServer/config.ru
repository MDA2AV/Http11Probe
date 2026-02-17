app = proc { |env|
  if env['PATH_INFO'] == '/echo'
    headers = env.select { |k, _| k.start_with?('HTTP_') }
    body = headers.map { |k, v| "#{k.sub('HTTP_', '').split('_').map(&:capitalize).join('-')}: #{v}" }.join("\n") + "\n"
    body += "Content-Type: #{env['CONTENT_TYPE']}\n" if env['CONTENT_TYPE']
    body += "Content-Length: #{env['CONTENT_LENGTH']}\n" if env['CONTENT_LENGTH']
    [200, { 'Content-Type' => 'text/plain' }, [body]]
  elsif env['PATH_INFO'] == '/cookie'
    body = ""
    if env['HTTP_COOKIE']
      env['HTTP_COOKIE'].split(';').each do |pair|
        trimmed = pair.lstrip
        eq = trimmed.index('=')
        if eq && eq > 0
          body += "#{trimmed[0...eq]}=#{trimmed[(eq+1)..]}\n"
        end
      end
    end
    [200, { 'Content-Type' => 'text/plain' }, [body]]
  elsif env['REQUEST_METHOD'] == 'POST'
    body = env['rack.input'].read
    [200, { 'content-type' => 'text/plain' }, [body]]
  else
    [200, { 'content-type' => 'text/plain' }, ['OK']]
  end
}
run app
