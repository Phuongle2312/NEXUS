package com.nexus.servlet;

import com.nexus.AppContext;
import com.nexus.model.City;
import com.nexus.util.Validator;
import jakarta.servlet.annotation.WebServlet;
import jakarta.servlet.http.HttpServletRequest;

/** /admin/cities (FR-ADM-07). */
@WebServlet({"/admin/cities", "/admin/cities/*"})
public class AdminCityServlet extends BaseAdminServlet<City> {
    public AdminCityServlet() {
        super("cities", AppContext.cityService());
    }

    @Override
    protected City parseForm(HttpServletRequest req) {
        City x = new City();
        x.setId(Validator.intOr(req.getParameter("id"), 0, 0, Integer.MAX_VALUE));
        x.setName(req.getParameter("name"));
        x.setCode(req.getParameter("code"));
        return x;
    }

    @Override
    protected City emptyItem() {
        return new City();
    }
}
