<%@ include file="/WEB-INF/views/layout/header.jspf" %>
<h1>Hóa đơn</h1>
<form class="toolbar" method="get" action="${ctx}/billing/list">
    <input name="accountId" value="<c:out value='${param.accountId}'/>" placeholder="Account ID">
    <select name="status">
        <option value="">Mọi trạng thái</option>
        <c:forEach var="s" items="UNPAID,PARTIAL,PAID">
            <option value="${s}" ${param.status == s ? 'selected' : ''}>${s}</option>
        </c:forEach>
    </select>
    <input type="date" name="from" value="<c:out value='${param.from}'/>">
    <input type="date" name="to" value="<c:out value='${param.to}'/>">
    <button type="submit" class="secondary">Lọc</button>
</form>
<c:if test="${role == 'ACCOUNTS' and not empty param.accountId}">
    <form method="post" action="${ctx}/payment/refund-deposit" class="toolbar">
        <t:csrf/>
        <input type="hidden" name="accountId" value="<c:out value='${param.accountId}'/>">
        <button type="submit" class="secondary">Hoàn cọc kết nối này</button>
    </form>
</c:if>
<div class="table-wrap"><table>
    <tr><th>Mã</th><th>Account ID</th><th>Khách</th><th>Kỳ cước</th><th class="num">Tổng</th><th class="num">Đã trả</th><th class="num">Còn nợ</th><th>Hạn</th><th>Trạng thái</th><th>Thao tác</th></tr>
    <c:forEach var="b" items="${result.items}">
        <tr>
            <td>${b.id}</td>
            <td><c:out value="${b.accountId}"/></td>
            <td><c:out value="${b.customerName}"/></td>
            <td>${b.periodStart} → ${b.periodEnd}</td>
            <td class="num">${b.totalAmount}</td>
            <td class="num">${b.paidAmount}</td>
            <td class="num">${b.dueAmount}</td>
            <td>${b.dueDate}</td>
            <td>${b.status}</td>
            <td class="actions">
                <a href="${ctx}/billing/view?id=${b.id}">Xem</a>
                <c:if test="${(role == 'ACCOUNTS' or role == 'RETAIL') and b.status != 'PAID'}">
                    <a href="${ctx}/payment/new?billId=${b.id}">Ghi thanh toán</a>
                </c:if>
            </td>
        </tr>
    </c:forEach>
</table></div>
<c:set var="base" value="${ctx}/billing/list"/>
<%@ include file="/WEB-INF/views/layout/pager.jspf" %>
<%@ include file="/WEB-INF/views/layout/footer.jspf" %>
