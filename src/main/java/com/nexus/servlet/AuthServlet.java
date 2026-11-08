package com.nexus.servlet;

import com.nexus.AppContext;
import com.nexus.filter.CsrfFilter;
import com.nexus.model.SessionUser;
import com.nexus.service.AuthService;
import com.nexus.service.BusinessException;
import jakarta.servlet.ServletException;
import jakarta.servlet.annotation.WebServlet;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import jakarta.servlet.http.HttpSession;

import java.io.IOException;

/** Đăng nhập, đăng xuất, đổi mật khẩu (API Design 4.1). */
@WebServlet("/auth/*")
public class AuthServlet extends BaseServlet {
    // Trang chủ công khai là task 12 của Giỏi; khi xong thì đổi thành /public/home theo API Design.
    private static final String AFTER_LOGOUT = "/auth/login";

    private final transient AuthService authService = AppContext.authService();

    public AuthServlet() {
        get("/login", this::loginForm);
        post("/login", this::login);
        post("/logout", this::logout);
        get("/change-password", this::changePasswordForm);
        post("/change-password", this::changePassword);
    }

    private void loginForm(HttpServletRequest req, HttpServletResponse resp) throws ServletException, IOException {
        if (currentUser(req) != null) {
            resp.sendRedirect(req.getContextPath() + "/dashboard");
            return;
        }
        view(req, resp, "auth/login");
    }

    /** Đăng nhập thành công: đổi session id và token CSRF để chống session fixation. */
    private void login(HttpServletRequest req, HttpServletResponse resp) throws ServletException, IOException {
        try {
            SessionUser user = authService.login(req.getParameter("username"), req.getParameter("password"));
            req.changeSessionId();
            HttpSession session = req.getSession();
            session.setAttribute(SessionUser.SESSION_KEY, user);
            session.setAttribute(CsrfFilter.TOKEN_ATTR, CsrfFilter.newToken());
            resp.sendRedirect(req.getContextPath() + (user.isMustChangePassword() ? "/auth/change-password" : "/dashboard"));
        } catch (BusinessException e) {
            resp.setStatus(e.getCode().httpStatus());
            req.setAttribute("errorMessage", e.getMessage());
            view(req, resp, "auth/login");
        }
    }

    private void logout(HttpServletRequest req, HttpServletResponse resp) throws IOException {
        HttpSession session = req.getSession(false);
        if (session != null) {
            session.invalidate();
        }
        resp.sendRedirect(req.getContextPath() + AFTER_LOGOUT);
    }

    private void changePasswordForm(HttpServletRequest req, HttpServletResponse resp) throws ServletException, IOException {
        view(req, resp, "auth/change-password");
    }

    private void changePassword(HttpServletRequest req, HttpServletResponse resp) throws ServletException, IOException {
        try {
            authService.changePassword(currentUser(req), req.getParameter("oldPassword"), req.getParameter("newPassword"));
            redirect(req, resp, "/dashboard", "Đã đổi mật khẩu.");
        } catch (BusinessException e) {
            resp.setStatus(e.getCode().httpStatus());
            req.setAttribute("errorMessage", e.getMessage());
            view(req, resp, "auth/change-password");
        }
    }
}
