package com.nexus.dao;

import com.nexus.model.Page;
import com.nexus.model.Payment;

import java.sql.Connection;
import java.sql.SQLException;
import java.time.LocalDate;
import java.util.Optional;

/** Truy cập thanh toán. */
public interface PaymentDao {

    /** Bộ lọc danh sách thanh toán; trường null thì bỏ qua. */
    record Filter(String accountId, Integer billId, LocalDate from, LocalDate to) {
    }

    int insert(Connection c, Payment payment, int employeeId) throws SQLException;

    Optional<Payment> findById(Connection c, int id) throws SQLException;

    Page<Payment> search(Connection c, Filter filter, int page, int size) throws SQLException;
}
