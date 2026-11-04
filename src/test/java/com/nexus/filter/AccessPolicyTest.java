package com.nexus.filter;

import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertTrue;

import org.junit.jupiter.api.Test;

/** Đối chiếu bảng phân quyền với API Design mục 3 và các ràng buộc BR-08. */
class AccessPolicyTest {

    private static boolean can(String role, String path) {
        return AccessPolicy.isAllowed("GET", path, role);
    }

    @Test
    void publicPaths() {
        assertTrue(AccessPolicy.isPublic("/public/home"));
        assertTrue(AccessPolicy.isPublic("/auth/login"));
        assertTrue(AccessPolicy.isPublic("/assets/css/app.css"));
        assertFalse(AccessPolicy.isPublic("/auth/change-password"));
        assertFalse(AccessPolicy.isPublic("/admin/shops"));
        assertFalse(AccessPolicy.isPublic("/publicx"));
    }

    @Test
    void onlyAdminReachesAdminModule() {
        assertTrue(can("ADMIN", "/admin/employees"));
        for (String role : new String[] {"RETAIL", "TECH", "ACCOUNTS", "CUSTOMER"}) {
            assertFalse(can(role, "/admin/employees"), role);
        }
    }

    @Test
    void onlyAccountsWritesBills() {
        assertTrue(can("ACCOUNTS", "/billing/generate"));
        assertTrue(can("ACCOUNTS", "/billing/update"));
        for (String role : new String[] {"ADMIN", "RETAIL", "TECH", "CUSTOMER"}) {
            assertFalse(can(role, "/billing/generate"), role);
            assertFalse(can(role, "/billing/update"), role);
            assertFalse(can(role, "/billing/request-suspend"), role);
        }
        assertTrue(can("RETAIL", "/billing/list"));
        assertTrue(can("ADMIN", "/billing/list"));
        assertFalse(can("TECH", "/billing/list"));
        assertTrue(can("CUSTOMER", "/billing/view"));
        assertFalse(can("TECH", "/billing/view"));
    }

    @Test
    void paymentRoles() {
        assertTrue(can("RETAIL", "/payment/create"));
        assertTrue(can("ACCOUNTS", "/payment/create"));
        assertFalse(can("ADMIN", "/payment/create"), "Admin chỉ xem");
        assertTrue(can("ADMIN", "/payment/list"));
        assertTrue(can("ACCOUNTS", "/payment/refund-deposit"));
        assertFalse(can("RETAIL", "/payment/refund-deposit"));
        assertFalse(can("CUSTOMER", "/payment/list"));
    }

    @Test
    void onlyRetailCreatesOrders() {
        assertTrue(can("RETAIL", "/order/create"));
        for (String role : new String[] {"ADMIN", "TECH", "ACCOUNTS", "CUSTOMER"}) {
            assertFalse(can(role, "/order/create"), role);
            assertFalse(can(role, "/order/requests/confirm"), role);
        }
        assertTrue(can("CUSTOMER", "/order/view"));
        assertFalse(can("CUSTOMER", "/order/list"));
    }

    @Test
    void customerAreaIsCustomerOnly() {
        assertTrue(can("CUSTOMER", "/my/bills"));
        assertFalse(can("ADMIN", "/my/bills"));
    }

    @Test
    void unknownPathIsDenied() {
        assertFalse(can("ADMIN", "/secret/thing"));
    }
}
