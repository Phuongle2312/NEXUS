package com.nexus.dao;

import com.nexus.model.Page;
import com.nexus.model.Shop;

import java.sql.Connection;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.util.List;
import java.util.Optional;

/** Bản JDBC của {@link ShopDao}. */
public class JdbcShopDao implements ShopDao {
    private static final String FROM = " FROM RetailShops s JOIN Cities c ON c.CityId = s.CityId";
    private static final String COLUMNS = "SELECT s.*, c.CityName";

    private static Shop map(ResultSet rs) throws SQLException {
        Shop x = new Shop();
        x.setId(rs.getInt("ShopId"));
        x.setName(rs.getString("ShopName"));
        x.setAddress(rs.getString("Address"));
        x.setCityId(rs.getInt("CityId"));
        x.setCityName(rs.getString("CityName"));
        x.setPhone(rs.getString("Phone"));
        x.setStatus(rs.getString("Status"));
        return x;
    }

    /** Tìm theo tên cửa hàng, địa chỉ hoặc thành phố. */
    @Override
    public Page<Shop> search(Connection c, String q, int page, int size) throws SQLException {
        String p = q == null ? "%" : Sql.contains(q);
        String where = FROM + " WHERE s.ShopName LIKE ? ESCAPE '\\' OR s.Address LIKE ? ESCAPE '\\' OR c.CityName LIKE ? ESCAPE '\\'";
        int total = Sql.count(c, "SELECT COUNT(*)" + where, p, p, p);
        List<Shop> items = Sql.list(c, COLUMNS + where + " ORDER BY s.ShopName OFFSET ? ROWS FETCH NEXT ? ROWS ONLY",
                JdbcShopDao::map, p, p, p, (page - 1) * size, size);
        return new Page<>(items, page, size, total);
    }

    @Override
    public Optional<Shop> findById(Connection c, int id) throws SQLException {
        return Sql.one(c, COLUMNS + FROM + " WHERE s.ShopId = ?", JdbcShopDao::map, id);
    }

    @Override
    public int insert(Connection c, Shop x) throws SQLException {
        return Sql.insert(c, "INSERT INTO RetailShops (ShopName, Address, CityId, Phone) VALUES (?,?,?,?)",
                x.getName(), x.getAddress(), x.getCityId(), x.getPhone());
    }

    @Override
    public int update(Connection c, Shop x) throws SQLException {
        return Sql.update(c, "UPDATE RetailShops SET ShopName = ?, Address = ?, CityId = ?, Phone = ? WHERE ShopId = ?",
                x.getName(), x.getAddress(), x.getCityId(), x.getPhone(), x.getId());
    }

    @Override
    public int delete(Connection c, int id) throws SQLException {
        return Sql.update(c, "DELETE FROM RetailShops WHERE ShopId = ?", id);
    }

    @Override
    public int setStatus(Connection c, int id, String status) throws SQLException {
        return Sql.update(c, "UPDATE RetailShops SET Status = ? WHERE ShopId = ?", status, id);
    }

    @Override
    public List<Shop> listActive(Connection c) throws SQLException {
        return Sql.list(c, COLUMNS + FROM + " WHERE s.Status = 'ACTIVE' ORDER BY s.ShopName", JdbcShopDao::map);
    }
}
