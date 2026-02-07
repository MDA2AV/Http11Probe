package server;

import java.nio.ByteBuffer;
import java.nio.charset.StandardCharsets;

import org.eclipse.jetty.server.Server;
import org.eclipse.jetty.server.ServerConnector;
import org.eclipse.jetty.server.Request;
import org.eclipse.jetty.server.Response;
import org.eclipse.jetty.server.Handler;
import org.eclipse.jetty.util.Callback;

public class Application extends Handler.Abstract {

    private static final ByteBuffer OK_BODY =
            ByteBuffer.wrap("OK".getBytes(StandardCharsets.UTF_8)).asReadOnlyBuffer();

    @Override
    public boolean handle(Request request, Response response, Callback callback) {
        response.setStatus(200);
        response.getHeaders().put("Content-Type", "text/plain");
        response.write(true, OK_BODY.slice(), callback);
        return true;
    }

    public static void main(String[] args) throws Exception {
        int port = args.length > 0 ? Integer.parseInt(args[0]) : 9007;

        Server server = new Server();
        ServerConnector connector = new ServerConnector(server);
        connector.setHost("127.0.0.1");
        connector.setPort(port);
        server.addConnector(connector);
        server.setHandler(new Application());
        server.start();
        server.join();
    }
}
