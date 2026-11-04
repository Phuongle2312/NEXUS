package com.nexus.filter;

import jakarta.servlet.Filter;
import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.ServletRequest;
import jakarta.servlet.ServletResponse;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import jakarta.servlet.http.HttpSession;

import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.security.SecureRandom;
import java.util.Base64;

/**
 * Chống CSRF: mỗi phiên có một token, mọi POST phải gửi lại token này qua tham số
 * "_csrf" (trong JSP dùng thẻ &lt;t:csrf/&gt;) hoặc header "X-CSRF-Token" (JSON).
 *
 * Form multipart cũng đọc được token nhờ allowCasualMultipartParsing trong
 * META-INF/context.xml.
 */
public class CsrfFilter implements Filter {
    public static final String TOKEN_ATTR = "csrfToken";
    private static final SecureRandom RANDOM = new SecureRandom();

    @Override
    public void doFilter(ServletRequest req, ServletResponse res, FilterChain chain)
            throws IOException, ServletException {
        HttpServletRequest request = (HttpServletRequest) req;
        HttpServletResponse response = (HttpServletResponse) res;

        if ("POST".equals(request.getMethod())) {
            HttpSession session = request.getSession(false);
            String expected = session == null ? null : (String) session.getAttribute(TOKEN_ATTR);
            String actual = request.getHeader("X-CSRF-Token");
            if (actual == null) {
                actual = request.getParameter("_csrf");
            }
            if (expected == null || actual == null || !constantTimeEquals(expected, actual)) {
                // Thường do phiên đã hết hạn khi form còn mở, nên báo lỗi rõ ràng là 403.
                response.sendError(HttpServletResponse.SC_FORBIDDEN, "Phiên làm việc không hợp lệ, vui lòng tải lại trang.");
                return;
            }
        } else {
            // Phát token cho các trang có form, kể cả trang đăng nhập chưa có người dùng.
            HttpSession session = request.getSession(true);
            if (session.getAttribute(TOKEN_ATTR) == null) {
                session.setAttribute(TOKEN_ATTR, newToken());
            }
        }
        chain.doFilter(req, res);
    }

    /** Sinh token mới; AuthServlet gọi khi đăng nhập để token cũ trước đăng nhập không dùng lại được. */
    public static String newToken() {
        byte[] bytes = new byte[32];
        RANDOM.nextBytes(bytes);
        return Base64.getUrlEncoder().withoutPadding().encodeToString(bytes);
    }

    private static boolean constantTimeEquals(String a, String b) {
        return MessageDigest.isEqual(a.getBytes(StandardCharsets.UTF_8), b.getBytes(StandardCharsets.UTF_8));
    }
}
