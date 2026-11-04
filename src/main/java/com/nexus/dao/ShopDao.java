package com.nexus.dao;

import com.nexus.model.Shop;

import java.sql.Connection;
import java.sql.SQLException;
import java.util.List;

/** Danh mục cửa hàng. */
public interface ShopDao extends CrudDao<Shop> {
    /** Danh sách cho ô chọn trên form. */
    List<Shop> listActive(Connection c) throws SQLException;
}
