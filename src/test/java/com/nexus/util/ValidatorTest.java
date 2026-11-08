package com.nexus.util;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertNull;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;

import com.nexus.service.BusinessException;
import org.junit.jupiter.api.Test;

import java.math.BigDecimal;

/** Kiểm tra quy tắc nhập liệu và chính sách mật khẩu. */
class ValidatorTest {

    @Test
    void orderAndAccountIdFormats() {
        assertEquals("D0000000001", Validator.orderId(" D0000000001 "));
        assertEquals("B001000000000001", Validator.accountId("B001000000000001"));
        assertThrows(BusinessException.class, () -> Validator.orderId("X0000000001"));
        assertThrows(BusinessException.class, () -> Validator.orderId("D000000001"));
        assertThrows(BusinessException.class, () -> Validator.accountId("B00100000000000"));
        assertThrows(BusinessException.class, () -> Validator.accountId("' OR 1=1 --"));
    }

    @Test
    void moneyAllowsTwoDecimalsOnly() {
        assertEquals(new BigDecimal("12.50"), Validator.money("12.5", "Số tiền"));
        assertEquals(new BigDecimal("12.50"), Validator.money("12.500", "Số tiền"));
        assertThrows(BusinessException.class, () -> Validator.money("12.555", "Số tiền"));
        assertThrows(BusinessException.class, () -> Validator.money("abc", "Số tiền"));
        assertThrows(BusinessException.class, () -> Validator.money("", "Số tiền"));
    }

    @Test
    void optionalFieldsAcceptBlank() {
        assertNull(Validator.phone("  "));
        assertNull(Validator.email(""));
        assertThrows(BusinessException.class, () -> Validator.phone("12ab"));
        assertThrows(BusinessException.class, () -> Validator.email("a@b"));
    }

    @Test
    void passwordPolicyAndTemporaryPassword() {
        assertFalse(PasswordUtil.isStrongEnough("short1"));
        assertFalse(PasswordUtil.isStrongEnough("onlyletters"));
        assertFalse(PasswordUtil.isStrongEnough("12345678"));
        assertTrue(PasswordUtil.isStrongEnough("Nexus2026"));
        for (int i = 0; i < 50; i++) {
            assertTrue(PasswordUtil.isStrongEnough(PasswordUtil.generateTemporary()));
        }
    }

    @Test
    void passwordHashRoundTrip() {
        String hash = PasswordUtil.hash("Nexus2026");
        assertTrue(PasswordUtil.verify("Nexus2026", hash));
        assertFalse(PasswordUtil.verify("Nexus2027", hash));
        assertFalse(PasswordUtil.verify("Nexus2026", "khong-phai-hash"));
    }
}
