<%@page contentType="text/plain" import="java.io.*"%><%
if ("POST".equals(request.getMethod())) {
    InputStream in = request.getInputStream();
    byte[] buf = in.readAllBytes();
    out.print(new String(buf, "UTF-8"));
} else {
    out.print("OK");
}
%>