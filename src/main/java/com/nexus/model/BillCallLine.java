package com.nexus.model;

import java.math.BigDecimal;

/** Một dòng cước gọi trên hóa đơn: loại cuộc gọi, số phút, đơn giá, thành tiền. */
public class BillCallLine {
    private final String callType;
    private final BigDecimal minutes;
    private final BigDecimal ratePerMin;
    private final BigDecimal amount;

    public BillCallLine(String callType, BigDecimal minutes, BigDecimal ratePerMin, BigDecimal amount) {
        this.callType = callType;
        this.minutes = minutes;
        this.ratePerMin = ratePerMin;
        this.amount = amount;
    }

    public String getCallType() {
        return callType;
    }

    public BigDecimal getMinutes() {
        return minutes;
    }

    public BigDecimal getRatePerMin() {
        return ratePerMin;
    }

    public BigDecimal getAmount() {
        return amount;
    }
}
