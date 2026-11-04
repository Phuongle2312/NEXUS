package com.nexus.dao;

import com.nexus.model.City;

import java.sql.Connection;
import java.sql.SQLException;
import java.util.List;

/** Danh mục thành phố. */
public interface CityDao extends CrudDao<City> {
    /** Đã cấp Account ID trong thành phố này chưa (khi đó không được đổi mã). */
    boolean hasConnections(Connection c, int id) throws SQLException;

    /** Danh sách cho ô chọn trên form. */
    List<City> listActive(Connection c) throws SQLException;
}
