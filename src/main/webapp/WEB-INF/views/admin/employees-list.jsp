<%@ include file="/WEB-INF/views/layout/header.jspf" %>
<h1>Nhân viên</h1>
<form class="toolbar" method="get" action="${ctx}/admin/employees">
    <input name="q" value="<c:out value='${q}'/>" placeholder="Họ tên, tên đăng nhập, điện thoại">
    <button type="submit" class="secondary">Tìm</button>
    <a class="button" href="${ctx}/admin/employees/new">Thêm nhân viên</a>
</form>
<div class="table-wrap"><table>
    <tr><th>Họ tên</th><th>Tên đăng nhập</th><th>Vai trò</th><th>Cửa hàng</th><th>Điện thoại</th><th>Trạng thái</th><th>Thao tác</th></tr>
    <c:forEach var="x" items="${result.items}">
        <tr>
            <td><c:out value="${x.fullName}"/></td>
            <td><c:out value="${x.username}"/></td>
            <td><c:out value="${x.role}"/></td>
            <td><c:out value="${x.shopName}"/></td>
            <td><c:out value="${x.phone}"/></td>
            <td><c:out value="${x.status}"/></td>
            <td class="actions">
                <a href="${ctx}/admin/employees/edit?id=${x.id}">Sửa</a>
                <form method="post" action="${ctx}/admin/employees/reset-password"><t:csrf/><input type="hidden" name="id" value="${x.id}"><button class="secondary">Đặt lại mật khẩu</button></form>
                <form method="post" action="${ctx}/admin/employees/deactivate"><t:csrf/><input type="hidden" name="id" value="${x.id}"><button class="secondary">Ngưng</button></form>
                <form method="post" action="${ctx}/admin/employees/delete"><t:csrf/><input type="hidden" name="id" value="${x.id}"><button class="secondary">Xóa</button></form>
            </td>
        </tr>
    </c:forEach>
</table></div>
<c:set var="base" value="${ctx}/admin/employees"/>
<%@ include file="/WEB-INF/views/layout/pager.jspf" %>
<%@ include file="/WEB-INF/views/layout/footer.jspf" %>
