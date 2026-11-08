package com.nexus.util;

/**
 * Abstraction của transaction để Service không phụ thuộc trực tiếp vào DbUtil tĩnh
 * (nguyên tắc D trong SOLID). Bản chạy thật là {@link DbTxRunner}; unit test dùng bản giả.
 */
public interface TxRunner {
    /** Chạy trong transaction: commit khi xong, rollback khi có lỗi. */
    <T> T inTransaction(DbUtil.TxWork<T> work);

    /** Đọc dữ liệu, không cần transaction. */
    <T> T read(DbUtil.TxWork<T> work);
}
