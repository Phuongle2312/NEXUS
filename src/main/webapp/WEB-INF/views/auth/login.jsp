<%@ include file="/WEB-INF/views/layout/header.jspf" %>
<h1>Đăng nhập</h1>
<form method="post" action="${ctx}/auth/login" class="form narrow">
    <t:csrf/>
    <label>Tên đăng nhập <input name="username" required autofocus autocomplete="username"></label>
    <label>Mật khẩu <input name="password" type="password" required autocomplete="current-password"></label>
    <button type="submit">Đăng nhập</button>
</form>
<%@ include file="/WEB-INF/views/layout/footer.jspf" %>
