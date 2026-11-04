package com.nexus.dao;

import com.nexus.model.City;
import com.nexus.model.Page;

import java.sql.Connection;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.util.List;
import java.util.Optional;

/** Bản JDBC của {@link CityDao}. */
public class JdbcCityDao implements CityDao {
    private static City map(ResultSet rs) throws SQLException {
        City x = new City();
        x.setId(rs.getInt("CityId"));
        x.setName(rs.getString("CityName"));
        x.setCode(rs.getString("CityCode"));
        x.setStatus(rs.getString("Status"));
        return x;
    }

    /** Tìm theo tên hoặc mã; q rỗng thì lấy tất cả. */
    @Override
    public Page<City> search(Connection c, String q, int page, int size) throws SQLException {
        String pattern = q == null ? "%" : Sql.contains(q);
        String where = " FROM Cities WHERE CityName LIKE ? ESCAPE '\\' OR CityCode LIKE ? ESCAPE '\\'";
        int total = Sql.count(c, "SELECT COUNT(*)" + where, pattern, pattern);
        List<City> items = Sql.list(c, "SELECT *" + where + " ORDER BY CityCode OFFSET ? ROWS FETCH NEXT ? ROWS ONLY",
                JdbcCityDao::map, pattern, pattern, (page - 1) * size, size);
        return new Page<>(items, page, size, total);
    }

    @Override
    public Optional<City> findById(Connection c, int id) throws SQLException {
        return Sql.one(c, "SELECT * FROM Cities WHERE CityId = ?", JdbcCityDao::map, id);
    }

    @Override
    public int insert(Connection c, City x) throws SQLException {
        return Sql.insert(c, "INSERT INTO Cities (CityName, CityCode) VALUES (?,?)", x.getName(), x.getCode());
    }

    @Override
    public int update(Connection c, City x) throws SQLException {
        return Sql.update(c, "UPDATE Cities SET CityName = ?, CityCode = ? WHERE CityId = ?", x.getName(), x.getCode(), x.getId());
    }

    @Override
    public int delete(Connection c, int id) throws SQLException {
        return Sql.update(c, "DELETE FROM Cities WHERE CityId = ?", id);
    }

    @Override
    public int setStatus(Connection c, int id, String status) throws SQLException {
        return Sql.update(c, "UPDATE Cities SET Status = ? WHERE CityId = ?", status, id);
    }

    @Override
    public boolean hasConnections(Connection c, int id) throws SQLException {
        return Sql.count(c, "SELECT COUNT(*) FROM Connections WHERE CityId = ?", id) > 0;
    }

    @Override
    public List<City> listActive(Connection c) throws SQLException {
        return Sql.list(c, "SELECT * FROM Cities WHERE Status = 'ACTIVE' ORDER BY CityName", JdbcCityDao::map);
    }
}
