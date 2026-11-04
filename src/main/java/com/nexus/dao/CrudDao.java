package com.nexus.dao;

import com.nexus.model.Page;

import java.sql.Connection;
import java.sql.SQLException;
import java.util.Optional;

/** Các thao tác chung của một danh mục Admin. C dùng lại cho vendors, products, plans. */
public interface CrudDao<T> {
    Page<T> search(Connection c, String q, int page, int size) throws SQLException;

    Optional<T> findById(Connection c, int id) throws SQLException;

    /** Trả về khóa vừa sinh. */
    int insert(Connection c, T item) throws SQLException;

    int update(Connection c, T item) throws SQLException;

    /** Xóa cứng; vướng khóa ngoại thì Sql ném IN_USE. */
    int delete(Connection c, int id) throws SQLException;

    int setStatus(Connection c, int id, String status) throws SQLException;
}
