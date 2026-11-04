<%@ tag pageEncoding="UTF-8" trimDirectiveWhitespaces="true" %>
<%-- Dat <t:csrf/> trong moi form POST. CsrfFilter tu choi POST khong co token nay. --%>
<input type="hidden" name="_csrf" value="${sessionScope.csrfToken}">
