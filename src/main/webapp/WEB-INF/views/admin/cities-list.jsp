<%@ include file="/WEB-INF/views/layout/header.jspf" %>
<h1>Thành phố</h1>
<form class="toolbar" method="get" action="${ctx}/admin/cities">
    <input name="q" value="<c:out value='${q}'/>" placeholder="Tên hoặc mã">
    <button type="submit" class="secondary">Tìm</button>
    <a class="button" href="${ctx}/admin/cities/new">Thêm thành phố</a>
</form>
<div class="table-wrap"><table>
    <tr><th>Mã</th><th>Tên</th><th>Trạng thái</th><th>Thao tác</th></tr>
    <c:forEach var="x" items="${result.items}">
        <tr>
            <td><c:out value="${x.code}"/></td>
            <td><c:out value="${x.name}"/></td>
            <td><c:out value="${x.status}"/></td>
            <td class="actions">
                <a href="${ctx}/admin/cities/edit?id=${x.id}">Sửa</a>
                <form method="post" action="${ctx}/admin/cities/deactivate"><t:csrf/><input type="hidden" name="id" value="${x.id}"><button class="secondary">Ngưng</button></form>
                <form method="post" action="${ctx}/admin/cities/delete"><t:csrf/><input type="hidden" name="id" value="${x.id}"><button class="secondary">Xóa</button></form>
            </td>
        </tr>
    </c:forEach>
</table></div>
<c:set var="base" value="${ctx}/admin/cities"/>
<%@ include file="/WEB-INF/views/layout/pager.jspf" %>
<%@ include file="/WEB-INF/views/layout/footer.jspf" %>
