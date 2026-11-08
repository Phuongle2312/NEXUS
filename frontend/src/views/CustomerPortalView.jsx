import React, { useState } from 'react';
import { Radio, FileText, Shield, Send, CheckCircle, Printer, Download, Clock } from 'lucide-react';

export default function CustomerPortalView({ connections, bills, orders }) {
  const [activeTab, setActiveTab] = useState('CONNECTIONS'); // CONNECTIONS | BILLS | ORDERS | SUPPORT
  const [selectedBill, setSelectedBill] = useState(null);
  const [ticketSent, setTicketSent] = useState(false);

  const customerName = 'Công ty Cổ phần Công nghệ Alpha';
  const customerId = 'CUST-ALPHA-2026';
  const myConns = connections.filter(c => c.customerName.includes('Alpha'));
  const myBills = bills.filter(b => b.customerName.includes('Alpha'));
  const myOrders = orders.filter(o => o.customerName.includes('Alpha'));

  const handleTicketSubmit = (e) => {
    e.preventDefault();
    setTicketSent(true);
    setTimeout(() => setTicketSent(false), 5000);
    e.target.reset();
  };

  return (
    <div>
      {/* Customer Profile Banner */}
      <div className="nexus-card" style={{ marginBottom: '1.5rem', background: 'linear-gradient(to right, #ffffff, #f0f9ff)', borderColor: '#bae6fd' }}>
        <div className="flex-between">
          <div style={{ display: 'flex', gap: '1.25rem', alignItems: 'center' }}>
            <div style={{ width: '60px', height: '60px', borderRadius: 'var(--radius)', background: 'var(--primary)', color: 'white', display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: '1.75rem', fontWeight: 800 }}>
              🏢
            </div>
            <div>
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                <h2 style={{ fontSize: '1.4rem', fontWeight: 800, color: 'var(--text-primary)' }}>{customerName}</h2>
                <span className="badge badge-success" style={{ padding: '4px 10px', fontSize: '0.75rem' }}>
                  Khách Hàng Doanh Nghiệp (VIP)
                </span>
              </div>
              <div style={{ fontSize: '0.85rem', color: 'var(--text-secondary)', marginTop: '4px' }}>
                Mã Khách Hàng: <code className="font-mono" style={{ color: 'var(--primary)', fontWeight: 700 }}>{customerId}</code> • Hotline đại diện: <strong>0901234567</strong> • Địa bàn: Hà Nội
              </div>
            </div>
          </div>
          <button className="btn btn-outline btn-sm" onClick={() => alert('Thông tin tài khoản khách hàng đã được đồng bộ với hệ thống Nexus.')}>
            Cập Nhật Hồ Sơ
          </button>
        </div>
      </div>

      {/* Quick Summary Cards */}
      <div className="stats-grid">
        <div className="stat-card" style={{ cursor: 'pointer' }} onClick={() => setActiveTab('CONNECTIONS')}>
          <div className="stat-icon blue"><Radio size={24} /></div>
          <div className="stat-info">
            <div className="stat-value">{myConns.length}</div>
            <div className="stat-label">Đường truyền đang hoạt động</div>
          </div>
        </div>
        <div className="stat-card" style={{ cursor: 'pointer' }} onClick={() => setActiveTab('BILLS')}>
          <div className="stat-icon green"><FileText size={24} /></div>
          <div className="stat-info">
            <div className="stat-value">${myBills.reduce((s, b) => s + b.totalThisCycle, 0).toFixed(2)}</div>
            <div className="stat-label">Tổng cước kỳ phát hành</div>
          </div>
        </div>
        <div className="stat-card" style={{ cursor: 'pointer' }} onClick={() => setActiveTab('ORDERS')}>
          <div className="stat-icon purple"><Clock size={24} /></div>
          <div className="stat-info">
            <div className="stat-value">{myOrders.length}</div>
            <div className="stat-label">Hợp đồng đăng ký dịch vụ</div>
          </div>
        </div>
        <div className="stat-card">
          <div className="stat-icon orange"><Shield size={24} /></div>
          <div className="stat-info">
            <div className="stat-value">${(myConns.length * 500 * 0.75).toLocaleString()}</div>
            <div className="stat-label">Tiền đặt cọc bảo đảm tại Nexus</div>
          </div>
        </div>
      </div>

      {/* Tabs */}
      <div className="service-nav-tabs">
        <button
          className={`service-tab-btn ${activeTab === 'CONNECTIONS' ? 'active' : ''}`}
          onClick={() => setActiveTab('CONNECTIONS')}
        >
          📡 Kết Nối & Thiết Bị Của Tôi ({myConns.length})
        </button>
        <button
          className={`service-tab-btn ${activeTab === 'BILLS' ? 'active' : ''}`}
          onClick={() => setActiveTab('BILLS')}
        >
          🧾 Hóa Đơn & Thanh Toán ({myBills.length})
        </button>
        <button
          className={`service-tab-btn ${activeTab === 'ORDERS' ? 'active' : ''}`}
          onClick={() => setActiveTab('ORDERS')}
        >
          📋 Tiến Độ Đơn Đặt Hàng ({myOrders.length})
        </button>
        <button
          className={`service-tab-btn ${activeTab === 'SUPPORT' ? 'active' : ''}`}
          onClick={() => setActiveTab('SUPPORT')}
        >
          🆘 Hỗ Trợ & Khiếu Nại Kỹ Thuật
        </button>
      </div>

      {/* Tab 1: Kết nối */}
      {activeTab === 'CONNECTIONS' && (
        <div className="nexus-card">
          <div className="nexus-card-header">
            <h3 className="nexus-card-title">Danh Sách Đường Truyền Kết Nối (Account ID)</h3>
            <span className="form-hint">Mã Account ID 16 ký tự dùng để thanh toán cước và báo hỏng</span>
          </div>
          <div className="table-responsive">
            <table className="nexus-table">
              <thead>
                <tr>
                  <th>Mã Account ID (16 số)</th>
                  <th>Gói Cước Đang Dùng</th>
                  <th>Thiết Bị Cấp Kèm</th>
                  <th>Địa Chỉ Lắp Đặt</th>
                  <th>Ngày Kích Hoạt</th>
                  <th>Trạng Thái</th>
                </tr>
              </thead>
              <tbody>
                {myConns.map(c => (
                  <tr key={c.accountId}>
                    <td><strong className="font-mono" style={{ color: 'var(--primary)', fontSize: '0.95rem' }}>{c.accountId}</strong></td>
                    <td>
                      <span className="badge badge-info">{c.service}</span>
                      <strong style={{ marginLeft: '6px' }}>{c.planName}</strong>
                    </td>
                    <td><code className="font-mono">{c.equipmentSerial}</code></td>
                    <td style={{ fontSize: '0.85rem' }}>{c.installationAddress}</td>
                    <td>{c.activatedAt}</td>
                    <td>
                      <span className={`badge ${c.status === 'ACTIVE' ? 'badge-success' : 'badge-danger'}`}>
                        {c.status}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Tab 2: Hóa đơn */}
      {activeTab === 'BILLS' && (
        <div className="nexus-card">
          <div className="nexus-card-header">
            <h3 className="nexus-card-title">Tra Cứu Hóa Đơn & Lịch Sử Đóng Cước</h3>
            <span className="form-hint">Thuế dịch vụ viễn thông 12.24% đã tính trong tổng hóa đơn</span>
          </div>
          <div className="table-responsive">
            <table className="nexus-table">
              <thead>
                <tr>
                  <th>Số Hóa Đơn</th>
                  <th>Account ID</th>
                  <th>Kỳ Cước</th>
                  <th>Cước Thuê Bao</th>
                  <th>Cước Vượt Hạn Mức</th>
                  <th>Thuế (12.24%)</th>
                  <th>Tổng Hóa Đơn</th>
                  <th>Tình Trạng</th>
                  <th>Biên Lai</th>
                </tr>
              </thead>
              <tbody>
                {myBills.map(b => (
                  <tr key={b.billId}>
                    <td><strong className="font-mono" style={{ color: 'var(--primary)' }}>{b.billId}</strong></td>
                    <td><code className="font-mono">{b.accountId}</code></td>
                    <td>{b.billingCycle}</td>
                    <td>${b.planFee.toFixed(2)}</td>
                    <td>${b.usageOverdue.toFixed(2)}</td>
                    <td>${b.taxAmount.toFixed(2)}</td>
                    <td><strong style={{ fontSize: '1rem' }}>${b.totalThisCycle.toFixed(2)}</strong></td>
                    <td>
                      <span className={`badge ${b.status === 'PAID' ? 'badge-success' : 'badge-danger'}`}>
                        {b.status}
                      </span>
                    </td>
                    <td>
                      <button className="btn btn-outline btn-sm" onClick={() => setSelectedBill(b)}>
                        <Printer size={14} /> Xem / In
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Tab 3: Đơn hàng */}
      {activeTab === 'ORDERS' && (
        <div className="nexus-card">
          <div className="nexus-card-header">
            <h3 className="nexus-card-title">Hợp Đồng & Đơn Đăng Ký Dịch Vụ Của Bạn</h3>
          </div>
          <div className="table-responsive">
            <table className="nexus-table">
              <thead>
                <tr>
                  <th>Mã Order ID</th>
                  <th>Dịch Vụ & Gói</th>
                  <th>Số Lượng Kết Nối</th>
                  <th>Mức Chiết Khấu</th>
                  <th>Tiền Đặt Cọc Đã Đóng</th>
                  <th>Ngày Ký Đơn</th>
                  <th>Tiến Độ Thi Công</th>
                </tr>
              </thead>
              <tbody>
                {myOrders.map(o => (
                  <tr key={o.orderId}>
                    <td><strong className="font-mono" style={{ color: 'var(--primary)' }}>{o.orderId}</strong></td>
                    <td><strong>{o.planName}</strong></td>
                    <td><strong>{o.quantity}</strong> đường</td>
                    <td><span className="badge badge-success">-{o.discountPercent}%</span></td>
                    <td><strong>${o.totalDeposit.toLocaleString()}</strong></td>
                    <td>{o.createdAt}</td>
                    <td>
                      <span className="badge badge-success">✓ {o.status}</span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Tab 4: Khiếu nại & Hỗ trợ kỹ thuật */}
      {activeTab === 'SUPPORT' && (
        <div className="nexus-card">
          <div className="nexus-card-header">
            <h3 className="nexus-card-title">Gửi Yêu Cầu Kỹ Thuật / Báo Hỏng Thiết Bị</h3>
          </div>
          {ticketSent && (
            <div style={{ padding: '12px', background: 'var(--success-bg)', color: 'var(--success)', borderRadius: 'var(--radius)', marginBottom: '1rem', fontWeight: 600 }}>
              ✓ Yêu cầu hỗ trợ của bạn đã được gửi thành công! Đội kỹ thuật khu vực Hà Nội sẽ liên hệ xử lý trong vòng 2 giờ.
            </div>
          )}
          <form onSubmit={handleTicketSubmit}>
            <div className="grid-2">
              <div className="form-group">
                <label className="form-label">Chọn đường truyền gặp sự cố (Account ID) <span className="req">*</span></label>
                <select className="form-select" required>
                  {myConns.map(c => (
                    <option key={c.accountId} value={c.accountId}>
                      {c.accountId} - {c.installationAddress} ({c.equipmentSerial})
                    </option>
                  ))}
                </select>
              </div>
              <div className="form-group">
                <label className="form-label">Loại sự cố cần hỗ trợ <span className="req">*</span></label>
                <select className="form-select" required>
                  <option>Mất tín hiệu mạng / Không kết nối được Internet</option>
                  <option>Tốc độ chậm / Chập chờn đường truyền</option>
                  <option>Thiết bị Modem/Router báo lỗi đèn đỏ / Hỏng hóc</option>
                  <option>Thắc mắc về hóa đơn cước và cách tính thuế 12.24%</option>
                  <option>Đề nghị tạm ngưng đường truyền ngắn hạn</option>
                </select>
              </div>
            </div>
            <div className="form-group">
              <label className="form-label">Mô tả chi tiết tình trạng gặp phải <span className="req">*</span></label>
              <textarea
                className="form-control"
                rows="4"
                placeholder="Vui lòng cung cấp chi tiết: thời điểm xảy ra sự cố, biểu hiện của đèn tín hiệu modem hoặc nội dung cần làm rõ..."
                required
              ></textarea>
            </div>
            <button type="submit" className="btn btn-primary">
              <Send size={16} /> Gửi Yêu Cầu Hỗ Trợ 24/7
            </button>
          </form>
        </div>
      )}

      {/* Printable Receipt Modal */}
      {selectedBill && (
        <div className="modal-overlay">
          <div className="modal-content" style={{ maxWidth: '520px' }}>
            <div className="modal-header">
              <h3 style={{ fontSize: '1.15rem', fontWeight: 700 }}>📄 Hóa Đơn & Biên Lai Thu Phí</h3>
              <button className="btn btn-outline btn-sm" onClick={() => setSelectedBill(null)}>✕</button>
            </div>
            <div className="modal-body">
              <div style={{ background: 'white', border: '1px dashed #cbd5e1', borderRadius: 'var(--radius)', padding: '2rem' }}>
                <div style={{ textAlign: 'center', marginBottom: '1.5rem', borderBottom: '1px dashed #cbd5e1', paddingBottom: '1rem' }}>
                  <h2 style={{ fontSize: '1.25rem', fontWeight: 800, color: 'var(--primary)' }}>NEXUS COMMUNICATION SYSTEM</h2>
                  <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginTop: '2px' }}>DỊCH VỤ VIỄN THÔNG & INTERNET</div>
                  <div style={{ marginTop: '8px', fontWeight: 700 }}>HÓA ĐƠN CƯỚC SỐ: {selectedBill.billId}</div>
                  <div style={{ fontSize: '0.775rem', color: 'var(--text-muted)' }}>Ngày phát hành: {selectedBill.issuedDate}</div>
                </div>

                <div className="flex-between" style={{ marginBottom: '8px', fontSize: '0.9rem' }}>
                  <span className="text-muted">Chủ thuê bao:</span>
                  <strong>{selectedBill.customerName}</strong>
                </div>
                <div className="flex-between" style={{ marginBottom: '8px', fontSize: '0.9rem' }}>
                  <span className="text-muted">Account ID:</span>
                  <code className="font-mono">{selectedBill.accountId}</code>
                </div>
                <div className="flex-between" style={{ marginBottom: '8px', fontSize: '0.9rem' }}>
                  <span className="text-muted">Kỳ thanh toán:</span>
                  <span>{selectedBill.billingCycle}</span>
                </div>
                <hr style={{ border: 'none', borderTop: '1px dashed #cbd5e1', margin: '10px 0' }} />

                <div className="flex-between" style={{ marginBottom: '8px', fontSize: '0.9rem' }}>
                  <span>Cước gói thuê bao:</span>
                  <span>${selectedBill.planFee.toFixed(2)}</span>
                </div>
                <div className="flex-between" style={{ marginBottom: '8px', fontSize: '0.9rem' }}>
                  <span>Cước vượt mức / cuộc gọi:</span>
                  <span>${selectedBill.usageOverdue.toFixed(2)}</span>
                </div>
                <div className="flex-between" style={{ marginBottom: '8px', fontSize: '0.9rem', color: 'var(--text-muted)' }}>
                  <span>Thuế dịch vụ viễn thông (12.24%):</span>
                  <span>${selectedBill.taxAmount.toFixed(2)}</span>
                </div>

                <div className="flex-between" style={{ borderTop: '2px solid var(--text-primary)', marginTop: '1rem', paddingTop: '0.75rem', fontWeight: 800, fontSize: '1.1rem' }}>
                  <span>TỔNG CỘNG:</span>
                  <span style={{ color: 'var(--primary)' }}>${selectedBill.totalThisCycle.toFixed(2)}</span>
                </div>

                <div className="flex-between" style={{ marginTop: '8px', fontSize: '0.9rem' }}>
                  <span className="text-muted">Tình trạng thanh toán:</span>
                  <span className="badge badge-success">✓ ĐÃ THANH TOÁN ĐỦ</span>
                </div>
              </div>
            </div>
            <div className="modal-footer">
              <button className="btn btn-secondary" onClick={() => setSelectedBill(null)}>Đóng</button>
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

