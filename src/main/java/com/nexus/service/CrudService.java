package com.nexus.service;

import com.nexus.model.Page;
import com.nexus.model.SessionUser;

/** Hợp đồng nghiệp vụ của một danh mục Admin; BaseAdminServlet chỉ phụ thuộc vào interface này. */
public interface CrudService<T> {
    Page<T> search(String q, int page, int size);

    /** Không tìm thấy thì ném NOT_FOUND. */
    T get(int id);

    /** Kiểm tra dữ liệu rồi tạo mới; trả về khóa. */
    int create(SessionUser actor, T item);

    void update(SessionUser actor, T item);

    /** Vướng dữ liệu liên quan thì ném IN_USE (409); khi đó dùng deactivate. */
    void delete(SessionUser actor, int id);

    void deactivate(SessionUser actor, int id);
}
