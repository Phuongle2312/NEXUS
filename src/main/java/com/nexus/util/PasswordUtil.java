package com.nexus.util;

import org.mindrot.jbcrypt.BCrypt;

import java.security.SecureRandom;

/**
 * Băm và kiểm tra mật khẩu bằng BCrypt (FR-AUTH-05), sinh mật khẩu tạm (FR-AUTH-06).
 */
public final class PasswordUtil {
    private static final int BCRYPT_COST = 10;
    private static final int TEMP_LENGTH = 10;
    // Bỏ các ký tự dễ nhầm (0/O, 1/l/I) vì mật khẩu tạm được đọc cho người dùng.
    private static final String TEMP_LETTERS = "abcdefghjkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ";
    private static final String TEMP_DIGITS = "23456789";
    private static final SecureRandom RANDOM = new SecureRandom();

    // Hash giả để so sánh khi không có tài khoản, giúp thời gian phản hồi không lộ username có tồn tại không.
    private static final String DUMMY_HASH = BCrypt.hashpw("nexus-dummy-password", BCrypt.gensalt(BCRYPT_COST));

    private PasswordUtil() {
    }

    public static String hash(String plain) {
        return BCrypt.hashpw(plain, BCrypt.gensalt(BCRYPT_COST));
    }

    /** Kiểm tra mật khẩu; hash rỗng hoặc sai định dạng được coi là không khớp. */
    public static boolean verify(String plain, String hash) {
        if (plain == null || hash == null) {
            return false;
        }
        try {
            return BCrypt.checkpw(plain, hash);
        } catch (IllegalArgumentException e) {
            return false;
        }
    }

    /** Tốn thời gian tương đương một lần verify, dùng khi username không tồn tại. */
    public static void burnVerifyTime(String plain) {
        verify(plain == null ? "" : plain, DUMMY_HASH);
    }

    /** Chính sách mật khẩu: tối thiểu 8 ký tự, có cả chữ và số. BCrypt chỉ dùng 72 byte đầu nên chặn trên. */
    public static boolean isStrongEnough(String plain) {
        return plain != null
                && plain.length() >= 8
                && plain.length() <= 72
                && plain.chars().anyMatch(Character::isLetter)
                && plain.chars().anyMatch(Character::isDigit);
    }

    /** Sinh mật khẩu tạm đạt chính sách: luôn có chữ và số. */
    public static String generateTemporary() {
        StringBuilder sb = new StringBuilder(TEMP_LENGTH);
        sb.append(TEMP_DIGITS.charAt(RANDOM.nextInt(TEMP_DIGITS.length())));
        String all = TEMP_LETTERS + TEMP_DIGITS;
        sb.append(TEMP_LETTERS.charAt(RANDOM.nextInt(TEMP_LETTERS.length())));
        while (sb.length() < TEMP_LENGTH) {
            sb.append(all.charAt(RANDOM.nextInt(all.length())));
        }
        return sb.toString();
    }
}
