<%@ include file="/WEB-INF/views/layout/header.jspf" %>
<h1>Ghi nhận thanh toán</h1>
<p>Hóa đơn #${bill.id} · <c:out value="${bill.accountId}"/> · <c:out value="${bill.customerName}"/><br>
    Tổng ${bill.totalAmount}$ · đã trả ${bill.paidAmount}$ · <strong>còn nợ ${bill.dueAmount}$</strong></p>
<form method="post" action="${ctx}/payment/create" class="form narrow">
    <t:csrf/>
    <input type="hidden" name="billId" value="${bill.id}">
    <label>Số tiền <input name="amount" type="number" min="0.01" step="0.01" max="${bill.dueAmount}" required value="<c:out value='${empty param.amount ? bill.dueAmount : param.amount}'/>"></label>
    <label>Hình thức
        <select name="method" required>
            <option value="CASH">Tiền mặt</option>
            <option value="CARD">Thẻ</option>
            <option value="TRANSFER">Chuyển khoản</option>
        </select>
    </label>
    <label>Ngày thanh toán <input name="paidDate" type="date" required value="<c:out value='${empty param.paidDate ? today : param.paidDate}'/>"></label>
    <label>Ghi chú <input name="note" maxlength="255" value="<c:out value='${param.note}'/>"></label>
    <button type="submit">Ghi nhận</button>
</form>
<%@ include file="/WEB-INF/views/layout/footer.jspf" %>
