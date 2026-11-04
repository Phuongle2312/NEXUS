<%@ include file="/WEB-INF/views/layout/header.jspf" %>
<h1>Sửa hóa đơn #${bill.id}</h1>
<p><c:out value="${bill.accountId}"/> · kỳ ${bill.periodStart} → ${bill.periodEnd}. Chỉ sửa được hóa đơn mới nhất và chưa có thanh toán.</p>
<form method="post" action="${ctx}/billing/update" class="form">
    <t:csrf/>
    <input type="hidden" name="id" value="${bill.id}">
    <c:forEach var="rate" items="${rates}">
        <label>Số phút ${rate.key} (${rate.value}$/phút)
            <input name="minutes${rate.key}" type="number" min="0" step="0.01" value="${minutes[rate.key]}"></label>
    </c:forEach>
    <c:if test="${not empty ctx2.hoursIncluded}">
        <label>Số giờ dùng trong kỳ <input name="usageHours" type="number" min="0" step="0.01" value="${bill.usageHours}"></label>
    </c:if>
    <label>Điều chỉnh (+/−) <input name="adjustment" type="number" step="0.01" value="${bill.adjustment}"></label>
    <label>Ghi chú <input name="note" maxlength="500" value="<c:out value='${bill.note}'/>"></label>
    <div><button type="submit">Lưu</button> <a class="button secondary" href="${ctx}/billing/view?id=${bill.id}">Hủy</a></div>
</form>
<%@ include file="/WEB-INF/views/layout/footer.jspf" %>
