package com.nexus.servlet;

import com.nexus.service.BusinessException;
import com.nexus.service.CrudService;
import com.nexus.util.Validator;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;

import java.io.IOException;

/**
 * Bộ 7 endpoint chuẩn của danh mục Admin (API Design 4.3): list, new, create, edit, update, delete, deactivate.
 *
 * Cách dùng lại (task 10, 11): kế thừa lớp này, map vào "/admin/{res}/*", viết parseForm,
 * tạo hai JSP admin/{res}-list.jsp và admin/{res}-form.jsp.
 */
public abstract class BaseAdminServlet<T> extends BaseServlet {
    private final String resource;
    private final transient CrudService<T> service;

    protected BaseAdminServlet(String resource, CrudService<T> service) {
        this.resource = resource;
        this.service = service;
        get("/", this::list);
        get("/new", this::newForm);
        post("/create", this::create);
        get("/edit", this::editForm);
        post("/update", this::update);
        post("/delete", this::delete);
        post("/deactivate", this::deactivate);
    }

    /** Đọc form thành đối tượng, chưa kiểm tra nghiệp vụ (Service kiểm tra), để hiện lại dữ liệu đã nhập khi lỗi. */
    protected abstract T parseForm(HttpServletRequest req);

    /** Đối tượng rỗng cho form thêm mới. */
    protected abstract T emptyItem();

    /** Nạp dữ liệu phụ cho form, ví dụ danh sách thành phố. Mặc định không có. */
    protected void prepareForm(HttpServletRequest req) {
    }

    private void list(HttpServletRequest req, HttpServletResponse resp) throws ServletException, IOException {
        String q = Validator.clean(req.getParameter("q"));
        int page = Validator.intOr(req.getParameter("page"), 1, 1, Integer.MAX_VALUE);
        int size = Validator.intOr(req.getParameter("size"), 20, 1, 100);
        req.setAttribute("q", q);
        req.setAttribute("result", service.search(q, page, size));
        view(req, resp, "admin/" + resource + "-list");
    }

    private void newForm(HttpServletRequest req, HttpServletResponse resp) throws ServletException, IOException {
        showForm(req, resp, emptyItem(), false);
    }

    private void editForm(HttpServletRequest req, HttpServletResponse resp) throws ServletException, IOException {
        showForm(req, resp, service.get(id(req)), true);
    }

    private void showForm(HttpServletRequest req, HttpServletResponse resp, T item, boolean editing)
            throws ServletException, IOException {
        prepareForm(req);
        req.setAttribute("item", item);
        req.setAttribute("editing", editing);
        view(req, resp, "admin/" + resource + "-form");
    }

    private void create(HttpServletRequest req, HttpServletResponse resp) throws ServletException, IOException {
        T item = parseForm(req);
        try {
            service.create(currentUser(req), item);
            redirect(req, resp, listPath(), "Đã thêm mới.");
        } catch (BusinessException e) {
            formError(req, resp, item, false, e);
        }
    }

    private void update(HttpServletRequest req, HttpServletResponse resp) throws ServletException, IOException {
        T item = parseForm(req);
        try {
            service.update(currentUser(req), item);
            redirect(req, resp, listPath(), "Đã cập nhật.");
        } catch (BusinessException e) {
            formError(req, resp, item, true, e);
        }
    }

    /** Hiện lại form với dữ liệu đã nhập, kèm thông báo lỗi và mã HTTP đúng. */
    private void formError(HttpServletRequest req, HttpServletResponse resp, T item, boolean editing, BusinessException e)
            throws ServletException, IOException {
        resp.setStatus(e.getCode().httpStatus());
        req.setAttribute("errorMessage", e.getMessage());
        showForm(req, resp, item, editing);
    }

    private void delete(HttpServletRequest req, HttpServletResponse resp) throws IOException {
        service.delete(currentUser(req), id(req));
        redirect(req, resp, listPath(), "Đã xóa.");
    }

    private void deactivate(HttpServletRequest req, HttpServletResponse resp) throws IOException {
        service.deactivate(currentUser(req), id(req));
        redirect(req, resp, listPath(), "Đã ngưng sử dụng.");
    }

    protected int id(HttpServletRequest req) {
        return Validator.intValue(req.getParameter("id"), "Mã");
    }

    protected String listPath() {
        return "/admin/" + resource;
    }
}
