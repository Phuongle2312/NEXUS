<%@ include file="/WEB-INF/views/layout/header.jspf" %>
<h1>Hóa đơn #${bill.id}</h1>
<p>
    Account ID: <strong><c:out value="${bill.accountId}"/></strong> · Khách: <c:out value="${bill.customerName}"/><br>
    Kỳ cước: ${bill.periodStart} → ${bill.periodEnd} · Ngày lập: ${bill.billDate} · Hạn thanh toán: ${bill.dueDate} · Trạng thái: ${bill.status}
</p>
<c:set var="b" value="${bill}"/>
<%@ include file="/WEB-INF/views/billing/amounts.jspf" %>
<c:if test="${not empty bill.note}"><p>Ghi chú: <c:out value="${bill.note}"/></p></c:if>
<p class="noprint toolbar">
    <button type="button" class="secondary" onclick="window.print()">In hóa đơn</button>
    <c:if test="${(role == 'ACCOUNTS' or role == 'RETAIL') and bill.status != 'PAID'}">
        <a class="button" href="${ctx}/payment/new?billId=${bill.id}">Ghi nhận thanh toán</a>
    </c:if>
    <c:if test="${role == 'ACCOUNTS'}">
        <a class="button secondary" href="${ctx}/billing/edit?id=${bill.id}">Sửa</a>
    </c:if>
</p>
<%@ include file="/WEB-INF/views/layout/footer.jspf" %>
