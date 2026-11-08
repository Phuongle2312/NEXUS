<%@ include file="/WEB-INF/views/layout/header.jspf" %>
<h1>${editing ? 'Sửa' : 'Thêm'} cửa hàng</h1>
<form method="post" action="${ctx}/admin/shops/${editing ? 'update' : 'create'}" class="form">
    <t:csrf/>
    <c:if test="${editing}"><input type="hidden" name="id" value="${item.id}"></c:if>
    <label>Tên cửa hàng <input name="name" required maxlength="150" value="<c:out value='${item.name}'/>"></label>
    <label>Địa chỉ <input name="address" required maxlength="255" value="<c:out value='${item.address}'/>"></label>
    <label>Thành phố
        <select name="cityId" required>
            <option value="">-- Chọn --</option>
            <c:forEach var="city" items="${cities}">
                <option value="${city.id}" ${city.id == item.cityId ? 'selected' : ''}><c:out value="${city.name}"/></option>
            </c:forEach>
        </select>
    </label>
    <label>Điện thoại <input name="phone" maxlength="20" value="<c:out value='${item.phone}'/>"></label>
    <div><button type="submit">Lưu</button> <a class="button secondary" href="${ctx}/admin/shops">Hủy</a></div>
</form>
<%@ include file="/WEB-INF/views/layout/footer.jspf" %>
