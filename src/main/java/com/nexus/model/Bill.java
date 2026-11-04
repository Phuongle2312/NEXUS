package com.nexus.model;

import java.math.BigDecimal;
import java.time.LocalDate;
import java.util.ArrayList;
import java.util.List;

/** Hóa đơn của một kết nối trong một kỳ cước. */
public class Bill {
    private int id;
    private String accountId;
    private int customerId;
    private String customerName;
    private LocalDate periodStart;
    private LocalDate periodEnd;
    private BigDecimal planFee;
    private BigDecimal callCharges;
    private BigDecimal usageHours;
    private BigDecimal overageCharges;
    private BigDecimal replacementCharges;
    private BigDecimal adjustment;
    private BigDecimal taxableBase;
    private BigDecimal taxRate;
    private BigDecimal taxAmount;
    private BigDecimal depositAmount;
    private BigDecimal totalAmount;
    private BigDecimal paidAmount;
    private BigDecimal dueAmount;
    private BigDecimal previousDue;
    private LocalDate billDate;
    private LocalDate dueDate;
    private String status;
    private String note;
    private List<BillCallLine> callLines = new ArrayList<>();

    /** Cần thanh toán = tổng kỳ này còn nợ + nợ kỳ trước (chỉ để hiển thị, không lưu). */
    public BigDecimal getAmountToPay() {
        return dueAmount.add(previousDue);
    }

    /** Thuế suất dạng phần trăm để hiển thị, ví dụ 12.24. */
    public BigDecimal getTaxPercent() {
        return taxRate.movePointRight(2).stripTrailingZeros();
    }

    public int getId() {
        return id;
    }

    public void setId(int id) {
        this.id = id;
    }

    public String getAccountId() {
        return accountId;
    }

    public void setAccountId(String accountId) {
        this.accountId = accountId;
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

    public LocalDate getPeriodStart() {
        return periodStart;
    }

    public void setPeriodStart(LocalDate periodStart) {
        this.periodStart = periodStart;
    }

    public LocalDate getPeriodEnd() {
        return periodEnd;
    }

    public void setPeriodEnd(LocalDate periodEnd) {
        this.periodEnd = periodEnd;
    }

    public BigDecimal getPlanFee() {
        return planFee;
    }

    public void setPlanFee(BigDecimal planFee) {
        this.planFee = planFee;
    }

    public BigDecimal getCallCharges() {
        return callCharges;
    }

    public void setCallCharges(BigDecimal callCharges) {
        this.callCharges = callCharges;
    }

    public BigDecimal getUsageHours() {
        return usageHours;
    }

    public void setUsageHours(BigDecimal usageHours) {
        this.usageHours = usageHours;
    }

    public BigDecimal getOverageCharges() {
        return overageCharges;
    }

    public void setOverageCharges(BigDecimal overageCharges) {
        this.overageCharges = overageCharges;
    }

    public BigDecimal getReplacementCharges() {
        return replacementCharges;
    }

    public void setReplacementCharges(BigDecimal replacementCharges) {
        this.replacementCharges = replacementCharges;
    }

    public BigDecimal getAdjustment() {
        return adjustment;
    }

    public void setAdjustment(BigDecimal adjustment) {
        this.adjustment = adjustment;
    }

    public BigDecimal getTaxableBase() {
        return taxableBase;
    }

    public void setTaxableBase(BigDecimal taxableBase) {
        this.taxableBase = taxableBase;
    }

    public BigDecimal getTaxRate() {
        return taxRate;
    }

    public void setTaxRate(BigDecimal taxRate) {
        this.taxRate = taxRate;
    }

    public BigDecimal getTaxAmount() {
        return taxAmount;
    }

    public void setTaxAmount(BigDecimal taxAmount) {
        this.taxAmount = taxAmount;
    }

    public BigDecimal getDepositAmount() {
        return depositAmount;
    }

    public void setDepositAmount(BigDecimal depositAmount) {
        this.depositAmount = depositAmount;
    }

    public BigDecimal getTotalAmount() {
        return totalAmount;
    }

    public void setTotalAmount(BigDecimal totalAmount) {
        this.totalAmount = totalAmount;
    }

    public BigDecimal getPaidAmount() {
        return paidAmount;
    }

    public void setPaidAmount(BigDecimal paidAmount) {
        this.paidAmount = paidAmount;
    }

    public BigDecimal getDueAmount() {
        return dueAmount;
    }

    public void setDueAmount(BigDecimal dueAmount) {
        this.dueAmount = dueAmount;
    }

    public BigDecimal getPreviousDue() {
        return previousDue;
    }

    public void setPreviousDue(BigDecimal previousDue) {
        this.previousDue = previousDue;
    }

    public LocalDate getBillDate() {
        return billDate;
    }

    public void setBillDate(LocalDate billDate) {
        this.billDate = billDate;
    }

    public LocalDate getDueDate() {
        return dueDate;
    }

    public void setDueDate(LocalDate dueDate) {
        this.dueDate = dueDate;
    }

    public String getStatus() {
        return status;
    }

    public void setStatus(String status) {
        this.status = status;
    }

    public String getNote() {
        return note;
    }

    public void setNote(String note) {
        this.note = note;
    }

    public List<BillCallLine> getCallLines() {
        return callLines;
    }

    public void setCallLines(List<BillCallLine> callLines) {
        this.callLines = callLines;
    }
}
