package com.nexus.servlet;

import com.nexus.AppContext;
import com.nexus.model.Shop;
import com.nexus.util.Validator;
import jakarta.servlet.annotation.WebServlet;
import jakarta.servlet.http.HttpServletRequest;

/** /admin/shops (FR-ADM-02). */
@WebServlet({"/admin/shops", "/admin/shops/*"})
public class AdminShopServlet extends BaseAdminServlet<Shop> {
    public AdminShopServlet() {
        super("shops", AppContext.shopService());
    }

    @Override
    protected Shop parseForm(HttpServletRequest req) {
        Shop x = new Shop();
        x.setId(Validator.intOr(req.getParameter("id"), 0, 0, Integer.MAX_VALUE));
        x.setName(req.getParameter("name"));
        x.setAddress(req.getParameter("address"));
        x.setCityId(Validator.intOr(req.getParameter("cityId"), 0, 0, Integer.MAX_VALUE));
        x.setPhone(req.getParameter("phone"));
        return x;
    }

    @Override
    protected Shop emptyItem() {
        return new Shop();
    }

    @Override
    protected void prepareForm(HttpServletRequest req) {
        req.setAttribute("cities", AppContext.cityService().listActive());
    }
}
