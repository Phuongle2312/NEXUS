<%@ include file="/WEB-INF/views/layout/header.jspf" %>
<h1>Biên lai #${payment.id}</h1>
<div class="table-wrap"><table>
    <tr><th>Khách hàng</th><td><c:out value="${payment.customerName}"/></td></tr>
    <tr><th>Account ID</th><td><c:out value="${payment.accountId}"/></td></tr>
    <tr><th>Hóa đơn</th><td>#${payment.billId}</td></tr>
    <tr><th>Ngày thanh toán</th><td>${payment.paidDate}</td></tr>
    <tr><th>Hình thức</th><td>${payment.method}</td></tr>
    <tr><th>Số tiền</th><td>${payment.amount}$</td></tr>
    <tr><th>Người nhận</th><td><c:out value="${payment.receivedBy}"/></td></tr>
    <c:if test="${not empty payment.note}"><tr><th>Ghi chú</th><td><c:out value="${payment.note}"/></td></tr></c:if>
</table></div>
<p class="noprint toolbar">
    <button type="button" class="secondary" onclick="window.print()">In biên lai</button>
    <a class="button secondary" href="${ctx}/billing/view?id=${payment.billId}">Xem hóa đơn</a>
</p>
<%@ include file="/WEB-INF/views/layout/footer.jspf" %>
