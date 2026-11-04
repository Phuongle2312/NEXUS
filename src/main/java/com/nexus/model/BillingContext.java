package com.nexus.model;

import java.math.BigDecimal;
import java.time.LocalDate;

/**
 * Thông tin kết nối cần để lập hóa đơn, gộp từ Connections, Orders, Plans, Customers.
 * listPlanFee và listDeposit là giá TRƯỚC ưu đãi chụp trên đơn; với landline thuộc đơn Dial-up
 * thì lấy các cột Landline* của đơn.
 */
public class BillingContext {
    private String accountId;
    private String connType;
    private String status;
    private int customerId;
    private String customerName;
    private int planId;
    private String planName;
    private int validityMonths;
    private Integer hoursIncluded;
    private BigDecimal overageRatePerHour;
    private BigDecimal listPlanFee;
    private BigDecimal listDeposit;
    private BigDecimal discountPercent;
    private LocalDate activationDate;
    private boolean depositRefunded;
    private boolean suspendRequested;

    public String getAccountId() {
        return accountId;
    }

    public void setAccountId(String accountId) {
        this.accountId = accountId;
    }

    public String getConnType() {
        return connType;
    }

    public void setConnType(String connType) {
        this.connType = connType;
    }

    public String getStatus() {
        return status;
    }

    public void setStatus(String status) {
        this.status = status;
    }

    public int getCustomerId() {
        return customerId;
    }

    public void setCustomerId(int customerId) {
        this.customerId = customerId;
    }

    public String getCustomerName() {
        return customerName;
    }

    public void setCustomerName(String customerName) {
        this.customerName = customerName;
    }

    public int getPlanId() {
        return planId;
    }

    public void setPlanId(int planId) {
        this.planId = planId;
    }

    public String getPlanName() {
        return planName;
    }

    public void setPlanName(String planName) {
        this.planName = planName;
    }

    public int getValidityMonths() {
        return validityMonths;
    }

    public void setValidityMonths(int validityMonths) {
        this.validityMonths = validityMonths;
    }

    public Integer getHoursIncluded() {
        return hoursIncluded;
    }

    public void setHoursIncluded(Integer hoursIncluded) {
        this.hoursIncluded = hoursIncluded;
    }

    public BigDecimal getOverageRatePerHour() {
        return overageRatePerHour;
    }

    public void setOverageRatePerHour(BigDecimal overageRatePerHour) {
        this.overageRatePerHour = overageRatePerHour;
    }

    public BigDecimal getListPlanFee() {
        return listPlanFee;
    }

    public void setListPlanFee(BigDecimal listPlanFee) {
        this.listPlanFee = listPlanFee;
    }

    public BigDecimal getListDeposit() {
        return listDeposit;
    }

    public void setListDeposit(BigDecimal listDeposit) {
        this.listDeposit = listDeposit;
    }

    public BigDecimal getDiscountPercent() {
        return discountPercent;
    }

    public void setDiscountPercent(BigDecimal discountPercent) {
        this.discountPercent = discountPercent;
    }

    public LocalDate getActivationDate() {
        return activationDate;
    }

    public void setActivationDate(LocalDate activationDate) {
        this.activationDate = activationDate;
    }

    public boolean isDepositRefunded() {
        return depositRefunded;
    }

    public void setDepositRefunded(boolean depositRefunded) {
        this.depositRefunded = depositRefunded;
    }

    public boolean isSuspendRequested() {
        return suspendRequested;
    }

    public void setSuspendRequested(boolean suspendRequested) {
        this.suspendRequested = suspendRequested;
    }
}
