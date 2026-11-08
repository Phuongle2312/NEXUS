package com.nexus;

import com.nexus.dao.AuditDao;
import com.nexus.dao.BillDao;
import com.nexus.dao.BillingContextDao;
import com.nexus.dao.CityDao;
import com.nexus.dao.ConfigDao;
import com.nexus.dao.JdbcAuditDao;
import com.nexus.dao.JdbcBillDao;
import com.nexus.dao.JdbcBillingContextDao;
import com.nexus.dao.JdbcCityDao;
import com.nexus.dao.JdbcConfigDao;
import com.nexus.dao.JdbcEmployeeDao;
import com.nexus.dao.JdbcPaymentDao;
import com.nexus.dao.JdbcReplacementChargeDao;
import com.nexus.dao.JdbcShopDao;
import com.nexus.dao.JdbcUserAccountDao;
import com.nexus.dao.PaymentDao;
import com.nexus.dao.ReplacementChargeDao;
import com.nexus.dao.ShopDao;
import com.nexus.dao.UserAccountDao;
import com.nexus.service.AuthService;
import com.nexus.service.BillingService;
import com.nexus.service.CityService;
import com.nexus.service.EmployeeService;
import com.nexus.service.PaymentService;
import com.nexus.service.ShopService;
import com.nexus.util.DbTxRunner;
import com.nexus.util.TxRunner;

/**
 * Nơi duy nhất lắp ráp đối tượng (composition root): chọn bản DAO cụ thể và truyền vào Service.
 * Servlet và filter lấy Service từ đây, nên chỉ lớp này biết đến các lớp Jdbc*.
 * Module mới (B, C): thêm DAO và Service của mình vào đây.
 */
public final class AppContext {
    private static final TxRunner TX = new DbTxRunner();
    private static final AuditDao AUDIT = new JdbcAuditDao();
    private static final UserAccountDao USERS = new JdbcUserAccountDao();
    private static final CityDao CITIES = new JdbcCityDao();
    private static final ShopDao SHOPS = new JdbcShopDao();
    private static final BillDao BILLS = new JdbcBillDao();
    private static final BillingContextDao CONTEXTS = new JdbcBillingContextDao();
    private static final ReplacementChargeDao CHARGES = new JdbcReplacementChargeDao();
    private static final ConfigDao CONFIG = new JdbcConfigDao();
    private static final PaymentDao PAYMENTS = new JdbcPaymentDao();

    private static final AuthService AUTH = new AuthService(TX, USERS, AUDIT);
    private static final CityService CITY = new CityService(TX, CITIES, AUDIT);
    private static final ShopService SHOP = new ShopService(TX, SHOPS, AUDIT);
    private static final EmployeeService EMPLOYEE = new EmployeeService(TX, new JdbcEmployeeDao(), USERS, AUDIT);
    private static final BillingService BILLING = new BillingService(TX, BILLS, CONTEXTS, CHARGES, CONFIG, AUDIT);
    private static final PaymentService PAYMENT = new PaymentService(TX, PAYMENTS, BILLS, CONTEXTS, CHARGES, AUDIT);

    private AppContext() {
    }

    public static AuthService authService() {
        return AUTH;
    }

    public static CityService cityService() {
        return CITY;
    }

    public static ShopService shopService() {
        return SHOP;
    }

    public static EmployeeService employeeService() {
        return EMPLOYEE;
    }

    public static BillingService billingService() {
        return BILLING;
    }

    public static PaymentService paymentService() {
        return PAYMENT;
    }
}
