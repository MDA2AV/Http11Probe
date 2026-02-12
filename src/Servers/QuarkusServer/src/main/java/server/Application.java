package server;

import java.io.InputStream;
import java.io.IOException;

import jakarta.ws.rs.GET;
import jakarta.ws.rs.POST;
import jakarta.ws.rs.Path;
import jakarta.ws.rs.Produces;
import jakarta.ws.rs.core.MediaType;

@Path("/")
public class Application {

    @GET
    @Path("{path:.*}")
    @Produces(MediaType.TEXT_PLAIN)
    public String catchAll() {
        return "OK";
    }

    @POST
    @Path("{path:.*}")
    @Produces(MediaType.TEXT_PLAIN)
    public byte[] catchAllPost(InputStream body) throws IOException {
        return body.readAllBytes();
    }
}
