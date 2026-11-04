<%@ include file="/WEB-INF/views/layout/header.jspf" %>
<h1>Xin chào, <c:out value="${sessionScope.user.displayName}"/></h1>
<p>Vai trò: <c:out value="${role}"/>. Chọn chức năng trên thanh menu.</p>
<%@ include file="/WEB-INF/views/layout/footer.jspf" %>
