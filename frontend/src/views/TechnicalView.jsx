import React from 'react';
import { Search, Zap, Radio, Check, X, RefreshCw, Power } from 'lucide-react';

export default function TechnicalView({ orders, connections, onUpdateOrderStatus, onAddConnection, onUpdateConnectionStatus, onReplaceDevice }) {
  const pendingOrders = orders.filter(o => o.status === 'SUBMITTED');
  const feasibleOrders = orders.filter(o => o.status === 'FEASIBLE');
  const activeConns = connections.filter(c => c.status === 'ACTIVE').length;

  const handleFeasible = (orderId) => {
    onUpdateOrderStatus(orderId, 'FEASIBLE');
    alert(`Đơn ${orderId} đã được xác nhận: ĐẠT KHẢ THI.\nĐã đủ điều kiện kỹ thuật để cấp phát đường truyền.`);
  };

  const handleNotFeasible = (orderId) => {
    onUpdateOrderStatus(orderId, 'NOT_FEASIBLE');
    alert(`Đơn ${orderId} đã bị từ chối: KHÔNG ĐẠT KHẢ THI (Hết cổng hoặc khoảng cách vượt chuẩn).`);
  };

  const handleProvision = (ord) => {
    onUpdateOrderStatus(ord.orderId, 'CONNECTED');

    // Cấp Account ID 16 chữ số: [Type][CityCode][12 số]
    const typeChar = ord.service === 'BROADBAND' ? 'B' : ord.service === 'DIALUP' ? 'D' : 'T';
    const cityCode = '001';
    const nextSeq = (connections.length + 1).toString().padStart(12, '0');
    const accountId = `${typeChar}${cityCode}${nextSeq}`;

    const newConn = {
      accountId,
      orderId: ord.orderId,
      customerName: ord.customerName,
      service: ord.service,
      planName: ord.planName,
      status: 'ACTIVE',
      equipmentSerial: `MODEM-${Math.floor(1000 + Math.random() * 9000)}`,
      cityCode,
      installationAddress: `Chi nhánh ${ord.customerName}, Địa bàn ${ord.city}`,
      activatedAt: new Date().toISOString().split('T')[0]
    };

    onAddConnection(newConn);
    alert(`🎉 Đã cấp phát kết nối thành công!\nMã Account ID: ${accountId}\nThiết bị gán: ${newConn.equipmentSerial}`);
  };

  return (
    <div>
      <div className="flex-between" style={{ marginBottom: '1.5rem' }}>
        <div>
          <h2 style={{ fontSize: '1.5rem', fontWeight: 800, color: 'var(--text-primary)' }}>
            🔧 Bộ Phận Kỹ Thuật (Technical Department)
          </h2>
          <p className="form-hint">Khảo sát hạ tầng, đo kiểm khoảng cách, cấp phát Account ID và quản lý đường truyền</p>
        </div>
      </div>

      {/* KPI Summary */}
      <div className="stats-grid">
        <div className="stat-card">
          <div className="stat-icon orange"><Search size={24} /></div>
          <div className="stat-info">
            <div className="stat-value">{pendingOrders.length}</div>
            <div className="stat-label">Đơn chờ khảo sát hạ tầng</div>
          </div>
        </div>
        <div className="stat-card">
          <div className="stat-icon blue"><Zap size={24} /></div>
          <div className="stat-info">
            <div className="stat-value">{feasibleOrders.length}</div>
            <div className="stat-label">Đơn khả thi chờ cấp đấu nối</div>
          </div>
        </div>
        <div className="stat-card">
          <div className="stat-icon green"><Radio size={24} /></div>
          <div className="stat-info">
            <div className="stat-value">{activeConns}</div>
            <div className="stat-label">Kết nối đang hoạt động</div>
          </div>
        </div>
      </div>

      {/* Section 1: Feasibility & Provisioning */}
      <div className="nexus-card" style={{ marginBottom: '1.5rem' }}>
        <div className="nexus-card-header">
          <h3 className="nexus-card-title">1. Khảo Sát Tính Khả Thi & Thi Công Đấu Nối</h3>
        </div>
        <div className="table-responsive">
          <table className="nexus-table">
            <thead>
              <tr>
                <th>Order ID</th>
                <th>Khách Hàng / Đơn Vị</th>
                <th>Dịch Vụ & Gói</th>
                <th>Số Lượng</th>
                <th>Địa Bàn</th>
                <th>Trạng Thái</th>
                <th>Hành Động Kỹ Thuật</th>
              </tr>
            </thead>
            <tbody>
              {orders.map(o => (
                <tr key={o.orderId}>
                  <td><strong className="font-mono" style={{ color: 'var(--primary)' }}>{o.orderId}</strong></td>
                  <td>
                    <strong>{o.customerName}</strong><br />
                    <span className="text-muted" style={{ fontSize: '0.8rem' }}>{o.phone}</span>
                  </td>
                  <td><span className="badge badge-info">{o.service}</span> {o.planName}</td>
                  <td><strong>{o.quantity}</strong> kết nối</td>
                  <td>{o.city}</td>
                  <td>
                    <span className={`badge ${
                      o.status === 'CONNECTED' ? 'badge-success' :
                      o.status === 'FEASIBLE' ? 'badge-info' :
                      o.status === 'SUBMITTED' ? 'badge-warning' : 'badge-danger'
                    }`}>
                      {o.status}
                    </span>
                  </td>
                  <td>
                    {o.status === 'SUBMITTED' ? (
                      <div className="flex-gap-2">
                        <button className="btn btn-success btn-sm" onClick={() => handleFeasible(o.orderId)}>
                          <Check size={14} /> Khả Thi
                        </button>
                        <button className="btn btn-danger btn-sm" onClick={() => handleNotFeasible(o.orderId)}>
                          <X size={14} /> Không Khả Thi
                        </button>
                      </div>
                    ) : o.status === 'FEASIBLE' ? (
                      <button className="btn btn-primary btn-sm" onClick={() => handleProvision(o)}>
                        <Zap size={14} /> Kích Hoạt & Cấp Account ID
                      </button>
                    ) : (
                      <span className="text-muted" style={{ fontSize: '0.825rem' }}>Đã thi công xong</span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Section 2: Active Connections & Equipment */}
      <div className="nexus-card">
        <div className="nexus-card-header">
          <h3 className="nexus-card-title">2. Quản Lý Kết Nối Đã Cấp Phát (Account ID) & Thiết Bị</h3>
        </div>
        <div className="table-responsive">
          <table className="nexus-table">
            <thead>
              <tr>
                <th>Account ID (16 số)</th>
                <th>Khách Hàng</th>
                <th>Thiết Bị (Serial)</th>
                <th>Địa Chỉ Đấu Nối</th>
                <th>Ngày Kích Hoạt</th>
                <th>Trạng Thái</th>
                <th>Thao Tác</th>
              </tr>
            </thead>
            <tbody>
              {connections.map(c => (
                <tr key={c.accountId}>
                  <td><strong className="font-mono" style={{ color: 'var(--primary)' }}>{c.accountId}</strong></td>
                  <td>
                    <strong>{c.customerName}</strong><br />
                    <span style={{ fontSize: '0.8rem' }} className="text-muted">{c.service}</span>
                  </td>
                  <td><code className="font-mono">{c.equipmentSerial}</code></td>
                  <td style={{ maxWidth: '260px', fontSize: '0.825rem' }}>{c.installationAddress}</td>
                  <td>{c.activatedAt}</td>
                  <td>
                    <span className={`badge ${
                      c.status === 'ACTIVE' ? 'badge-success' :
                      c.status === 'TEMPORARILY_INACTIVE' ? 'badge-warning' : 'badge-danger'
                    }`}>
                      {c.status}
                    </span>
                  </td>
                  <td>
                    <div className="flex-gap-2">
                      {c.status === 'ACTIVE' ? (
                        <button
                          className="btn btn-warning btn-sm"
                          onClick={() => {
                            onUpdateConnectionStatus(c.accountId, 'TEMPORARILY_INACTIVE');
                            alert(`Đã tạm ngưng đường truyền ${c.accountId}!`);
                          }}
                        >
                          <Power size={14} /> Tạm Ngưng
                        </button>
                      ) : (
                        <button
                          className="btn btn-success btn-sm"
                          onClick={() => {
                            onUpdateConnectionStatus(c.accountId, 'ACTIVE');
                            alert(`Đã khôi phục đường truyền ${c.accountId}!`);
                          }}
                        >
                          <Power size={14} /> Mở Lại
                        </button>
                      )}
                      <button
                        className="btn btn-outline btn-sm"
                        onClick={() => {
                          const newSerial = `RTR-NEW-${Math.floor(1000 + Math.random() * 9000)}`;
                          onReplaceDevice(c.accountId, newSerial);
                          alert(`Đã đổi thiết bị cho ${c.accountId} sang ${newSerial}.\nPhí đổi ($40) được ghi nhận Unbilled để Kế toán thu ở kỳ sau.`);
                        }}
                      >
                        <RefreshCw size={14} /> Đổi Thiết Bị
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}

