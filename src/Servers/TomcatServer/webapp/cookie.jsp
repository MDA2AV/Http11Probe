<%@page contentType="text/plain"%><%
jakarta.servlet.http.Cookie[] cookies = request.getCookies();
if (cookies != null) {
    for (jakarta.servlet.http.Cookie c : cookies) {
        out.print(c.getName() + "=" + c.getValue() + "\n");
    }
}
%>