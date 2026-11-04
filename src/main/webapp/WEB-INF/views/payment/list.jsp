<%@ include file="/WEB-INF/views/layout/header.jspf" %>
<h1>Lịch sử thanh toán</h1>
<form class="toolbar" method="get" action="${ctx}/payment/list">
    <input name="accountId" value="<c:out value='${param.accountId}'/>" placeholder="Account ID">
    <input name="billId" type="number" min="1" value="<c:out value='${param.billId}'/>" placeholder="Mã hóa đơn">
    <input type="date" name="from" value="<c:out value='${param.from}'/>">
    <input type="date" name="to" value="<c:out value='${param.to}'/>">
    <button type="submit" class="secondary">Lọc</button>
</form>
<div class="table-wrap"><table>
    <tr><th>Biên lai</th><th>Hóa đơn</th><th>Account ID</th><th>Khách</th><th>Ngày</th><th>Hình thức</th><th class="num">Số tiền</th><th>Người nhận</th></tr>
    <c:forEach var="p" items="${result.items}">
        <tr>
            <td><a href="${ctx}/payment/receipt?id=${p.id}">#${p.id}</a></td>
            <td><a href="${ctx}/billing/view?id=${p.billId}">#${p.billId}</a></td>
            <td><c:out value="${p.accountId}"/></td>
            <td><c:out value="${p.customerName}"/></td>
            <td>${p.paidDate}</td>
            <td>${p.method}</td>
            <td class="num">${p.amount}</td>
            <td><c:out value="${p.receivedBy}"/></td>
        </tr>
    </c:forEach>
</table></div>
<c:set var="base" value="${ctx}/payment/list"/>
<%@ include file="/WEB-INF/views/layout/pager.jspf" %>
<%@ include file="/WEB-INF/views/layout/footer.jspf" %>
