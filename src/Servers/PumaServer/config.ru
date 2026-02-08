app = proc { |_env| [200, { 'content-type' => 'text/plain' }, ['OK']] }
run app
