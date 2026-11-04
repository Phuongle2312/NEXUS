<%@ page isErrorPage="true" %>
<%@ include file="/WEB-INF/views/layout/header.jspf" %>
<%-- Chi hien thong bao than thien, khong hien stack trace. --%>
<c:set var="status" value="${requestScope['jakarta.servlet.error.status_code']}"/>
<h1>Có lỗi xảy ra</h1>
<c:choose>
    <c:when test="${not empty errorCode}"><p class="muted"><c:out value="${errorCode}"/></p></c:when>
    <c:when test="${status == 403}"><p class="error">Bạn không có quyền thực hiện thao tác này, hoặc phiên làm việc đã hết hạn.</p></c:when>
    <c:when test="${status == 404}"><p class="error">Không tìm thấy trang.</p></c:when>
    <c:otherwise><p class="error">Lỗi hệ thống, vui lòng thử lại sau.</p></c:otherwise>
</c:choose>
<p><a href="${ctx}/dashboard">Về trang chủ</a></p>
<%@ include file="/WEB-INF/views/layout/footer.jspf" %>
