package com.nexus.servlet;

import com.nexus.AppContext;
import com.nexus.dao.PaymentDao;
import com.nexus.service.BusinessException;
import com.nexus.service.PaymentService;
import com.nexus.util.Validator;
import jakarta.servlet.ServletException;
import jakarta.servlet.annotation.WebServlet;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;

import java.io.IOException;
import java.math.BigDecimal;
import java.time.LocalDate;

/** Thanh toán và hoàn cọc (API Design 4.8). */
@WebServlet("/payment/*")
public class PaymentServlet extends BaseServlet {
    private final transient PaymentService payments = AppContext.paymentService();

    public PaymentServlet() {
        get("/list", this::list);
        get("/new", this::newForm);
        post("/create", this::create);
        get("/receipt", this::receipt);
        post("/refund-deposit", this::refundDeposit);
    }

    private void list(HttpServletRequest req, HttpServletResponse resp) throws ServletException, IOException {
        String accountId = Validator.clean(req.getParameter("accountId"));
        String billId = Validator.clean(req.getParameter("billId"));
        PaymentDao.Filter filter = new PaymentDao.Filter(
                accountId == null ? null : Validator.accountId(accountId),
                billId == null ? null : Integer.valueOf(Validator.intValue(billId, "Mã hóa đơn")),
                Validator.dateOrNull(req.getParameter("from"), "Từ ngày"),
                Validator.dateOrNull(req.getParameter("to"), "Đến ngày"));
        int page = Validator.intOr(req.getParameter("page"), 1, 1, Integer.MAX_VALUE);
        req.setAttribute("result", payments.search(filter, page, 20));
        view(req, resp, "payment/list");
    }

    private void newForm(HttpServletRequest req, HttpServletResponse resp) throws ServletException, IOException {
        req.setAttribute("bill", payments.billForPayment(Validator.intValue(req.getParameter("billId"), "Mã hóa đơn")));
        req.setAttribute("today", LocalDate.now());
        view(req, resp, "payment/new");
    }

    /** Lỗi nghiệp vụ thì hiện lại form cùng số còn nợ mới nhất. */
    private void create(HttpServletRequest req, HttpServletResponse resp) throws ServletException, IOException {
        int billId = Validator.intValue(req.getParameter("billId"), "Mã hóa đơn");
        try {
            BigDecimal amount = Validator.money(req.getParameter("amount"), "Số tiền");
            LocalDate paidDate = Validator.date(req.getParameter("paidDate"), "Ngày thanh toán");
            String note = Validator.optional(req.getParameter("note"), "Ghi chú", 255);
            int id = payments.record(currentUser(req), billId, amount, req.getParameter("method"), paidDate, note);
            redirect(req, resp, "/payment/receipt?id=" + id, "Đã ghi nhận thanh toán.");
        } catch (BusinessException e) {
            resp.setStatus(e.getCode().httpStatus());
            req.setAttribute("errorMessage", e.getMessage());
            req.setAttribute("bill", payments.billForPayment(billId));
            req.setAttribute("today", LocalDate.now());
            view(req, resp, "payment/new");
        }
    }

    private void receipt(HttpServletRequest req, HttpServletResponse resp) throws ServletException, IOException {
        req.setAttribute("payment", payments.receipt(Validator.intValue(req.getParameter("id"), "Mã biên lai")));
        view(req, resp, "payment/receipt");
    }

    private void refundDeposit(HttpServletRequest req, HttpServletResponse resp) throws IOException {
        String accountId = Validator.accountId(req.getParameter("accountId"));
        PaymentService.RefundResult r = payments.refundDeposit(currentUser(req), accountId);
        redirect(req, resp, "/billing/list?accountId=" + accountId,
                "Hoàn cọc " + accountId + ": cọc " + r.billedDeposit() + "$, bù trừ " + r.offset() + "$, hoàn " + r.refund() + "$.");
    }
}
