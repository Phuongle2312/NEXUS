<%@ include file="/WEB-INF/views/layout/header.jspf" %>
<h1>${editing ? 'Sửa' : 'Thêm'} nhân viên</h1>
<form method="post" action="${ctx}/admin/employees/${editing ? 'update' : 'create'}" class="form">
    <t:csrf/>
    <c:if test="${editing}"><input type="hidden" name="id" value="${item.id}"></c:if>
    <label>Họ tên <input name="fullName" required maxlength="150" value="<c:out value='${item.fullName}'/>"></label>
    <label>Vai trò
        <select name="role" required>
            <c:forEach var="r" items="ADMIN,ACCOUNTS,TECH,RETAIL">
                <option value="${r}" ${r == item.role ? 'selected' : ''}>${r}</option>
            </c:forEach>
        </select>
    </label>
    <label>Cửa hàng (bắt buộc với RETAIL)
        <select name="shopId">
            <option value="">-- Không --</option>
            <c:forEach var="s" items="${shops}">
                <option value="${s.id}" ${s.id == item.shopId ? 'selected' : ''}><c:out value="${s.name}"/></option>
            </c:forEach>
        </select>
    </label>
    <label>Điện thoại <input name="phone" maxlength="20" value="<c:out value='${item.phone}'/>"></label>
    <label>Email <input name="email" type="email" maxlength="150" value="<c:out value='${item.email}'/>"></label>
    <c:if test="${not editing}">
        <label>Tên đăng nhập <input name="username" required maxlength="50" autocomplete="off" value="<c:out value='${item.username}'/>"></label>
        <label>Mật khẩu tạm (8–72 ký tự, có chữ và số; phải đổi ở lần đăng nhập đầu)
            <input name="password" type="password" required minlength="8" maxlength="72" autocomplete="new-password"></label>
    </c:if>
    <div><button type="submit">Lưu</button> <a class="button secondary" href="${ctx}/admin/employees">Hủy</a></div>
</form>
<%@ include file="/WEB-INF/views/layout/footer.jspf" %>
