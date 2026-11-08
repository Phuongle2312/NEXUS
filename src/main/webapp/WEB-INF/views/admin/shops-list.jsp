<%@ include file="/WEB-INF/views/layout/header.jspf" %>
<h1>Cửa hàng</h1>
<form class="toolbar" method="get" action="${ctx}/admin/shops">
    <input name="q" value="<c:out value='${q}'/>" placeholder="Tên, địa chỉ, thành phố">
    <button type="submit" class="secondary">Tìm</button>
    <a class="button" href="${ctx}/admin/shops/new">Thêm cửa hàng</a>
</form>
<div class="table-wrap"><table>
    <tr><th>Mã</th><th>Tên</th><th>Địa chỉ</th><th>Thành phố</th><th>Điện thoại</th><th>Trạng thái</th><th>Thao tác</th></tr>
    <c:forEach var="x" items="${result.items}">
        <tr>
            <td>${x.id}</td>
            <td><c:out value="${x.name}"/></td>
            <td><c:out value="${x.address}"/></td>
            <td><c:out value="${x.cityName}"/></td>
            <td><c:out value="${x.phone}"/></td>
            <td><c:out value="${x.status}"/></td>
            <td class="actions">
                <a href="${ctx}/admin/shops/edit?id=${x.id}">Sửa</a>
                <form method="post" action="${ctx}/admin/shops/deactivate"><t:csrf/><input type="hidden" name="id" value="${x.id}"><button class="secondary">Ngưng</button></form>
                <form method="post" action="${ctx}/admin/shops/delete"><t:csrf/><input type="hidden" name="id" value="${x.id}"><button class="secondary">Xóa</button></form>
            </td>
        </tr>
    </c:forEach>
</table></div>
<c:set var="base" value="${ctx}/admin/shops"/>
<%@ include file="/WEB-INF/views/layout/pager.jspf" %>
<%@ include file="/WEB-INF/views/layout/footer.jspf" %>
