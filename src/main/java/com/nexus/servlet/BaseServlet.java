package com.nexus.servlet;

import com.nexus.model.SessionUser;
import com.nexus.service.BusinessException;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServlet;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import jakarta.servlet.http.HttpSession;

import java.io.IOException;
import java.util.HashMap;
import java.util.Map;

/**
 * Lớp cha của mọi servlet: định tuyến theo action, hiển thị JSP, thông báo một lần.
 *
 * Cách dùng: map servlet vào "/module/*", rồi đăng ký trong constructor
 * {@code get("/list", this::list); post("/create", this::create);}.
 * Servlet chỉ nhận request, gọi Service và chọn JSP; không đặt nghiệp vụ ở đây.
 */
public abstract class BaseServlet extends HttpServlet {

    /** Xử lý một action. */
    @FunctionalInterface
    protected interface Action {
        void handle(HttpServletRequest req, HttpServletResponse resp) throws ServletException, IOException;
    }

    private static final String FLASH_KEY = "flash";
    private final transient Map<String, Action> getActions = new HashMap<>();
    private final transient Map<String, Action> postActions = new HashMap<>();

    protected void get(String path, Action action) {
        getActions.put(path, action);
    }

    protected void post(String path, Action action) {
        postActions.put(path, action);
    }

    @Override
    protected void doGet(HttpServletRequest req, HttpServletResponse resp) throws ServletException, IOException {
        dispatch(getActions, req, resp);
    }

    @Override
    protected void doPost(HttpServletRequest req, HttpServletResponse resp) throws ServletException, IOException {
        dispatch(postActions, req, resp);
    }

    /** Tìm action theo pathInfo. BusinessException được đổi thành trang lỗi với mã HTTP đúng. */
    private void dispatch(Map<String, Action> actions, HttpServletRequest req, HttpServletResponse resp)
            throws ServletException, IOException {
        String path = req.getPathInfo() == null ? "/" : req.getPathInfo();
        Action action = actions.get(path);
        if (action == null) {
            resp.sendError(HttpServletResponse.SC_NOT_FOUND);
            return;
        }
        try {
            action.handle(req, resp);
        } catch (BusinessException e) {
            showError(req, resp, e);
        }
    }

    /** Trang lỗi chung. Lớp con có thể bắt lỗi sớm hơn để hiển thị lại form cùng dữ liệu đã nhập. */
    protected void showError(HttpServletRequest req, HttpServletResponse resp, BusinessException e)
            throws ServletException, IOException {
        resp.setStatus(e.getCode().httpStatus());
        req.setAttribute("errorCode", e.getCode().name());
        req.setAttribute("errorMessage", e.getMessage());
        view(req, resp, "error");
    }

    /** Hiển thị JSP trong WEB-INF/views. */
    protected void view(HttpServletRequest req, HttpServletResponse resp, String name)
            throws ServletException, IOException {
        req.getRequestDispatcher("/WEB-INF/views/" + name + ".jsp").forward(req, resp);
    }

    /** Post-Redirect-Get: chuyển trang kèm thông báo hiển thị một lần ở trang đích. */
    protected void redirect(HttpServletRequest req, HttpServletResponse resp, String path, String flash)
            throws IOException {
        if (flash != null) {
            req.getSession().setAttribute(FLASH_KEY, flash);
        }
        resp.sendRedirect(req.getContextPath() + path);
    }

    /** Người dùng hiện tại; AuthFilter đã bảo đảm tồn tại với URL cần đăng nhập. */
    protected SessionUser currentUser(HttpServletRequest req) {
        HttpSession session = req.getSession(false);
        return session == null ? null : (SessionUser) session.getAttribute(SessionUser.SESSION_KEY);
    }
}
