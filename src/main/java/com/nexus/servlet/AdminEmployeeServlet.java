package com.nexus.servlet;

import com.nexus.AppContext;
import com.nexus.model.Employee;
import com.nexus.service.EmployeeService;
import com.nexus.util.Validator;
import jakarta.servlet.annotation.WebServlet;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;

import java.io.IOException;

/** /admin/employees (FR-ADM-01), thêm endpoint đặt lại mật khẩu. */
@WebServlet({"/admin/employees", "/admin/employees/*"})
public class AdminEmployeeServlet extends BaseAdminServlet<Employee> {
    private final transient EmployeeService employees = AppContext.employeeService();

    public AdminEmployeeServlet() {
        super("employees", AppContext.employeeService());
        post("/reset-password", this::resetPassword);
    }

    @Override
    protected Employee parseForm(HttpServletRequest req) {
        Employee x = new Employee();
        x.setId(Validator.intOr(req.getParameter("id"), 0, 0, Integer.MAX_VALUE));
        x.setFullName(req.getParameter("fullName"));
        x.setRole(req.getParameter("role"));
        x.setPhone(req.getParameter("phone"));
        x.setEmail(req.getParameter("email"));
        int shopId = Validator.intOr(req.getParameter("shopId"), 0, 0, Integer.MAX_VALUE);
        x.setShopId(shopId == 0 ? null : shopId);
        x.setUsername(req.getParameter("username"));
        x.setPassword(req.getParameter("password"));
        return x;
    }

    @Override
    protected Employee emptyItem() {
        return new Employee();
    }

    @Override
    protected void prepareForm(HttpServletRequest req) {
        req.setAttribute("shops", AppContext.shopService().listActive());
    }

    /** Mật khẩu tạm chỉ hiển thị một lần qua thông báo, không lưu ở đâu ngoài bản băm. */
    private void resetPassword(HttpServletRequest req, HttpServletResponse resp) throws IOException {
        String temporary = employees.resetPassword(currentUser(req), id(req));
        redirect(req, resp, listPath(), "Mật khẩu tạm (chỉ hiện một lần): " + temporary);
    }
}
