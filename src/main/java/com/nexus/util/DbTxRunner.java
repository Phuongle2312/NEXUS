package com.nexus.util;

/** Bản chạy thật của {@link TxRunner}, dùng connection pool trong DbUtil. */
public class DbTxRunner implements TxRunner {
    @Override
    public <T> T inTransaction(DbUtil.TxWork<T> work) {
        return DbUtil.inTransaction(work);
    }

    @Override
    public <T> T read(DbUtil.TxWork<T> work) {
        return DbUtil.read(work);
    }
}
