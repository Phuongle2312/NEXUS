<%@ include file="/WEB-INF/views/layout/header.jspf" %>
<h1>${editing ? 'Sửa' : 'Thêm'} thành phố</h1>
<form method="post" action="${ctx}/admin/cities/${editing ? 'update' : 'create'}" class="form">
    <t:csrf/>
    <c:if test="${editing}"><input type="hidden" name="id" value="${item.id}"></c:if>
    <label>Tên thành phố <input name="name" required maxlength="100" value="<c:out value='${item.name}'/>"></label>
    <label>Mã 3 chữ số <input name="code" required pattern="[0-9]{3}" maxlength="3" value="<c:out value='${item.code}'/>"></label>
    <div><button type="submit">Lưu</button> <a class="button secondary" href="${ctx}/admin/cities">Hủy</a></div>
</form>
<%@ include file="/WEB-INF/views/layout/footer.jspf" %>
