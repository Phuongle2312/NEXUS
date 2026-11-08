package com.nexus.filter;

import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;

import java.io.IOException;

/** Tiện ích dùng chung cho các filter. */
final class FilterPaths {
    private FilterPaths() {
    }

    /** Đường dẫn trong ứng dụng, không gồm context path, để so khớp quy tắc phân quyền. */
    static String path(HttpServletRequest request) {
        String servletPath = request.getServletPath();
        String pathInfo = request.getPathInfo();
        return pathInfo == null ? servletPath : servletPath + pathInfo;
    }

    /** Trả lỗi JSON theo định dạng API Design 1.4. Mã lỗi và thông báo là hằng số, không chứa dữ liệu người dùng. */
    static void jsonError(HttpServletResponse response, int status, String code, String message) throws IOException {
        response.setStatus(status);
        response.setContentType("application/json;charset=UTF-8");
        response.getWriter().write("{\"success\":false,\"error\":{\"code\":\"" + code + "\",\"message\":\"" + message + "\"}}");
    }
}
