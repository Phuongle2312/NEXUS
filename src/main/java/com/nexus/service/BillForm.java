package com.nexus.service;

import java.math.BigDecimal;
import java.time.LocalDate;
import java.util.Map;

/**
 * Dữ liệu kế toán nhập khi lập hoặc sửa hóa đơn (giả định SRS phần 10, mục 7 và 8).
 *
 * @param minutes số phút theo loại cuộc gọi (LOCAL, STD, SMS)
 */
public record BillForm(String accountId, LocalDate periodStart, LocalDate periodEnd,
                       Map<String, BigDecimal> minutes, BigDecimal usageHours, BigDecimal adjustment, String note) {
}
