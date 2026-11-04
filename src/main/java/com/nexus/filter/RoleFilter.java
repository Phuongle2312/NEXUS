package com.nexus.filter;

import com.nexus.model.SessionUser;
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
 * Kiểm tra vai trò theo {@link AccessPolicy}, chạy sau AuthFilter. Sai quyền trả 403.
 * Quyền sở hữu dữ liệu của khách (chỉ xem hóa đơn của mình...) kiểm tra thêm ở tầng Service.
 */
public class RoleFilter implements Filter {
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

        // AuthFilter đã bảo đảm có user trong session.
        HttpSession session = request.getSession(false);
        SessionUser user = (SessionUser) session.getAttribute(SessionUser.SESSION_KEY);
        if (AccessPolicy.isAllowed(request.getMethod(), path, user.getRole())) {
            chain.doFilter(req, res);
        } else if (path.startsWith("/api/")) {
            FilterPaths.jsonError(response, HttpServletResponse.SC_FORBIDDEN, "FORBIDDEN", "Không đủ quyền");
        } else {
            response.sendError(HttpServletResponse.SC_FORBIDDEN);
        }
    }
}
