package com.nexus.service;

import com.nexus.dao.AuditDao;
import com.nexus.dao.CityDao;
import com.nexus.model.City;
import com.nexus.util.TxRunner;
import com.nexus.util.Validator;

import java.sql.Connection;
import java.sql.SQLException;
import java.util.List;

/** Danh mục thành phố (FR-ADM-07). */
public class CityService extends AbstractCrudService<City> {
    private final CityDao cities;

    public CityService(TxRunner tx, CityDao cities, AuditDao audit) {
        super(tx, cities, audit, "City");
        this.cities = cities;
    }

    @Override
    protected void validate(City x) {
        x.setName(Validator.required(x.getName(), "Tên thành phố", 100));
        x.setCode(Validator.cityCode(x.getCode()));
    }

    @Override
    protected int idOf(City x) {
        return x.getId();
    }

    /** Account ID bất biến sau khi cấp, nên thành phố đã có kết nối không được đổi mã. */
    @Override
    protected void beforeUpdate(Connection c, City x) throws SQLException {
        City current = cities.findById(c, x.getId()).orElseThrow(this::notFound);
        if (!current.getCode().equals(x.getCode()) && cities.hasConnections(c, x.getId())) {
            throw new BusinessException(ErrorCode.IN_USE, "Thành phố đã có kết nối nên không được đổi mã.");
        }
    }

    public List<City> listActive() {
        return tx.read(cities::listActive);
    }
}
