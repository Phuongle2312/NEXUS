package com.nexus.filter;

import java.util.ArrayList;
import java.util.List;
import java.util.Set;

/**
 * Bảng phân quyền endpoint (API Design, mục 3 và 4). Quy tắc khớp theo thứ tự,
 * nên đường dẫn cụ thể phải đứng trước đường dẫn cha. URL không khớp quy tắc nào
 * bị từ chối: thêm endpoint mới thì phải khai báo ở đây.
 */
public final class AccessPolicy {
    private static final Set<String> STAFF = Set.of("ADMIN", "RETAIL", "TECH", "ACCOUNTS");
    private static final Set<String> EVERYONE = Set.of("ADMIN", "RETAIL", "TECH", "ACCOUNTS", "CUSTOMER");

    private record Rule(String method, String path, boolean prefix, Set<String> roles) {
        boolean matches(String m, String p) {
            if (method != null && !method.equals(m)) {
                return false;
            }
            return prefix ? p.startsWith(path) : p.equals(path);
        }
    }

    private static final List<Rule> RULES = new ArrayList<>();

    static {
        // Sau đăng nhập: trang chủ theo vai trò, đổi mật khẩu, đăng xuất.
        exact("/dashboard", EVERYONE);
        prefix("/auth/", EVERYONE);

        // Admin.
        prefix("/admin/", Set.of("ADMIN"));

        // Hồ sơ khách hàng: Retail CRU, nhân viên khác chỉ xem.
        exact("/customer-mgmt/list", STAFF);
        exact("/customer-mgmt/view", STAFF);
        prefix("/customer-mgmt/", Set.of("RETAIL"));

        // Đơn đăng ký: chỉ Retail tạo; khách chỉ xem đơn của mình (Service kiểm tra sở hữu).
        exact("/order/list", STAFF);
        exact("/order/view", EVERYONE);
        prefix("/order/", Set.of("RETAIL"));

        // Kết nối và kỹ thuật.
        prefix("/connections/", STAFF);
        prefix("/tech/", Set.of("TECH"));

        // Hóa đơn: chỉ Accounts tạo và sửa; khách chỉ xem hóa đơn của mình.
        exact("/billing/list", Set.of("ADMIN", "RETAIL", "ACCOUNTS"));
        exact("/billing/view", Set.of("ADMIN", "RETAIL", "ACCOUNTS", "CUSTOMER"));
        prefix("/billing/", Set.of("ACCOUNTS"));

        // Thanh toán: Retail và Accounts ghi nhận; Admin chỉ xem; hoàn cọc chỉ Accounts.
        exact("/payment/list", Set.of("ADMIN", "RETAIL", "ACCOUNTS"));
        exact("/payment/receipt", Set.of("ADMIN", "RETAIL", "ACCOUNTS"));
        exact("/payment/refund-deposit", Set.of("ACCOUNTS"));
        prefix("/payment/", Set.of("RETAIL", "ACCOUNTS"));

        // Khu vực khách hàng.
        prefix("/my/", Set.of("CUSTOMER"));

        // Tìm kiếm nâng cao: mọi người đã đăng nhập, khách bị giới hạn phạm vi ở Service.
        prefix("/search/", EVERYONE);
        prefix("/api/search/", EVERYONE);

        // Phản hồi.
        exact("/feedback/list", STAFF);
        exact("/feedback/create", Set.of("RETAIL"));

        // Báo cáo (mục 4.13).
        exact("/reports/orders", Set.of("ADMIN", "TECH", "RETAIL"));
        exact("/reports/connections", Set.of("ADMIN", "TECH"));
        exact("/reports/stock", Set.of("ADMIN", "TECH"));
        exact("/reports/billing", Set.of("ADMIN", "ACCOUNTS"));

        // JSON phụ trợ cho form động (mục 4.11).
        exact("/api/shops", Set.of("CUSTOMER"));
        exact("/api/customers/lookup", Set.of("RETAIL"));
        prefix("/api/", EVERYONE);
    }

    private AccessPolicy() {
    }

    private static void exact(String path, Set<String> roles) {
        RULES.add(new Rule(null, path, false, roles));
    }

    private static void prefix(String path, Set<String> roles) {
        RULES.add(new Rule(null, path, true, roles));
    }

    /** URL công khai, không cần đăng nhập. */
    public static boolean isPublic(String path) {
        return path.equals("/")
                || path.startsWith("/public/")
                || path.startsWith("/assets/")
                || path.equals("/auth/login");
    }

    /** Vai trò có được gọi URL này không. Không khớp quy tắc nào thì từ chối. */
    public static boolean isAllowed(String method, String path, String role) {
        for (Rule rule : RULES) {
            if (rule.matches(method, path)) {
                return rule.roles().contains(role);
            }
        }
        return false;
    }
}
