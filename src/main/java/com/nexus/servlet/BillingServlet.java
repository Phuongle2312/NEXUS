package com.nexus.servlet;

import com.nexus.AppContext;
import com.nexus.dao.BillDao;
import com.nexus.model.Bill;
import com.nexus.model.BillCallLine;
import com.nexus.model.BillingContext;
import com.nexus.service.BillForm;
import com.nexus.service.BillingService;
import com.nexus.service.BusinessException;
import com.nexus.util.Validator;
import jakarta.servlet.ServletException;
import jakarta.servlet.annotation.WebServlet;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;

import java.io.IOException;
import java.math.BigDecimal;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

/** Hóa đơn (API Design 4.7). Phân quyền theo URL nằm ở AccessPolicy. */
@WebServlet("/billing/*")
public class BillingServlet extends BaseServlet {
    private static final List<String> CALL_TYPES = List.of("LOCAL", "STD", "SMS");

    private final transient BillingService billing = AppContext.billingService();

    public BillingServlet() {
        get("/list", this::list);
        get("/new", this::newForm);
        post("/preview", this::preview);
        post("/generate", this::generate);
        get("/edit", this::editForm);
        post("/update", this::update);
        get("/view", this::viewBill);
        get("/overdue", this::overdue);
        post("/request-suspend", this::requestSuspend);
    }

    private void list(HttpServletRequest req, HttpServletResponse resp) throws ServletException, IOException {
        String accountId = Validator.clean(req.getParameter("accountId"));
        BillDao.Filter filter = new BillDao.Filter(
                accountId == null ? null : Validator.accountId(accountId),
                Validator.clean(req.getParameter("status")),
                Validator.dateOrNull(req.getParameter("from"), "Từ ngày"),
                Validator.dateOrNull(req.getParameter("to"), "Đến ngày"));
        int page = Validator.intOr(req.getParameter("page"), 1, 1, Integer.MAX_VALUE);
        req.setAttribute("result", billing.search(filter, page, 20));
        view(req, resp, "billing/list");
    }

    /** Chưa có accountId: chỉ hiện ô nhập mã. Có accountId: hiện form theo gói của kết nối. */
    private void newForm(HttpServletRequest req, HttpServletResponse resp) throws ServletException, IOException {
        String accountId = Validator.clean(req.getParameter("accountId"));
        if (accountId != null) {
            prepareContext(req, Validator.accountId(accountId));
        }
        view(req, resp, "billing/form");
    }

    /** Nạp thông tin kết nối và đơn giá cho form. */
    private void prepareContext(HttpServletRequest req, String accountId) {
        BillingContext ctx = billing.context(accountId);
        req.setAttribute("ctx2", ctx);
        req.setAttribute("rates", billing.callRates(ctx.getPlanId()));
    }

    private void preview(HttpServletRequest req, HttpServletResponse resp) throws ServletException, IOException {
        String accountId = Validator.accountId(req.getParameter("accountId"));
        prepareContext(req, accountId);
        try {
            req.setAttribute("preview", billing.preview(parseForm(req, accountId)));
        } catch (BusinessException e) {
            resp.setStatus(e.getCode().httpStatus());
            req.setAttribute("errorMessage", e.getMessage());
        }
        view(req, resp, "billing/form");
    }

    private void generate(HttpServletRequest req, HttpServletResponse resp) throws ServletException, IOException {
        String accountId = Validator.accountId(req.getParameter("accountId"));
        try {
            int id = billing.generate(currentUser(req), parseForm(req, accountId));
            redirect(req, resp, "/billing/view?id=" + id, "Đã lập hóa đơn.");
        } catch (BusinessException e) {
            prepareContext(req, accountId);
            resp.setStatus(e.getCode().httpStatus());
            req.setAttribute("errorMessage", e.getMessage());
            view(req, resp, "billing/form");
        }
    }

    private void editForm(HttpServletRequest req, HttpServletResponse resp) throws ServletException, IOException {
        Bill bill = billing.view(currentUser(req), id(req));
        prepareContext(req, bill.getAccountId());
        req.setAttribute("bill", bill);
        Map<String, BigDecimal> minutes = new LinkedHashMap<>();
        for (BillCallLine line : bill.getCallLines()) {
            minutes.put(line.getCallType(), line.getMinutes());
        }
        req.setAttribute("minutes", minutes);
        view(req, resp, "billing/edit");
    }

    private void update(HttpServletRequest req, HttpServletResponse resp) throws IOException {
        int billId = id(req);
        billing.update(currentUser(req), billId, parseUsage(req));
        redirect(req, resp, "/billing/view?id=" + billId, "Đã cập nhật hóa đơn.");
    }

    private void viewBill(HttpServletRequest req, HttpServletResponse resp) throws ServletException, IOException {
        req.setAttribute("bill", billing.view(currentUser(req), id(req)));
        view(req, resp, "billing/view");
    }

    private void overdue(HttpServletRequest req, HttpServletResponse resp) throws ServletException, IOException {
        String days = Validator.clean(req.getParameter("days"));
        req.setAttribute("days", days);
        req.setAttribute("bills", billing.overdue(days == null ? null : Integer.valueOf(Validator.intOr(days, 30, 0, 3650))));
        view(req, resp, "billing/overdue");
    }

    private void requestSuspend(HttpServletRequest req, HttpServletResponse resp) throws IOException {
        String accountId = Validator.accountId(req.getParameter("accountId"));
        String reason = Validator.required(req.getParameter("reason"), "Lý do", 500);
        billing.requestSuspend(currentUser(req), accountId, reason);
        redirect(req, resp, "/billing/overdue", "Đã gửi đề nghị tạm ngưng " + accountId + " tới Kỹ thuật.");
    }

    private int id(HttpServletRequest req) {
        return Validator.intValue(req.getParameter("id"), "Mã hóa đơn");
    }

    /** Đọc form lập hóa đơn: kỳ cước và số liệu sử dụng. */
    private BillForm parseForm(HttpServletRequest req, String accountId) {
        BillForm usage = parseUsage(req);
        return new BillForm(accountId,
                Validator.date(req.getParameter("periodStart"), "Ngày bắt đầu kỳ"),
                Validator.date(req.getParameter("periodEnd"), "Ngày kết thúc kỳ"),
                usage.minutes(), usage.usageHours(), usage.adjustment(), usage.note());
    }

    /** Đọc phần số liệu sử dụng, dùng chung cho lập mới và sửa. */
    private BillForm parseUsage(HttpServletRequest req) {
        Map<String, BigDecimal> minutes = new LinkedHashMap<>();
        for (String type : CALL_TYPES) {
            minutes.put(type, Validator.nonNegativeOrZero(req.getParameter("minutes" + type), "Số phút " + type));
        }
        return new BillForm(null, null, null, minutes,
                Validator.nonNegativeOrZero(req.getParameter("usageHours"), "Số giờ dùng"),
                Validator.moneyOrZero(req.getParameter("adjustment"), "Điều chỉnh"),
                Validator.optional(req.getParameter("note"), "Ghi chú", 500));
    }
}
