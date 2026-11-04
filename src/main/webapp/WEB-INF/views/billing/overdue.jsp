<%@ include file="/WEB-INF/views/layout/header.jspf" %>
<h1>Hóa đơn quá hạn</h1>
<form class="toolbar" method="get" action="${ctx}/billing/overdue">
    <label>Quá hạn hơn (ngày, bỏ trống = theo cấu hình)
        <input name="days" type="number" min="0" value="<c:out value='${days}'/>"></label>
    <button type="submit" class="secondary">Lọc</button>
</form>
<div class="table-wrap"><table>
    <tr><th>Mã</th><th>Account ID</th><th>Khách</th><th>Hạn</th><th class="num">Còn nợ</th><th>Đề nghị tạm ngưng</th></tr>
    <c:forEach var="b" items="${bills}">
        <tr>
            <td><a href="${ctx}/billing/view?id=${b.id}">${b.id}</a></td>
            <td><c:out value="${b.accountId}"/></td>
            <td><c:out value="${b.customerName}"/></td>
            <td>${b.dueDate}</td>
            <td class="num">${b.dueAmount}</td>
            <td>
                <form method="post" action="${ctx}/billing/request-suspend" class="toolbar">
                    <t:csrf/>
                    <input type="hidden" name="accountId" value="<c:out value='${b.accountId}'/>">
                    <input name="reason" required maxlength="500" value="Quá hạn thanh toán hóa đơn #${b.id}">
                    <button type="submit" class="secondary">Gửi Kỹ thuật</button>
                </form>
            </td>
        </tr>
    </c:forEach>
    <c:if test="${empty bills}"><tr><td colspan="6">Không có hóa đơn quá hạn.</td></tr></c:if>
</table></div>
<%@ include file="/WEB-INF/views/layout/footer.jspf" %>
