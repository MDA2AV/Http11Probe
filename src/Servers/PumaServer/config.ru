app = proc { |env|
  if env['REQUEST_METHOD'] == 'POST'
    body = env['rack.input'].read
    [200, { 'content-type' => 'text/plain' }, [body]]
  else
    [200, { 'content-type' => 'text/plain' }, ['OK']]
  end
}
run app
