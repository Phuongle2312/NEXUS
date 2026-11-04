package com.nexus.service;

import com.nexus.dao.AuditDao;
import com.nexus.dao.ShopDao;
import com.nexus.model.Shop;
import com.nexus.util.TxRunner;
import com.nexus.util.Validator;

import java.util.List;

/** Danh mục cửa hàng bán lẻ (FR-ADM-02). */
public class ShopService extends AbstractCrudService<Shop> {
    private final ShopDao shops;

    public ShopService(TxRunner tx, ShopDao shops, AuditDao audit) {
        super(tx, shops, audit, "RetailShop");
        this.shops = shops;
    }

    @Override
    protected void validate(Shop x) {
        x.setName(Validator.required(x.getName(), "Tên cửa hàng", 150));
        x.setAddress(Validator.required(x.getAddress(), "Địa chỉ", 255));
        x.setPhone(Validator.phone(x.getPhone()));
        if (x.getCityId() <= 0) {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Vui lòng chọn thành phố.");
        }
    }

    @Override
    protected int idOf(Shop x) {
        return x.getId();
    }

    public List<Shop> listActive() {
        return tx.read(shops::listActive);
    }
}
