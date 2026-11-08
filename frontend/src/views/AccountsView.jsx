import React, { useState } from 'react';
import { TAX_RATE } from '../data/mockData.js';
import { FileText, DollarSign, AlertTriangle, Plus, Printer, CheckCircle } from 'lucide-react';

export default function AccountsView({ bills, connections, onAddBill, onCollectPayment }) {
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [selectedReceipt, setSelectedReceipt] = useState(null);

  // Form states
  const [accountId, setAccountId] = useState(connections[0]?.accountId || 'B001000000000001');
  const [planFee, setPlanFee] = useState(350);
  const [usageFee, setUsageFee] = useState(25);
  const [replaceFee, setReplaceFee] = useState(0);
  const [prevDue, setPrevDue] = useState(0);

  const taxableAmount = planFee + usageFee + replaceFee;
  const taxAmount = taxableAmount * TAX_RATE;
  const totalThisCycle = taxableAmount + taxAmount;

  const handleCreateBill = (e) => {
    e.preventDefault();
    const targetConn = connections.find(c => c.accountId === accountId);
    const newBill = {
      billId: `INV-202611-${(bills.length + 1).toString().padStart(3, '0')}`,
      accountId,
      customerName: targetConn ? targetConn.customerName : 'Khách Hàng',
      billingCycle: '11/2026 - 12/2026',
      planFee,
      usageOverdue: usageFee,
      replacementCharge: replaceFee,
      taxableAmount,
      taxAmount,
      depositAdjusted: 0,
      totalThisCycle,
      previousDue: prevDue,
      paidAmount: 0,
      dueBalance: totalThisCycle + prevDue,
      status: 'UNPAID',
      issuedDate: new Date().toISOString().split('T')[0]
    };

    onAddBill(newBill);
    setShowCreateModal(false);
    alert(`Đã phát hành thành công hóa đơn ${newBill.billId} cho ${newBill.customerName}!`);
  };

  return (
    <div>
      <div className="flex-between" style={{ marginBottom: '1.5rem' }}>
        <div>
          <h2 style={{ fontSize: '1.5rem', fontWeight: 800, color: 'var(--text-primary)' }}>
            📊 Ban Tài Chính - Kế Toán (Accounts & Billing)
          </h2>
          <p className="form-hint">Lập hóa đơn cước tự động, tính thuế giá trị dịch vụ 12.24% và theo dõi công nợ</p>
        </div>
        <button className="btn btn-primary" onClick={() => setShowCreateModal(true)}>
          <Plus size={18} /> Lập Hóa Đơn Cước Mới
        </button>
      </div>

      {/* KPI Cards */}
      <div className="stats-grid">
        <div className="stat-card">
          <div className="stat-icon blue"><FileText size={24} /></div>
          <div className="stat-info">
            <div className="stat-value">
              ${bills.reduce((sum, b) => sum + b.totalThisCycle, 0).toFixed(2)}
            </div>
            <div className="stat-label">Tổng cước phát hành kỳ này</div>
          </div>
        </div>
        <div className="stat-card">
          <div className="stat-icon green"><DollarSign size={24} /></div>
          <div className="stat-info">
            <div className="stat-value">
              ${bills.reduce((sum, b) => sum + b.paidAmount, 0).toFixed(2)}
            </div>
            <div className="stat-label">Thực thu vào tài khoản</div>
          </div>
        </div>
        <div className="stat-card">
          <div className="stat-icon orange"><AlertTriangle size={24} /></div>
          <div className="stat-info">
            <div className="stat-value" style={{ color: 'var(--danger)' }}>
              ${bills.filter(b => b.status === 'OVERDUE').reduce((sum, b) => sum + b.dueBalance, 0).toFixed(2)}
            </div>
            <div className="stat-label">Nợ cước quá hạn cần xử lý</div>
          </div>
        </div>
      </div>

      {/* Bills Table */}
      <div className="nexus-card">
        <div className="nexus-card-header">
          <h3 className="nexus-card-title">Hóa Đơn Cước Dịch Vụ Viễn Thông & Internet</h3>
        </div>
        <div className="table-responsive">
          <table className="nexus-table">
            <thead>
              <tr>
                <th>Số Hóa Đơn</th>
                <th>Account ID</th>
                <th>Khách Hàng</th>
                <th>Kỳ Cước</th>
                <th>Cước Gói</th>
                <th>Vượt Mức/Gọi</th>
                <th>Thuế (12.24%)</th>
                <th>Tổng Hóa Đơn</th>
                <th>Đã Trả</th>
                <th>Còn Nợ</th>
                <th>Trạng Thái</th>
                <th>Thao Tác</th>
              </tr>
            </thead>
            <tbody>
              {bills.map(b => (
                <tr key={b.billId}>
                  <td><strong className="font-mono" style={{ color: 'var(--primary)' }}>{b.billId}</strong></td>
                  <td><code className="font-mono">{b.accountId}</code></td>
                  <td><strong>{b.customerName}</strong></td>
                  <td>{b.billingCycle}</td>
                  <td>${b.planFee.toFixed(2)}</td>
                  <td>${(b.usageOverdue + b.replacementCharge).toFixed(2)}</td>
                  <td>${b.taxAmount.toFixed(2)}</td>
                  <td><strong>${b.totalThisCycle.toFixed(2)}</strong></td>
                  <td>${b.paidAmount.toFixed(2)}</td>
                  <td>
                    <strong style={{ color: b.dueBalance > 0 ? 'var(--danger)' : 'var(--success)' }}>
                      ${b.dueBalance.toFixed(2)}
                    </strong>
                  </td>
                  <td>
                    <span className={`badge ${
                      b.status === 'PAID' ? 'badge-success' :
                      b.status === 'OVERDUE' ? 'badge-danger' : 'badge-warning'
                    }`}>
                      {b.status}
                    </span>
                  </td>
                  <td>
                    <div className="flex-gap-2">
                      {b.status !== 'PAID' && (
                        <button
                          className="btn btn-success btn-sm"
                          onClick={() => {
                            onCollectPayment(b.billId);
                            alert(`Đã ghi nhận thanh toán thành công cho hóa đơn ${b.billId}!`);
                          }}
                        >
                          <CheckCircle size={14} /> Thu Cước
                        </button>
                      )}
                      <button className="btn btn-outline btn-sm" onClick={() => setSelectedReceipt(b)}>
                        <Printer size={14} /> Biên Lai
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Create Bill Modal */}
      {showCreateModal && (
        <div className="modal-overlay">
          <div className="modal-content">
            <div className="modal-header">
              <h3 style={{ fontSize: '1.15rem', fontWeight: 700 }}>🧾 Lập Hóa Đơn Cước Mới Cho Thuê Bao</h3>
              <button className="btn btn-outline btn-sm" onClick={() => setShowCreateModal(false)}>✕</button>
            </div>
            <form onSubmit={handleCreateBill}>
              <div className="modal-body">
                <div className="form-group">
                  <label className="form-label">Chọn Tài Khoản Kết Nối (Account ID) <span className="req">*</span></label>
                  <select
                    className="form-select"
                    value={accountId}
                    onChange={(e) => setAccountId(e.target.value)}
                  >
                    {connections.map(c => (
                      <option key={c.accountId} value={c.accountId}>
                        {c.accountId} - {c.customerName} ({c.planName})
                      </option>
                    ))}
                  </select>
                </div>

                <div className="grid-2">
                  <div className="form-group">
                    <label className="form-label">Cước thuê bao gói cố định ($) <span className="req">*</span></label>
                    <input
                      type="number"
                      step="0.01"
                      className="form-control"
                      value={planFee}
                      onChange={(e) => setPlanFee(parseFloat(e.target.value) || 0)}
                      required
                    />
                  </div>
                  <div className="form-group">
                    <label className="form-label">Cước vượt giờ / Cước cuộc gọi ($)</label>
                    <input
                      type="number"
                      step="0.01"
                      className="form-control"
                      value={usageFee}
                      onChange={(e) => setUsageFee(parseFloat(e.target.value) || 0)}
                    />
                  </div>
                </div>

                <div className="grid-2">
                  <div className="form-group">
                    <label className="form-label">Phí đổi thiết bị phát sinh ($)</label>
                    <input
                      type="number"
                      step="0.01"
                      className="form-control"
                      value={replaceFee}
                      onChange={(e) => setReplaceFee(parseFloat(e.target.value) || 0)}
                    />
                  </div>
                  <div className="form-group">
                    <label className="form-label">Nợ kỳ trước mang sang ($)</label>
                    <input
                      type="number"
                      step="0.01"
                      className="form-control"
                      value={prevDue}
                      onChange={(e) => setPrevDue(parseFloat(e.target.value) || 0)}
                    />
                  </div>
                </div>

                {/* Realtime calculation box */}
                <div style={{ background: '#f8fafc', border: '1px solid var(--border-color)', borderRadius: 'var(--radius)', padding: '14px', marginTop: '1rem' }}>
                  <div className="flex-between" style={{ fontSize: '0.85rem', marginBottom: '4px' }}>
                    <span>Phần cước chịu thuế:</span>
                    <span style={{ fontWeight: 700 }}>${taxableAmount.toFixed(2)}</span>
                  </div>
                  <div className="flex-between" style={{ fontSize: '0.85rem', marginBottom: '6px', color: '#0369a1' }}>
                    <span>Thuế dịch vụ viễn thông nhà nước (12.24%):</span>
                    <span style={{ fontWeight: 700 }}>${taxAmount.toFixed(2)}</span>
                  </div>
                  <div className="flex-between" style={{ fontSize: '1.05rem', borderTop: '1px dashed var(--border-color)', paddingTop: '8px', fontWeight: 800, color: 'var(--primary)' }}>
                    <span>Tổng hóa đơn phát hành kỳ này:</span>
                    <span>${totalThisCycle.toFixed(2)}</span>
                  </div>
                </div>
              </div>
              <div className="modal-footer">
                <button type="button" className="btn btn-secondary" onClick={() => setShowCreateModal(false)}>
                  Hủy
                </button>
                <button type="submit" className="btn btn-primary">
                  Xác Nhận Phát Hành
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Receipt Modal */}
      {selectedReceipt && (
        <div className="modal-overlay">
          <div className="modal-content" style={{ maxWidth: '520px' }}>
            <div className="modal-header">
              <h3 style={{ fontSize: '1.15rem', fontWeight: 700 }}>📄 Hóa Đơn & Biên Lai Thu Phí</h3>
              <button className="btn btn-outline btn-sm" onClick={() => setSelectedReceipt(null)}>✕</button>
            </div>
            <div className="modal-body">
              <div style={{ background: 'white', border: '1px dashed #cbd5e1', borderRadius: 'var(--radius)', padding: '2rem' }}>
                <div style={{ textAlign: 'center', marginBottom: '1.5rem', borderBottom: '1px dashed #cbd5e1', paddingBottom: '1rem' }}>
                  <h2 style={{ fontSize: '1.25rem', fontWeight: 800, color: 'var(--primary)' }}>NEXUS COMMUNICATION SYSTEM</h2>
                  <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginTop: '2px' }}>DỊCH VỤ VIỄN THÔNG & INTERNET</div>
                  <div style={{ marginTop: '8px', fontWeight: 700 }}>HÓA ĐƠN CƯỚC SỐ: {selectedReceipt.billId}</div>
                  <div style={{ fontSize: '0.775rem', color: 'var(--text-muted)' }}>Ngày phát hành: {selectedReceipt.issuedDate}</div>
                </div>

                <div className="flex-between" style={{ marginBottom: '8px', fontSize: '0.9rem' }}>
                  <span className="text-muted">Chủ thuê bao:</span>
                  <strong>{selectedReceipt.customerName}</strong>
                </div>
                <div className="flex-between" style={{ marginBottom: '8px', fontSize: '0.9rem' }}>
                  <span className="text-muted">Account ID:</span>
                  <code className="font-mono">{selectedReceipt.accountId}</code>
                </div>
                <div className="flex-between" style={{ marginBottom: '8px', fontSize: '0.9rem' }}>
                  <span className="text-muted">Kỳ thanh toán:</span>
                  <span>{selectedReceipt.billingCycle}</span>
                </div>
                <hr style={{ border: 'none', borderTop: '1px dashed #cbd5e1', margin: '10px 0' }} />

                <div className="flex-between" style={{ marginBottom: '8px', fontSize: '0.9rem' }}>
                  <span>Cước gói thuê bao:</span>
                  <span>${selectedReceipt.planFee.toFixed(2)}</span>
                </div>
                <div className="flex-between" style={{ marginBottom: '8px', fontSize: '0.9rem' }}>
                  <span>Cước vượt mức / cuộc gọi:</span>
                  <span>${selectedReceipt.usageOverdue.toFixed(2)}</span>
                </div>
                {selectedReceipt.replacementCharge > 0 && (
                  <div className="flex-between" style={{ marginBottom: '8px', fontSize: '0.9rem' }}>
                    <span>Phí thay thế thiết bị hỏng:</span>
                    <span>${selectedReceipt.replacementCharge.toFixed(2)}</span>
                  </div>
                )}
                <div className="flex-between" style={{ marginBottom: '8px', fontSize: '0.9rem', color: 'var(--text-muted)' }}>
                  <span>Thuế dịch vụ viễn thông (12.24%):</span>
                  <span>${selectedReceipt.taxAmount.toFixed(2)}</span>
                </div>
                {selectedReceipt.previousDue > 0 && (
                  <div className="flex-between" style={{ marginBottom: '8px', fontSize: '0.9rem', color: 'var(--danger)' }}>
                    <span>Nợ tồn kỳ trước:</span>
                    <span>+${selectedReceipt.previousDue.toFixed(2)}</span>
                  </div>
                )}

                <div className="flex-between" style={{ borderTop: '2px solid var(--text-primary)', marginTop: '1rem', paddingTop: '0.75rem', fontWeight: 800, fontSize: '1.1rem' }}>
                  <span>TỔNG CỘNG:</span>
                  <span style={{ color: 'var(--primary)' }}>
                    ${(selectedReceipt.totalThisCycle + selectedReceipt.previousDue).toFixed(2)}
                  </span>
                </div>

                <div className="flex-between" style={{ marginTop: '8px', fontSize: '0.9rem' }}>
                  <span className="text-muted">Đã thanh toán:</span>
                  <strong style={{ color: 'var(--success)' }}>${selectedReceipt.paidAmount.toFixed(2)}</strong>
                </div>
                <div className="flex-between" style={{ marginTop: '4px', fontSize: '0.9rem' }}>
                  <span className="text-muted">Còn phải trả:</span>
                  <strong style={{ color: selectedReceipt.dueBalance > 0 ? 'var(--danger)' : 'var(--text-primary)' }}>
                    ${selectedReceipt.dueBalance.toFixed(2)}
                  </strong>
                </div>
              </div>
            </div>
            <div className="modal-footer">
              <button className="btn btn-secondary" onClick={() => setSelectedReceipt(null)}>Đóng</button>
              <button className="btn btn-primary" onClick={() => window.print()}>
                <Printer size={16} /> In Hóa Đơn
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
