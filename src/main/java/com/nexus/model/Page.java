package com.nexus.model;

import java.util.List;

/** Một trang kết quả. Dùng getter thường vì EL trong JSP không đọc được accessor của record. */
public class Page<T> {
    private final List<T> items;
    private final int page;
    private final int size;
    private final int total;

    public Page(List<T> items, int page, int size, int total) {
        this.items = items;
        this.page = page;
        this.size = size;
        this.total = total;
    }

    public List<T> getItems() {
        return items;
    }

    public int getPage() {
        return page;
    }

    public int getSize() {
        return size;
    }

    public int getTotal() {
        return total;
    }

    public boolean isHasPrevious() {
        return page > 1;
    }

    public boolean isHasNext() {
        return page * size < total;
    }
}
