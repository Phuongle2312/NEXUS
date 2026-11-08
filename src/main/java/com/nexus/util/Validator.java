package com.nexus.util;

import com.nexus.service.BusinessException;
import com.nexus.service.ErrorCode;

import java.math.BigDecimal;
import java.time.LocalDate;
import java.time.format.DateTimeParseException;
import java.util.regex.Pattern;

/**
 * Kiểm tra và chuyển đổi dữ liệu đầu vào. Mọi lỗi đều ném VALIDATION_ERROR kèm tên trường.
 */
public final class Validator {
    private static final Pattern ORDER_ID = Pattern.compile("^[DTB][0-9]{10}$");
    private static final Pattern ACCOUNT_ID = Pattern.compile("^[DBT][0-9]{15}$");
    private static final Pattern PHONE = Pattern.compile("^\\+?[0-9]{8,15}$");
    private static final Pattern EMAIL = Pattern.compile("^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$");
    private static final Pattern CITY_CODE = Pattern.compile("^[0-9]{3}$");
    private static final Pattern USERNAME = Pattern.compile("^[A-Za-z0-9._-]{3,50}$");

    private Validator() {
    }

    private static BusinessException invalid(String message) {
        return new BusinessException(ErrorCode.VALIDATION_ERROR, message);
    }

    /** Cắt khoảng trắng; chuỗi rỗng coi như null. */
    public static String clean(String value) {
        if (value == null) {
            return null;
        }
        String t = value.trim();
        return t.isEmpty() ? null : t;
    }

    /** Trường bắt buộc, có giới hạn độ dài theo cột trong CSDL. */
    public static String required(String value, String field, int maxLength) {
        String t = clean(value);
        if (t == null) {
            throw invalid(field + " không được để trống.");
        }
        return maxLength(t, field, maxLength);
    }

    /** Trường tùy chọn, có giới hạn độ dài. */
    public static String optional(String value, String field, int maxLength) {
        return maxLength(clean(value), field, maxLength);
    }

    private static String maxLength(String value, String field, int max) {
        if (value != null && value.length() > max) {
            throw invalid(field + " không được dài quá " + max + " ký tự.");
        }
        return value;
    }

    public static String orderId(String value) {
        return matches(value, ORDER_ID, "Order ID");
    }

    public static String accountId(String value) {
        return matches(value, ACCOUNT_ID, "Account ID");
    }

    public static String cityCode(String value) {
        return matches(value, CITY_CODE, "Mã thành phố (3 chữ số)");
    }

    public static String username(String value) {
        return matches(value, USERNAME, "Tên đăng nhập");
    }

    /** Số điện thoại tùy chọn; có giá trị thì phải đúng định dạng. */
    public static String phone(String value) {
        String t = clean(value);
        return t == null ? null : matches(t, PHONE, "Số điện thoại");
    }

    /** Email tùy chọn; có giá trị thì phải đúng định dạng. */
    public static String email(String value) {
        String t = maxLength(clean(value), "Email", 150);
        return t == null ? null : matches(t, EMAIL, "Email");
    }

    private static String matches(String value, Pattern pattern, String field) {
        String t = clean(value);
        if (t == null || !pattern.matcher(t).matches()) {
            throw invalid(field + " không hợp lệ.");
        }
        return t;
    }

    /** Số nguyên bắt buộc. */
    public static int intValue(String value, String field) {
        try {
            return Integer.parseInt(String.valueOf(clean(value)));
        } catch (NumberFormatException e) {
            throw invalid(field + " phải là số nguyên.");
        }
    }

    /** Số nguyên tùy chọn với giá trị mặc định và chặn dưới (dùng cho page, size). */
    public static int intOr(String value, int defaultValue, int min, int max) {
        try {
            int v = Integer.parseInt(String.valueOf(clean(value)));
            return Math.min(Math.max(v, min), max);
        } catch (NumberFormatException e) {
            return defaultValue;
        }
    }

    /** Số thập phân tối đa 2 chữ số sau dấu phẩy. Rỗng trả về 0. */
    public static BigDecimal moneyOrZero(String value, String field) {
        String t = clean(value);
        if (t == null) {
            return BigDecimal.ZERO.setScale(2);
        }
        return money(t, field);
    }

    /** Số tiền bắt buộc, không chấp nhận quá 2 chữ số lẻ hoặc vượt DECIMAL(12,2). */
    public static BigDecimal money(String value, String field) {
        try {
            BigDecimal v = new BigDecimal(String.valueOf(clean(value)));
            if (v.stripTrailingZeros().scale() > 2 || v.abs().compareTo(new BigDecimal("9999999999.99")) > 0) {
                throw invalid(field + " không hợp lệ (tối đa 2 chữ số thập phân).");
            }
            return v.setScale(2);
        } catch (NumberFormatException e) {
            throw invalid(field + " phải là số.");
        }
    }

    /** Số không âm, rỗng trả về 0 (số phút gọi, số giờ dùng). */
    public static BigDecimal nonNegativeOrZero(String value, String field) {
        BigDecimal v = moneyOrZero(value, field);
        if (v.signum() < 0) {
            throw invalid(field + " không được âm.");
        }
        return v;
    }

    /** Ngày bắt buộc, định dạng yyyy-MM-dd. */
    public static LocalDate date(String value, String field) {
        try {
            return LocalDate.parse(String.valueOf(clean(value)));
        } catch (DateTimeParseException e) {
            throw invalid(field + " phải có định dạng yyyy-MM-dd.");
        }
    }

    /** Ngày tùy chọn (bộ lọc); rỗng trả về null. */
    public static LocalDate dateOrNull(String value, String field) {
        return clean(value) == null ? null : date(value, field);
    }
}
