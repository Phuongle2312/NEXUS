<%@ include file="/WEB-INF/views/layout/header.jspf" %>
<h1>Đổi mật khẩu</h1>
<c:if test="${sessionScope.user.mustChangePassword}">
    <p class="flash">Bạn đang dùng mật khẩu tạm, cần đổi mật khẩu trước khi tiếp tục.</p>
</c:if>
<form method="post" action="${ctx}/auth/change-password" class="form narrow">
    <t:csrf/>
    <label>Mật khẩu hiện tại <input name="oldPassword" type="password" required autocomplete="current-password"></label>
    <label>Mật khẩu mới (8–72 ký tự, có chữ và số)
        <input name="newPassword" type="password" required minlength="8" maxlength="72" autocomplete="new-password"></label>
    <button type="submit">Đổi mật khẩu</button>
</form>
<%@ include file="/WEB-INF/views/layout/footer.jspf" %>
