package probe;

import org.redkale.net.http.RestMapping;
import org.redkale.net.http.RestService;
import org.redkale.service.Service;

@RestService(autoMapping = true)
public class OkService implements Service {

    @RestMapping(name = "**")
    public String handle() {
        return "OK";
    }
}
