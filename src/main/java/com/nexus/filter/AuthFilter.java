package com.nexus.filter;

import com.nexus.AppContext;
import com.nexus.model.SessionUser;
import com.nexus.service.AuthService;
import jakarta.servlet.Filter;
import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.ServletRequest;
import jakarta.servlet.ServletResponse;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import jakarta.servlet.http.HttpSession;

import java.io.IOException;

/**
 * Chặn mọi URL trừ nhóm công khai (API Design 1.3).
 * Chưa đăng nhập: trang thường chuyển về login, /api/* trả 401 JSON.
 * Tài khoản đang dùng mật khẩu tạm chỉ được đổi mật khẩu hoặc đăng xuất (FR-AUTH-06).
 */
public class AuthFilter implements Filter {
    private final AuthService authService = AppContext.authService();

    @Override
    public void doFilter(ServletRequest req, ServletResponse res, FilterChain chain)
            throws IOException, ServletException {
        HttpServletRequest request = (HttpServletRequest) req;
        HttpServletResponse response = (HttpServletResponse) res;
        String path = FilterPaths.path(request);

        if (AccessPolicy.isPublic(path)) {
            chain.doFilter(req, res);
            return;
        }

        HttpSession session = request.getSession(false);
        SessionUser user = session == null ? null : (SessionUser) session.getAttribute(SessionUser.SESSION_KEY);

        // Tài khoản bị khóa hoặc đổi vai trò sau khi đăng nhập thì phiên cũ hết hiệu lực ngay.
        if (user != null && !authService.isSessionStillValid(user)) {
            session.invalidate();
            user = null;
        }

        if (user == null) {
            if (path.startsWith("/api/")) {
                FilterPaths.jsonError(response, HttpServletResponse.SC_UNAUTHORIZED, "AUTH_INVALID", "Chưa đăng nhập");
            } else {
                response.sendRedirect(request.getContextPath() + "/auth/login");
            }
            return;
        }

        if (user.isMustChangePassword() && !path.equals("/auth/change-password") && !path.equals("/auth/logout")) {
            response.sendRedirect(request.getContextPath() + "/auth/change-password");
            return;
        }

        chain.doFilter(req, res);
    }
}
