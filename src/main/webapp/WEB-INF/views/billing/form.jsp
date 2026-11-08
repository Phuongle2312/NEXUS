<%@ include file="/WEB-INF/views/layout/header.jspf" %>
<h1>Lập hóa đơn</h1>
<c:choose>
    <c:when test="${empty ctx2}">
        <form method="get" action="${ctx}/billing/new" class="form narrow">
            <label>Account ID <input name="accountId" required maxlength="16" placeholder="B001000000000001"></label>
            <button type="submit">Tiếp tục</button>
        </form>
    </c:when>
    <c:otherwise>
        <p><strong><c:out value="${ctx2.accountId}"/></strong> · <c:out value="${ctx2.customerName}"/> · gói <c:out value="${ctx2.planName}"/>
            · trạng thái <c:out value="${ctx2.status}"/></p>
        <form method="post" class="form">
            <t:csrf/>
            <input type="hidden" name="accountId" value="<c:out value='${ctx2.accountId}'/>">
            <label>Từ ngày <input type="date" name="periodStart" required value="<c:out value='${param.periodStart}'/>"></label>
            <label>Đến ngày <input type="date" name="periodEnd" required value="<c:out value='${param.periodEnd}'/>"></label>
            <c:forEach var="rate" items="${rates}">
                <c:set var="field" value="minutes${rate.key}"/>
                <label>Số phút ${rate.key} (${rate.value}$/phút)
                    <input name="${field}" type="number" min="0" step="0.01" value="<c:out value='${param[field]}'/>"></label>
            </c:forEach>
            <c:if test="${not empty ctx2.hoursIncluded}">
                <label>Số giờ dùng trong kỳ (gói ${ctx2.hoursIncluded} giờ / ${ctx2.validityMonths} tháng, vượt ${ctx2.overageRatePerHour}$/giờ)
                    <input name="usageHours" type="number" min="0" step="0.01" value="<c:out value='${param.usageHours}'/>"></label>
            </c:if>
            <label>Điều chỉnh (+/−, tính vào phần chịu thuế)
                <input name="adjustment" type="number" step="0.01" value="<c:out value='${param.adjustment}'/>"></label>
            <label>Ghi chú <input name="note" maxlength="500" value="<c:out value='${param.note}'/>"></label>
            <div>
                <button type="submit" formaction="${ctx}/billing/preview" class="secondary">Tính thử</button>
                <button type="submit" formaction="${ctx}/billing/generate">Lập hóa đơn</button>
            </div>
        </form>
    </c:otherwise>
</c:choose>
<c:if test="${not empty preview}">
    <h2>Kết quả tính thử (chưa lưu)</h2>
    <c:set var="b" value="${preview}"/>
    <%@ include file="/WEB-INF/views/billing/amounts.jspf" %>
</c:if>
<%@ include file="/WEB-INF/views/layout/footer.jspf" %>
