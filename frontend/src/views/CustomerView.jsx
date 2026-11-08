import React, { useState } from 'react';
import { Radio, FileText, Shield, Send } from 'lucide-react';

export default function CustomerView({ connections, bills }) {
  const customerName = 'Công ty Cổ phần Công nghệ Alpha';
  const myConns = connections.filter(c => c.customerName.includes('Alpha'));
  const myBills = bills.filter(b => b.customerName.includes('Alpha'));

  const [ticketSent, setTicketSent] = useState(false);

  const handleTicketSubmit = (e) => {
    e.preventDefault();
    setTicketSent(true);
    setTimeout(() => setTicketSent(false), 5000);
    e.target.reset();
  };

  return (
    <div>
      {/* Profile Card */}
      <div className="nexus-card" style={{ marginBottom: '1.5rem', background: 'linear-gradient(to right, #ffffff, #f0f9ff)', borderColor: '#bae6fd' }}>
        <div className="flex-between">
          <div style={{ display: 'flex', gap: '1rem', alignItems: 'center' }}>
            <div style={{ width: '56px', height: '56px', borderRadius: 'var(--radius)', background: 'var(--primary)', color: 'white', display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: '1.5rem', fontWeight: 800 }}>
              🏢
            </div>
            <div>
              <h2 style={{ fontSize: '1.35rem', fontWeight: 800, color: 'var(--text-primary)' }}>{customerName}</h2>
              <div style={{ fontSize: '0.85rem', color: 'var(--text-secondary)', marginTop: '2px' }}>
                Mã khách hàng: <code className="font-mono">CUST-ALPHA-2026</code> • Trụ sở: Tháp A Keangnam, Cầu Giấy, Hà Nội
              </div>
            </div>
          </div>
          <span className="badge badge-success" style={{ padding: '6px 12px', fontSize: '0.825rem' }}>
            Khách Hàng Doanh Nghiệp (VIP)
          </span>
        </div>
      </div>

      {/* Quick Stats */}
      <div className="stats-grid">
        <div className="stat-card">
          <div className="stat-icon blue"><Radio size={24} /></div>
          <div className="stat-info">
            <div className="stat-value">{myConns.length}</div>
            <div className="stat-label">Đường truyền đang hoạt động</div>
          </div>
        </div>
        <div className="stat-card">
          <div className="stat-icon green"><FileText size={24} /></div>
          <div className="stat-info">
            <div className="stat-value">{myBills.length}</div>
            <div className="stat-label">Hóa đơn cước đã lập</div>
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

      <div className="grid-2">
        {/* Active Connections */}
        <div className="nexus-card">
          <div className="nexus-card-header">
            <h3 className="nexus-card-title">📡 Kết Nối & Thiết Bị Của Tôi</h3>
          </div>
          <div className="table-responsive">
            <table className="nexus-table">
              <thead>
                <tr>
                  <th>Account ID</th>
                  <th>Dịch Vụ</th>
                  <th>Thiết Bị</th>
                  <th>Trạng Thái</th>
                </tr>
              </thead>
              <tbody>
                {myConns.map(c => (
                  <tr key={c.accountId}>
                    <td><strong className="font-mono" style={{ color: 'var(--primary)' }}>{c.accountId}</strong></td>
                    <td>{c.planName}</td>
                    <td><code className="font-mono">{c.equipmentSerial}</code></td>
                    <td><span className="badge badge-success">{c.status}</span></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>

        {/* Bills */}
        <div className="nexus-card">
          <div className="nexus-card-header">
            <h3 className="nexus-card-title">🧾 Hóa Đơn & Tình Trạng Cước</h3>
          </div>
          <div className="table-responsive">
            <table className="nexus-table">
              <thead>
                <tr>
                  <th>Số Hóa Đơn</th>
                  <th>Kỳ Cước</th>
                  <th>Tổng Tiền</th>
                  <th>Tình Trạng</th>
                </tr>
              </thead>
              <tbody>
                {myBills.map(b => (
                  <tr key={b.billId}>
                    <td><strong className="font-mono">{b.billId}</strong></td>
                    <td>{b.billingCycle}</td>
                    <td><strong>${b.totalThisCycle.toFixed(2)}</strong></td>
                    <td>
                      <span className={`badge ${b.status === 'PAID' ? 'badge-success' : 'badge-danger'}`}>
                        {b.status}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </div>

      {/* Support Ticket */}
      <div className="nexus-card mt-4">
        <div className="nexus-card-header">
          <h3 className="nexus-card-title">🆘 Gửi Yêu Cầu Kỹ Thuật / Khiếu Nại Cước</h3>
        </div>
        {ticketSent && (
          <div style={{ padding: '12px', background: 'var(--success-bg)', color: 'var(--success)', borderRadius: 'var(--radius)', marginBottom: '1rem', fontWeight: 600 }}>
            ✓ Yêu cầu hỗ trợ đã được chuyển thành công tới Đội Kỹ thuật & Kế toán phụ trách hợp đồng của bạn!
          </div>
        )}
        <form onSubmit={handleTicketSubmit}>
          <div className="grid-2">
            <div className="form-group">
              <label className="form-label">Chọn đường truyền gặp sự cố</label>
              <select className="form-select">
                {myConns.map(c => (
                  <option key={c.accountId} value={c.accountId}>
                    {c.accountId} - {c.installationAddress}
                  </option>
                ))}
              </select>
            </div>
            <div className="form-group">
              <label className="form-label">Loại yêu cầu</label>
              <select className="form-select">
                <option>Chập chờn tín hiệu mạng / Mất kết nối</option>
                <option>Đề nghị đổi thiết bị Modem/Router hỏng</option>
                <option>Thắc mắc cách tính cước cuộc gọi / vượt giờ</option>
                <option>Tạm ngưng dịch vụ ngắn ngày</option>
              </select>
            </div>
          </div>
          <div className="form-group">
            <label className="form-label">Mô tả chi tiết sự cố</label>
            <textarea className="form-control" rows="3" placeholder="Vui lòng cung cấp chi tiết tình trạng để đội kỹ thuật xử lý nhanh nhất..." required></textarea>
          </div>
          <button type="submit" className="btn btn-primary">
            <Send size={16} /> Gửi Yêu Cầu Hỗ Trợ 24/7
          </button>
        </form>
      </div>
    </div>
  );
}
